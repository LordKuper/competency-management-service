#!/usr/bin/env node
/*
 * ASD framework updater - per-file state machine (replaces the old wholesale
 * delete+copy driver).
 *
 * Scope: syncs ONLY the canonical `.asd/` SSoT tree (rules, templates,
 * agents, skills, workflows, hooks, sync.js itself) from the upstream ASD
 * repo into a consumer checkout. It never touches `.claude/`, `.codex/`,
 * `.agents/` or the repo-root AGENTS.md/CLAUDE.md - those are generated
 * *from* canon by `.asd/sync.js`, so update.js hands off to
 * `sync.js --check` once it is done (see runSyncCheck / main()).
 *
 * Classification (delegated to `.asd/sync.js`'s classifyUpdateItem, the
 * Stage-0 pure decision function - this module is the fetch/hash/report/
 * apply driver wrapped around it):
 *   add | update | noop | delete | keep-local-modified | conflict |
 *   conflict-foreign | reject (unsafe path) | foreign (symlink target)
 *
 * Order of operations (plan is explicit about this): fetch the full new
 * file set -> classify EVERY managed path up front -> report ALL conflicts
 * and planned actions -> only THEN write. planUpdate() never touches disk
 * outside of reading; applyPlan() is the only function that writes, and it
 * is always called after the caller has had a chance to see the plan.
 *
 * No writes happen before every conflict is known, and conflicting /
 * foreign paths are never written regardless of mode.
 *
 * Migrations (`.asd/migrations/<version>.js`, see runMigrations below) run
 * as the last step of applyPlan(): AFTER the managed-path replacement above,
 * BEFORE the manifest write that records the reached asd_version - fixed
 * sequencing, no pre/post mode knob.
 *
 * Manifest-schema decision: no new "last known upstream state" file was
 * invented. `.asd/release-manifest.json` already ships `canon_hashes` +
 * (empty-until-now) `upstream_hashes` placeholders. `upstream_hashes` is
 * repurposed here as update.js's own per-managed-path hash ledger: the
 * consumer's LOCAL release-manifest.json (before an update run) IS the
 * record of "what I was last updated to" - no third file needed. See the
 * updated $comment in release-manifest.json for the full rationale.
 *
 * Zero deps beyond Node core + `.asd/sync.js` (also zero-dep). Network
 * fetch (fetchUpstreamTarball) requires `tar` on PATH, same as the old
 * script; the plan/classify/apply core takes a plain `sourceRoot` directory
 * and has no network dependency, so it is fully unit-testable against two
 * local fixture directories (see tests/run.js).
 *
 * Manual invocation (consumer project root):
 *   node .asd/skills/asd-update/update.js [--dry-run]
 *   node .asd/skills/asd-update/update.js --force <relPath...>   (after review,
 *     overwrite specific reported conflicts the user has explicitly confirmed)
 */
'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');
const https = require('https');
const { execFileSync } = require('child_process');

const sync = require(path.join(__dirname, '..', '..', 'sync.js'));

// `sync.js` is itself one of the managed_paths this very driver can
// overwrite (an "update" status like any other canonical file). Node's
// require() cache means the module-level `sync` above stays the OLD code for
// the rest of this process even after applyClassifications() writes a new
// `.asd/sync.js` to disk - so a post-apply `sync.runCheck()` call using it
// would silently run the PREVIOUS release's generator logic, missing
// exactly the kind of drift a generator bug fix was meant to catch. Bust the
// cache and re-require fresh from disk right before the post-apply check.
function loadFreshSync(repoRoot) {
  const syncPath = path.join(repoRoot, '.asd', 'sync.js');
  if (!fs.existsSync(syncPath)) {
    // No .asd/sync.js at the target repo at all (shouldn't happen in a real
    // checkout - it's always a managed file, but test fixtures may omit it).
    // Nothing to freshly load; degrade to the already-loaded module-level
    // sync rather than crashing over a missing convenience file.
    return sync;
  }
  // Does NOT catch here: a file that EXISTS but throws on require (syntax
  // error, broken top-level code) means the JUST-WRITTEN engine is broken -
  // that must surface as a loud failure, not be silently masked by falling
  // back to the OLD engine and reporting a false-green post-update check.
  delete require.cache[require.resolve(syncPath)];
  return require(syncPath);
}

const log = (m) => process.stdout.write(m + '\n');
const die = (m) => { process.stderr.write('asd-update: ' + m + '\n'); process.exitCode = 1; };

// ---------------------------------------------------------------------------
// Managed-path expansion: manifest entries may be a file or a directory
// (walked recursively). Every emitted relPath uses '/' separators regardless
// of host OS, so it matches consistently between the local root and the
// fetched-upstream root and can key the upstream_hashes ledger stably.
//
// posixJoin/walkDir/expandManagedPath/hashIfFile live in sync.js (this
// module already requires it, and sync.js's own release-manifest.json
// hash-ledger recompute needs them too - defining them there and aliasing
// here keeps one implementation instead of two, without sync.js requiring
// this module back).
// ---------------------------------------------------------------------------

const { posixJoin, walkDir, expandManagedPath, hashIfFile } = sync;

// Union of every real file under every managed_paths entry, expanded against
// BOTH roots (old local checkout, freshly fetched upstream) so paths that
// exist on only one side (add / delete) are still included. Unsafe entries
// in the manifest's own managed_paths list are collected separately and
// treated as fatal by the caller - a bad top-level entry is a bigger red
// flag than a single bad leaf file, so this fails the whole run before any
// fs access happens under it.
function buildFileUniverse(oldRoot, oldManifest, newRoot, newManifest) {
  const managedPaths = Array.from(new Set([
    ...(oldManifest.managed_paths || []),
    ...(newManifest.managed_paths || []),
  ]));
  const rejectedManagedPaths = [];
  const relSet = new Set();
  for (const mp of managedPaths) {
    if (!sync.isSafeRelPath(mp)) {
      rejectedManagedPaths.push(mp);
      continue;
    }
    for (const f of expandManagedPath(oldRoot, mp)) relSet.add(f);
    for (const f of expandManagedPath(newRoot, mp)) relSet.add(f);
  }
  return { relPaths: Array.from(relSet).sort(), rejectedManagedPaths };
}

// Case-collision guard: two manifest-derived paths differing only by case
// would silently alias on a case-insensitive filesystem (default on Windows
// and macOS). Fail closed before any write rather than let one clobber the
// other.
function checkCaseCollisions(relPaths) {
  const seen = new Map();
  for (const p of relPaths) {
    const lower = p.toLowerCase();
    const prior = seen.get(lower);
    if (prior !== undefined && prior !== p) {
      throw new Error(`case-collision between managed paths "${prior}" and "${p}" - refusing to run`);
    }
    seen.set(lower, p);
  }
}

// ---------------------------------------------------------------------------
// Classification: hash local + upstream, look up the old recorded hash,
// delegate the actual decision to sync.js's classifyUpdateItem.
// ---------------------------------------------------------------------------

function classifyAll(repoRoot, sourceRoot, oldManifest, newManifest, relPaths) {
  const oldHashes = oldManifest.upstream_hashes || {};
  return relPaths.map((relPath) => {
    const localAbs = path.join(repoRoot, relPath);
    const upstreamAbs = path.join(sourceRoot, relPath);
    if (!sync.isSafeRelPath(relPath)) {
      return { relPath, status: 'reject', localAbs, upstreamAbs };
    }
    const existsLocally = fs.existsSync(localAbs);
    const upstreamExists = fs.existsSync(upstreamAbs);
    const localHash = hashIfFile(localAbs);
    const newUpstreamHash = hashIfFile(upstreamAbs);
    const oldReleaseHash = Object.prototype.hasOwnProperty.call(oldHashes, relPath) ? oldHashes[relPath] : null;
    const status = sync.classifyUpdateItem({
      relPath,
      existsLocally,
      localHash,
      oldReleaseHash,
      newUpstreamHash,
      upstreamExists,
      targetPathForSymlinkCheck: localAbs,
    });
    return { relPath, status, localAbs, upstreamAbs, localHash, newUpstreamHash, oldReleaseHash };
  });
}

const WRITE_STATUSES = new Set(['add', 'update', 'delete']);
const NEEDS_ATTENTION = new Set(['conflict', 'conflict-foreign', 'keep-local-modified', 'foreign', 'reject']);

function summarize(classifications) {
  const byStatus = {};
  for (const item of classifications) {
    (byStatus[item.status] = byStatus[item.status] || []).push(item.relPath);
  }
  return {
    byStatus,
    plannedWrites: classifications.filter((c) => WRITE_STATUSES.has(c.status)).length,
    needsAttention: classifications.filter((c) => NEEDS_ATTENTION.has(c.status)).map((c) => ({ relPath: c.relPath, status: c.status })),
  };
}

// ---------------------------------------------------------------------------
// Plan: classify-only, zero writes. Callers MUST show this in full (per the
// plan's ordering requirement) before ever calling applyPlan.
// ---------------------------------------------------------------------------

function planUpdate(repoRoot, sourceRoot) {
  const oldManifest = sync.loadReleaseManifest(repoRoot); // fail-closed on unknown schema_version
  const newManifest = sync.loadReleaseManifest(sourceRoot); // same fail-closed check on fetched upstream
  const { relPaths, rejectedManagedPaths } = buildFileUniverse(repoRoot, oldManifest, sourceRoot, newManifest);
  if (rejectedManagedPaths.length > 0) {
    throw new Error('unsafe managed_paths entries in manifest, aborting before any writes: ' + rejectedManagedPaths.join(', '));
  }
  checkCaseCollisions(relPaths);
  const classifications = classifyAll(repoRoot, sourceRoot, oldManifest, newManifest, relPaths);
  const localMigrations = listMigrations(path.join(repoRoot, '.asd', 'migrations'));
  const incomingMigrations = listMigrations(path.join(sourceRoot, '.asd', 'migrations'));
  const pendingMigrationVersions = pendingMigrations(unionMigrationsByVersion(localMigrations, incomingMigrations), oldManifest.asd_version, newManifest.asd_version).map((m) => m.version);
  return {
    oldManifest,
    newManifest,
    oldVersion: oldManifest.asd_version,
    newVersion: newManifest.asd_version,
    classifications,
    pendingMigrationVersions,
    report: summarize(classifications),
  };
}

// ---------------------------------------------------------------------------
// Apply: writes only add/update/delete. Everything else (conflict*,
// keep-local-modified, foreign, reject) is left untouched, always.
// ---------------------------------------------------------------------------

// `forceRelPaths` is the set of relPaths the USER has explicitly confirmed
// overwriting. A differing file is a conflict and is never touched without
// explicit confirmation, so some path must honor a confirmed "yes, overwrite";
// add/update/delete are always applied because they were never conflicts to
// begin with.
function applyClassifications(classifications, forceRelPaths) {
  const forceSet = forceRelPaths instanceof Set ? forceRelPaths : new Set(forceRelPaths || []);
  const applied = [];
  for (const item of classifications) {
    const forced = (item.status === 'conflict' || item.status === 'conflict-foreign') && forceSet.has(item.relPath);
    if (item.status === 'add' || item.status === 'update' || forced) {
      const content = sync.readNormalized(item.upstreamAbs);
      sync.writeNormalized(item.localAbs, content);
      applied.push({ relPath: item.relPath, action: forced ? `${item.status}-forced` : item.status });
    } else if (item.status === 'delete') {
      fs.rmSync(item.localAbs, { force: true });
      applied.push({ relPath: item.relPath, action: 'delete' });
    }
  }
  return applied;
}

// Rebuilds the upstream_hashes ledger for the NEXT run: written paths (incl.
// forced conflicts) get the hash we just wrote; deleted paths are dropped;
// everything left untouched (unresolved conflict*/keep-local-modified/
// foreign/reject) keeps its OLD recorded hash, so it keeps surfacing as
// needing attention until a human resolves it.
function buildNextUpstreamHashes(oldManifest, classifications, forceRelPaths) {
  const forceSet = forceRelPaths instanceof Set ? forceRelPaths : new Set(forceRelPaths || []);
  const next = Object.assign({}, oldManifest.upstream_hashes || {});
  for (const item of classifications) {
    const forced = (item.status === 'conflict' || item.status === 'conflict-foreign') && forceSet.has(item.relPath);
    if (item.status === 'add' || item.status === 'update' || item.status === 'noop' || forced) {
      if (item.newUpstreamHash) next[item.relPath] = item.newUpstreamHash;
      else delete next[item.relPath];
    } else if (item.status === 'delete') {
      delete next[item.relPath];
    }
    // Unresolved conflict / conflict-foreign / keep-local-modified / foreign / reject: leave as-is.
  }
  return next;
}

// Writes the manifest with the version migrations actually reached, never
// newManifest.asd_version outright, so a failed migration can't be recorded
// as fully applied.
function writeUpdatedManifest(repoRoot, newManifest, nextUpstreamHashes, reachedVersion) {
  const merged = Object.assign({}, newManifest, {
    asd_version: reachedVersion,
    upstream_hashes: nextUpstreamHashes,
  });
  const p = path.join(repoRoot, '.asd', 'release-manifest.json');
  sync.writeNormalized(p, JSON.stringify(merged, null, 2) + '\n');
}

// ---------------------------------------------------------------------------
// Migrations: `.asd/migrations/<version>.js`, one zero-dependency Node script
// per target ASD version, run against a consumer checkout. Contract, also
// documented in each script's own header comment:
//   - Filename (minus `.js`) is the exact target `asd_version` string the
//     script migrates a consumer TO, e.g. `3.2.0.js`.
//   - `module.exports = (ctx) => MigrationReport | Promise<MigrationReport>`,
//     where `ctx.repoRoot` is the CONSUMER project root and `MigrationReport`
//     is a plain object the script defines for its own tests to assert
//     against; runMigrations collects it per version and never inspects its
//     shape. A script needing sync.js's helpers `require`s `.asd/sync.js`
//     from `ctx.repoRoot` itself.
//   - Idempotent: re-running an already-applied migration is a no-op, never
//     an error - check current state before mutating (e.g. "file already
//     absent" is success, not failure).
//   - Throwing fails the migration; the runner stops there (see below).
// Migrations always run AFTER managed-path replacement, never before - no
// pre/post mode knob.
// ---------------------------------------------------------------------------

// Plain dotted-numeric version compare (asd_version is always e.g. "3.1.0" -
// no pre-release/build-metadata segments to handle).
function compareVersions(a, b) {
  const pa = String(a).split('.').map(Number);
  const pb = String(b).split('.').map(Number);
  const len = Math.max(pa.length, pb.length);
  for (let i = 0; i < len; i++) {
    const diff = (pa[i] || 0) - (pb[i] || 0);
    if (diff !== 0) return diff;
  }
  return 0;
}

const MIGRATION_FILE_RE = /^(\d+(?:\.\d+)*)\.js$/;

// Lists every migration script found on disk, sorted ascending by target
// version. A missing directory (no migrations shipped yet, or a fixture
// without one) yields an empty list rather than an error. Filenames not
// matching the version-only contract are ignored, not rejected - the
// directory may reasonably hold no other file, but this stays permissive
// rather than adding a second failure mode migration authors must avoid.
function listMigrations(migrationsDir) {
  if (!fs.existsSync(migrationsDir)) return [];
  return fs.readdirSync(migrationsDir)
    .map((file) => { const m = file.match(MIGRATION_FILE_RE); return m ? { version: m[1], file } : null; })
    .filter(Boolean)
    .sort((a, b) => compareVersions(a.version, b.version));
}

// Every migration strictly newer than the consumer's current version, up to
// and including the release's target version - ascending order.
function pendingMigrations(migrations, oldVersion, newVersion) {
  return migrations.filter((m) => compareVersions(m.version, oldVersion) > 0 && compareVersions(m.version, newVersion) <= 0);
}

// A migration the incoming release delivers may not exist in the consumer's
// PRE-update tree at all (`.asd/migrations` is itself a managed path); one a
// consumer authored ahead of upstream may exist only locally. Union by
// version, local wins on a name collision, so a plan built before the
// managed-path write still names every migration that will actually be
// found and run afterward.
function unionMigrationsByVersion(localMigrations, incomingMigrations) {
  const byVersion = new Map();
  for (const m of incomingMigrations) byVersion.set(m.version, m);
  for (const m of localMigrations) byVersion.set(m.version, m);
  return Array.from(byVersion.values()).sort((a, b) => compareVersions(a.version, b.version));
}

// Same rationale as loadFreshSync: a migration script shipped in THIS apply
// must be the one that runs, not a stale require-cache copy (there is none
// yet in practice, since these files are new, but a re-run within the same
// process - e.g. tests - must still see the just-written version).
function loadFreshMigration(absPath) {
  delete require.cache[require.resolve(absPath)];
  return require(absPath);
}

// Same rationale, applied to `.asd/sync.js` itself: the module-level `sync`
// required at the top of this file is loaded once per process, so without
// this, a migration requiring `.asd/sync.js` from `ctx.repoRoot` (the
// pattern the contract above mandates) would receive whatever copy an
// earlier require in this same process cached - stale the moment a prior
// managed-path write already replaced the file on disk.
function invalidateSyncCache(repoRoot) {
  const syncPath = path.join(repoRoot, '.asd', 'sync.js');
  if (fs.existsSync(syncPath)) delete require.cache[require.resolve(syncPath)];
}

// Runs every pending migration in ascending order against the consumer at
// `repoRoot` (already updated to the new managed-path content by the time
// this is called). Stops at the first failure and reports which migration
// failed, which ones had already succeeded, and each succeeded migration's
// own report keyed by version. `reachedVersion` is what the caller should
// record: `newVersion` when every pending migration succeeded (including the
// "no migrations were pending" case - nothing blocks advancing straight to
// the target), otherwise the last migration that actually succeeded (or
// `oldVersion` if none did).
async function runMigrations(repoRoot, oldVersion, newVersion) {
  const migrationsDir = path.join(repoRoot, '.asd', 'migrations');
  const pending = pendingMigrations(listMigrations(migrationsDir), oldVersion, newVersion);
  const ran = [];
  const reports = {};
  for (const m of pending) {
    invalidateSyncCache(repoRoot);
    const absPath = path.join(migrationsDir, m.file);
    try {
      const migrate = loadFreshMigration(absPath);
      if (typeof migrate !== 'function') throw new Error('migration does not export a function');
      reports[m.version] = await migrate({ repoRoot });
    } catch (e) {
      return {
        reachedVersion: ran.length > 0 ? ran[ran.length - 1] : oldVersion,
        ran,
        reports,
        failure: { version: m.version, error: String(e && e.message ? e.message : e) },
      };
    }
    ran.push(m.version);
  }
  return { reachedVersion: newVersion, ran, reports, failure: null };
}

// Applies a plan from planUpdate(). No-op (report only) when dryRun.
// `options.force`: relPaths (array or Set) whose 'conflict'/'conflict-foreign'
// classification the user has explicitly confirmed overwriting - the other
// half of "never touch a conflict WITHOUT explicit confirmation". Runs
// pending migrations (see runMigrations) after the managed-path write and
// before the manifest write below, so a migration shipped in this very
// release is the one that runs against the tree it just produced. Runs
// `sync.js --check` after a real apply (canon changed upstream means
// provider-views are now stale and need a subsequent `sync.js --apply`,
// which this function deliberately does NOT do automatically).
async function applyPlan(repoRoot, plan, options) {
  const dryRun = !!(options && options.dryRun);
  const force = (options && options.force) || [];
  if (dryRun) return { dryRun: true, applied: [], syncCheck: null, migrations: null };
  const applied = applyClassifications(plan.classifications, force);
  const nextHashes = buildNextUpstreamHashes(plan.oldManifest, plan.classifications, force);
  const migrations = await runMigrations(repoRoot, plan.oldVersion, plan.newVersion);
  writeUpdatedManifest(repoRoot, plan.newManifest, nextHashes, migrations.reachedVersion);
  const syncCheck = loadFreshSync(repoRoot).runCheck(repoRoot);
  return { dryRun: false, applied, syncCheck, migrations };
}

// ---------------------------------------------------------------------------
// Network fetch (real upstream tarball). Not exercised by tests/run.js -
// tests point planUpdate/applyPlan at a second local fixture directory
// standing in for "upstream" instead.
// ---------------------------------------------------------------------------

function parseRepo(url) {
  const m = String(url).replace(/\.git$/, '').match(/github\.com[/:]([^/]+)\/([^/]+)/);
  if (!m) throw new Error(`cannot parse owner/repo from repo URL: ${url}`);
  return { owner: m[1], name: m[2] };
}

function get(url, cb) {
  https.get(url, { headers: { 'User-Agent': 'asd-update' } }, (res) => {
    if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
      res.resume();
      return get(res.headers.location, cb);
    }
    cb(res);
  }).on('error', (e) => { throw new Error(`network error: ${e.message}`); });
}

function download(url, dest) {
  return new Promise((resolve, reject) => {
    get(url, (res) => {
      if (res.statusCode !== 200) return reject(new Error(`HTTP ${res.statusCode} fetching ${url}`));
      const f = fs.createWriteStream(dest);
      res.pipe(f);
      f.on('finish', () => f.close(() => resolve()));
      f.on('error', reject);
    });
  });
}

function checkTar() {
  try {
    execFileSync('tar', ['--version'], { stdio: 'ignore' });
  } catch {
    throw new Error('`tar` not found on PATH. Install tar (bundled with Win10 1803+, macOS, Linux) and retry.');
  }
}

// Fetches + extracts the upstream tarball named by manifest.repo/branch and
// returns the extracted source root (the directory containing its own
// `.asd/`). Caller is responsible for cleanup (cleanupFetch()).
async function fetchUpstreamTarball(manifest) {
  checkTar();
  const { owner, name } = parseRepo(manifest.repo);
  const branch = manifest.branch || 'main';
  const tarUrl = `https://codeload.github.com/${owner}/${name}/tar.gz/refs/heads/${branch}`;

  const work = fs.mkdtempSync(path.join(os.tmpdir(), 'asd-update-'));
  const exdir = path.join(work, 'x');
  fs.mkdirSync(exdir);

  await download(tarUrl, path.join(work, 'src.tar.gz'));
  execFileSync('tar', ['-xzf', 'src.tar.gz', '-C', 'x'], { cwd: work });

  const top = fs.readdirSync(exdir).filter((d) => fs.statSync(path.join(exdir, d)).isDirectory());
  if (top.length !== 1) throw new Error(`unexpected tarball layout (${top.length} top dirs)`);
  return { sourceRoot: path.join(exdir, top[0]), workDir: work };
}

function cleanupFetch(workDir) {
  try { fs.rmSync(workDir, { recursive: true, force: true }); } catch { /* best effort */ }
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

function printPlan(plan) {
  log(`ASD framework: ${plan.oldVersion || '?'} -> ${plan.newVersion || '?'}`);
  log(`Planned writes: ${plan.report.plannedWrites}`);
  for (const status of Object.keys(plan.report.byStatus)) {
    if (status === 'noop') continue;
    log(`  ${status}: ${plan.report.byStatus[status].length}`);
    for (const relPath of plan.report.byStatus[status]) log(`    - ${relPath}`);
  }
  if (plan.pendingMigrationVersions.length > 0) {
    log(`\nMigrations that will run: ${plan.pendingMigrationVersions.join(', ')}`);
  }
  if (plan.report.needsAttention.length > 0) {
    log('\nNeeds manual attention (left untouched):');
    for (const item of plan.report.needsAttention) log(`  ${item.status}: ${item.relPath}`);
  }
}

// `--force <relPath...>`: every remaining arg after the flag is a relPath the
// user has explicitly confirmed overwriting despite its conflict status.
function parseForceArgs(argv) {
  const idx = argv.indexOf('--force');
  if (idx === -1) return [];
  return argv.slice(idx + 1).filter((a) => a !== '--dry-run');
}

async function main() {
  const dryRun = process.argv.includes('--dry-run');
  const force = parseForceArgs(process.argv);
  const repoRoot = sync.findRepoRoot(process.cwd());
  const oldManifestForFetch = sync.loadReleaseManifest(repoRoot);

  let fetched;
  try {
    fetched = await fetchUpstreamTarball(oldManifestForFetch);
  } catch (e) {
    die(e.message);
    return;
  }

  try {
    const plan = planUpdate(repoRoot, fetched.sourceRoot);
    printPlan(plan);
    if (plan.report.plannedWrites === 0 && plan.report.needsAttention.length === 0) {
      log('\nAlready up to date.');
      return;
    }
    if (dryRun) {
      log('\n[dry-run] no files written.');
      return;
    }
    const result = await applyPlan(repoRoot, plan, { dryRun: false, force });
    log(`\nApplied ${result.applied.length} change(s).`);
    if (result.migrations && result.migrations.ran.length > 0) {
      log(`Ran ${result.migrations.ran.length} migration(s): ${result.migrations.ran.join(', ')}`);
      for (const version of result.migrations.ran) {
        log(`  ${version}: ${JSON.stringify(result.migrations.reports[version])}`);
      }
    }
    if (result.migrations && result.migrations.failure) {
      die(`migration ${result.migrations.failure.version} failed: ${result.migrations.failure.error} (recorded version stays at ${result.migrations.reachedVersion})`);
    }
    const stale = (result.syncCheck || []).filter((i) => i.status === 'stale' || i.status === 'missing');
    if (stale.length > 0) {
      log(`\nCanon changed: ${stale.length} provider-view file(s) are now stale/missing. Run \`node .asd/sync.js --apply <file...>\` to regenerate them:`);
      for (const s of stale) log(`  - ${s.target} (${s.status})`);
    } else {
      log('\nProvider views already current (sync.js --check is clean).');
    }
  } catch (e) {
    die(e.message);
  } finally {
    cleanupFetch(fetched.workDir);
  }
}

if (require.main === module) {
  main();
}

module.exports = {
  expandManagedPath,
  buildFileUniverse,
  checkCaseCollisions,
  classifyAll,
  summarize,
  planUpdate,
  applyPlan,
  buildNextUpstreamHashes,
  compareVersions,
  listMigrations,
  pendingMigrations,
  unionMigrationsByVersion,
  runMigrations,
  fetchUpstreamTarball,
  cleanupFetch,
};

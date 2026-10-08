/*
 * ASD migration -> 5.0.0. Cleanup for this release's `asd-pm` removal: the standalone PM agent is
 * retired, its responsibilities absorbed by the main orchestrator (no spawned agent replaces it).
 * Consumer projects only - this repo does its own equivalent cleanup by editing canon and running
 * `sync.js --apply`, never by running this script.
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the consumer project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const RETIRED_TARGETS = [
  ['.claude', 'agents', 'asd-pm.md'],
  ['.codex', 'agents', 'asd-pm.toml'],
];

// True only when `target` still carries an ASD full-file marker whose recorded content digest
// matches the file's current body - i.e. nothing hand-edited it since generation.
function intactGeneratedView(sync, target) {
  if (sync.isSymlink(target)) return false;
  const text = sync.readNormalized(target);
  const lines = text.split('\n');
  const markerIndex = lines[0] === '---' ? 1 : 0;
  const marker = sync.parseFullFileMarker(lines[markerIndex] || '');
  if (!marker) return false;
  const body = markerIndex === 1 ? [lines[0]].concat(lines.slice(2)).join('\n') : lines.slice(1).join('\n');
  return sync.sha256Hex(body) === marker.contentDigest;
}

// True only when `target`'s real (symlink-resolved) path stays inside `repoRoot`'s real path -
// guards the delete below against a symlink that escapes the repo.
function staysWithinRepo(repoRoot, target) {
  try {
    const root = fs.realpathSync(repoRoot);
    const resolved = fs.realpathSync(target);
    const relative = path.relative(root, resolved);
    return relative !== '' && relative !== '..' && !relative.startsWith(`..${path.sep}`) && !path.isAbsolute(relative);
  } catch (_) {
    return false;
  }
}

/** Deletes the retired `asd-pm` agent's generated provider views, per-target skip reasons in the returned report. */
module.exports = function migrate(ctx) {
  const sync = require(path.join(ctx.repoRoot, '.asd', 'sync.js'));
  const report = { deleted: [], missing: [], skippedUnmarked: [], skippedModified: [], skippedUnsafe: [] };
  for (const parts of RETIRED_TARGETS) {
    const target = path.join(ctx.repoRoot, ...parts);
    const rel = path.relative(ctx.repoRoot, target).replace(/\\/g, '/');
    if (!fs.existsSync(target)) { report.missing.push(rel); continue; }
    if (!staysWithinRepo(ctx.repoRoot, target)) { report.skippedUnsafe.push(rel); continue; }
    if (!sync.hasOwnershipMarker(target)) { report.skippedUnmarked.push(rel); continue; }
    if (!intactGeneratedView(sync, target)) { report.skippedModified.push(rel); continue; }
    fs.rmSync(target, { force: true });
    sync.removeIfEmptyDir(path.dirname(target));
    report.deleted.push(rel);
  }
  return report;
};

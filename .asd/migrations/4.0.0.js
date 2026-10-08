#!/usr/bin/env node
/*
 * ASD migration -> 4.0.0. Cleanup for this release's agent-roster churn: five internal code
 * reviewers merged into asd-reviewer-correctness / asd-reviewer-efficiency, the two dev agents
 * merged into asd-dev, and asd-test-engineer / asd-ux-designer renamed to asd-tester / asd-ux.
 * Consumer projects only - this repo does its own equivalent cleanup by editing canon and running
 * `sync.js --apply`, never by running this script.
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the consumer project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error. This script's MigrationReport shape is
 * `{ deleted, skippedUnmarked, missing, commandsYaml, activeReviewSprints }` (see `migrate` below).
 *
 * Scope, in order:
 *   1. Delete the generated provider views of the nine agents this release retires, gated on the
 *      ASD ownership marker so a consumer's own hand-authored agent of the same name is never
 *      touched - an explicit hardcoded name list, never a generic scan of the generated trees
 *      (that broader scan already exists, marker-gated, in `.asd/sync.js`'s orphan detection,
 *      which a consumer reaches via a separate `sync.js --apply`, not this migration).
 *   2. Add `test_affected` to the consumer's `.asd/project/commands.yaml` when absent and a
 *      supported test runner is detected; leave it alone otherwise.
 *   3. Warn (never rewrite) when an active sprint sits in a review phase, since its
 *      `state.json.reviews.*.verdicts` may carry retired reviewer keys - sprint state is out of
 *      migration scope.
 * Never touches: `.asd/project/config.yaml` values, `.asd/sprints/**` content, `docs/**`, custom
 * rules, custom skills/agents/hooks.
 */
'use strict';

const fs = require('fs');
const path = require('path');

// The nine agent names this release retires. Hardcoded rather than derived from current canon, so
// a later canon edit can never silently change what THIS migration deletes.
const RETIRED_AGENTS = [
  'asd-reviewer-quality',
  'asd-reviewer-implementation',
  'asd-reviewer-ui',
  'asd-reviewer-simplification',
  'asd-reviewer-performance',
  'asd-backend-dev',
  'asd-frontend-dev',
  'asd-test-engineer',
  'asd-ux-designer',
];

function retiredAgentTargets(repoRoot) {
  const targets = [];
  for (const name of RETIRED_AGENTS) {
    targets.push(path.join(repoRoot, '.claude', 'agents', `${name}.md`));
    targets.push(path.join(repoRoot, '.codex', 'agents', `${name}.toml`));
    targets.push(path.join(repoRoot, '.agents', 'skills', name, 'SKILL.md'));
  }
  return targets;
}

function toRepoRel(repoRoot, absPath) {
  return path.relative(repoRoot, absPath).replace(/\\/g, '/');
}

// A consumer's `.asd/sync.js` may still be the pre-4.0.0 engine (any classification that leaves it
// unwritten this apply), which predates this helper - falls back to an equivalent local
// implementation so the destructive delete below always completes rather than throwing mid-way.
function removeIfEmptyDir(sync, absDir) {
  if (typeof sync.removeIfEmptyDir === 'function') {
    sync.removeIfEmptyDir(absDir);
    return;
  }
  if (!fs.existsSync(absDir)) return;
  if (fs.readdirSync(absDir).length > 0) return;
  fs.rmdirSync(absDir);
}

// Deletes exactly one generated-view file, gated on the ASD ownership marker. A missing file is
// success (idempotency: re-running after a prior successful run finds nothing left to delete). A
// file without the marker is a consumer's own agent/skill sharing the retired name by coincidence
// - left alone and reported, never touched.
function deleteMarkedView(sync, repoRoot, absPath, report) {
  const rel = toRepoRel(repoRoot, absPath);
  if (!fs.existsSync(absPath)) {
    report.missing.push(rel);
    return;
  }
  if (!sync.hasOwnershipMarker(absPath)) {
    report.skippedUnmarked.push(rel);
    return;
  }
  fs.rmSync(absPath, { force: true });
  removeIfEmptyDir(sync, path.dirname(absPath));
  report.deleted.push(rel);
}

function deleteRetiredAgentViews(sync, repoRoot, report) {
  for (const absPath of retiredAgentTargets(repoRoot)) {
    deleteMarkedView(sync, repoRoot, absPath, report);
  }
}

// Runner keyed on the test runner actually present in the consumer project, matching the same
// detection convention used at project setup so both paths agree on what counts as "detectable".
// No match, or a matched runner with no such flag, returns null - never guess.
function detectTestAffected(repoRoot) {
  const pkgPath = path.join(repoRoot, 'package.json');
  if (fs.existsSync(pkgPath)) {
    let pkg = null;
    try {
      pkg = JSON.parse(fs.readFileSync(pkgPath, 'utf8'));
    } catch (_) {
      pkg = null;
    }
    if (pkg) {
      const deps = Object.assign({}, pkg.dependencies, pkg.devDependencies);
      if (Object.prototype.hasOwnProperty.call(deps, 'jest')) return 'jest --changedSince=<BASE_REF>';
      if (Object.prototype.hasOwnProperty.call(deps, 'vitest')) return 'vitest run --changed <BASE_REF>';
    }
  }
  const pyManifests = ['pyproject.toml', 'requirements.txt', 'requirements-dev.txt'];
  for (const name of pyManifests) {
    const p = path.join(repoRoot, name);
    if (!fs.existsSync(p)) continue;
    let text = '';
    try {
      text = fs.readFileSync(p, 'utf8');
    } catch (_) {
      continue;
    }
    if (/pytest-testmon/i.test(text)) return 'pytest --testmon';
    if (/pytest-picked/i.test(text)) return 'pytest --picked --mode=branch --parent-branch=<BASE_REF>';
  }
  return null;
}

// Additive only: never overwrites an existing `test_affected` line (active, non-comment), and
// never writes a guessed value when no supported runner is detected - the impacted set then keeps
// falling back to the search-derived definition, exactly as an omitted field already means today.
function migrateCommandsYaml(sync, repoRoot, report) {
  const p = path.join(repoRoot, '.asd', 'project', 'commands.yaml');
  if (!fs.existsSync(p)) {
    report.commandsYaml = { status: 'missing' };
    return;
  }
  const text = sync.readNormalized(p);
  if (/^test_affected\s*:/m.test(text)) {
    report.commandsYaml = { status: 'already-present' };
    return;
  }
  const value = detectTestAffected(repoRoot);
  if (!value) {
    report.commandsYaml = { status: 'undetectable' };
    return;
  }
  const line = `test_affected: "${value}"`;
  const customHeader = /^custom:\s*$/m;
  const next = customHeader.test(text)
    ? text.replace(customHeader, `${line}\n\ncustom:`)
    : `${text.replace(/\n*$/, '\n')}\n${line}\n`;
  sync.writeNormalized(p, next);
  report.commandsYaml = { status: 'added', value };
}

// Warn-only, never rewrites: an active sprint sitting in a review phase may have retired reviewer
// keys under `state.json.reviews.<phase>.verdicts` from the agent-roster rename/merge above.
// Sprint state is out of migration scope - the fix is finishing or re-running that review
// iteration, which the consumer does themselves.
const REVIEW_PHASES = new Set(['design-review', 'impl-review']);

function warnActiveReviewSprints(repoRoot, report, warn) {
  const sprintsDir = path.join(repoRoot, '.asd', 'sprints');
  if (!fs.existsSync(sprintsDir)) return;
  for (const entry of fs.readdirSync(sprintsDir)) {
    const statePath = path.join(sprintsDir, entry, 'state.json');
    if (!fs.existsSync(statePath)) continue;
    let state = null;
    try {
      state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
    } catch (_) {
      continue;
    }
    if (!state || !REVIEW_PHASES.has(state.phase)) continue;
    report.activeReviewSprints.push(entry);
    warn(
      `sprint "${entry}" is mid-"${state.phase}" and may hold retired reviewer keys under ` +
      'state.json.reviews.*.verdicts (agent roster changed in ASD 4.0.0) - finish or re-run that ' +
      'review iteration before relying on the APPROVE latch for this sprint.'
    );
  }
}

module.exports = function migrate(ctx) {
  const repoRoot = ctx.repoRoot;
  const sync = require(path.join(repoRoot, '.asd', 'sync.js'));
  const warn = (m) => process.stdout.write(`asd-migration 4.0.0: warning: ${m}\n`);

  const report = {
    deleted: [],
    skippedUnmarked: [],
    missing: [],
    commandsYaml: null,
    activeReviewSprints: [],
  };

  deleteRetiredAgentViews(sync, repoRoot, report);
  migrateCommandsYaml(sync, repoRoot, report);
  warnActiveReviewSprints(repoRoot, report, warn);

  if (report.skippedUnmarked.length > 0) {
    warn(
      `left ${report.skippedUnmarked.length} unmarked file(s) untouched (not ASD-generated, ` +
      `likely a consumer's own agent/skill sharing a retired name): ${report.skippedUnmarked.join(', ')}`
    );
  }

  return report;
};

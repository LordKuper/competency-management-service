/*
 * ASD migration -> 6.0.0. Cleanup for this release's retirement of `state.json.escalations`: the
 * per-sprint friction log is now the single channel for workflow malfunction, so the key is gone
 * from `t_state.json` and no writer or reader remains. This strips the leftover key from a
 * consumer's active sprint state. Consumer projects only - this repo does its own equivalent
 * cleanup by editing canon, never by running this script.
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the consumer project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error.
 *
 * `.asd/sprints/**` is otherwise out of migration scope (4.0.0 warns about stale sprint state
 * rather than rewriting it). Deliberate exception here: the retired key lives nowhere else, so
 * warn-only would leave a dead field in every live sprint state until that sprint is archived.
 * Archived sprints keep theirs - closed sprints are immutable history.
 *
 * Sprint state is machine-written and every reader parses it; byte layout is not worth preserving,
 * so the rewrite is whole-file.
 *
 * The release's other breaking change, the new `retro` phase between `impl-review` and `pr`,
 * needs no state mutation: an in-flight sprint routes into `retro` through the new chain and
 * lands on the retro workflow's empty-log branch, which reads an absent log and completes.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ARCHIVE_DIR = 'archived';
const RETIRED_KEY = 'escalations';
const INDENT = 2;

// Active sprints only - direct children of `.asd/sprints/` holding a `state.json`. The archive
// subtree is skipped whole.
function activeSprintStatePaths(repoRoot) {
  const sprintsDir = path.join(repoRoot, '.asd', 'sprints');
  if (!fs.existsSync(sprintsDir)) return [];
  const statePaths = [];
  for (const entry of fs.readdirSync(sprintsDir)) {
    if (entry === ARCHIVE_DIR) continue;
    const statePath = path.join(sprintsDir, entry, 'state.json');
    if (fs.existsSync(statePath)) statePaths.push(statePath);
  }
  return statePaths;
}

// Serialized the way sprint state is written - 2-space indent - carrying over the line ending and
// trailing newline of the file being replaced.
function serializeLike(raw, state) {
  const eol = raw.includes('\r\n') ? '\r\n' : '\n';
  const trailingNewline = /\r?\n$/.test(raw) ? eol : '';
  return JSON.stringify(state, null, INDENT).split('\n').join(eol) + trailingNewline;
}

// Temp file plus rename: an interrupted run leaves the sprint's sole recovery point intact rather
// than truncated.
function replaceFileAtomically(filePath, text) {
  const tempPath = `${filePath}.asd-migration.tmp`;
  fs.writeFileSync(tempPath, text, { encoding: 'utf8' });
  fs.renameSync(tempPath, filePath);
}

function stripSprintState(repoRoot, statePath, report, warn) {
  const rel = path.relative(repoRoot, statePath).replace(/\\/g, '/');
  const raw = fs.readFileSync(statePath, 'utf8');
  let state = null;
  try {
    state = JSON.parse(raw);
  } catch (_) {
    report.skipped.push(rel);
    warn(`${rel} is not parsable JSON - left untouched, remove the "escalations" key by hand.`);
    return;
  }
  if (!state || !Object.prototype.hasOwnProperty.call(state, RETIRED_KEY)) {
    report.absent.push(rel);
    return;
  }
  const dropped = state[RETIRED_KEY];
  delete state[RETIRED_KEY];
  if (Array.isArray(dropped) && dropped.length > 0) {
    warn(
      `${rel} carried ${dropped.length} recorded escalation(s), now dropped - re-record them as ` +
      `friction entries in that sprint's friction-log.md: ${JSON.stringify(dropped)}`
    );
  }
  replaceFileAtomically(statePath, serializeLike(raw, state));
  report.stripped.push(rel);
}

/** Removes the retired `escalations` key from every active sprint's `state.json`, per-file outcome in the returned report. */
module.exports = function migrate(ctx) {
  const warn = (m) => process.stdout.write(`asd-migration 6.0.0: warning: ${m}\n`);
  const report = { stripped: [], absent: [], skipped: [] };
  for (const statePath of activeSprintStatePaths(ctx.repoRoot)) {
    stripSprintState(ctx.repoRoot, statePath, report, warn);
  }
  return report;
};

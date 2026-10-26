#!/usr/bin/env node
/*
 * ASD migration -> 14.0.0. Cleanup for this release's roster cut: nine agent specs are retired
 * (creator work moved inline into the orchestrator; the correctness, testing and documentation
 * reviewers merged into one `quality` reviewer; the `critical` task tier folded into `standard`).
 * Consumer projects only - this repo does its own equivalent cleanup by editing canon and running
 * `sync.js --apply`, never by running this script.
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the consumer project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error. This script's MigrationReport shape is
 * `{ deleted, skippedUnmarked, missing, rewritten, unchanged, skipped }`.
 *
 * Scope, in order:
 *   1. Delete the generated Claude/Codex views of the nine retired agents, gated on the ASD
 *      ownership marker so a consumer's own hand-authored agent of the same name is never touched -
 *      an explicit hardcoded name list, never a generic scan of the generated trees.
 *   2. Rewrite every active sprint's `state.json` (atomic, whole-file) so it resumes on the new
 *      roster: each review node and iteration carrying a legacy `correctness`/`testing`/
 *      `documentation` verdict gains a `quality` verdict - `APPROVE` when every verdict of the
 *      node's legacy roster (design: correctness, documentation; impl: all three) is satisfied (an
 *      `APPROVE` token, a `skipped:` skip, or a bare `CONCERNS`/`FAIL` whose review file names
 *      every finding in `resolved:` lines), else the most severe legacy token; an iteration
 *      missing a rostered key gets none (a lost dispatch stays blocking) - and a `quality` latch
 *      only when every rostered reviewer was latched.
 *      `efficiency` is unchanged and the legacy keys stay as history. Every `task_routing` tier
 *      `critical` becomes `standard`.
 * Archived and finished sprints keep their state - closed sprints are immutable history. Never
 * touches config values, `docs/**`, custom rules, agents, skills, hooks or agent memory.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const RETIRED_AGENTS = [
  'asd-ba',
  'asd-ux',
  'asd-architect',
  'asd-reviewer-correctness',
  'asd-reviewer-testing',
  'asd-reviewer-documentation',
  'asd-dev-critical',
  'asd-tester-critical',
  'asd-tester-mechanical',
];
const LEGACY_REVIEWERS = ['correctness', 'testing', 'documentation'];
const DESIGN_ROSTER = ['correctness', 'documentation'];
const IMPL_ROSTER = LEGACY_REVIEWERS;
const MERGED_REVIEWER = 'quality';
const SEVERITY_ORDER = ['FAIL', 'CONCERNS'];
const RETIRED_TIER = 'critical';
const REPLACEMENT_TIER = 'standard';
const ARCHIVE_DIR = 'archived';
const FINISHED_PHASE = 'done';
const INDENT = 2;

function toRepoRel(repoRoot, absPath) {
  return path.relative(repoRoot, absPath).replace(/\\/g, '/');
}

function retiredAgentTargets(repoRoot) {
  return RETIRED_AGENTS.flatMap((name) => [
    path.join(repoRoot, '.claude', 'agents', `${name}.md`),
    path.join(repoRoot, '.codex', 'agents', `${name}.toml`),
  ]);
}

/** Deletes one generated view when it carries the ownership marker; a missing file is success, an unmarked one is reported and kept. */
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
  sync.removeIfEmptyDir(path.dirname(absPath));
  report.deleted.push(rel);
}

function activeSprintDirs(repoRoot) {
  const sprintsDir = path.join(repoRoot, '.asd', 'sprints');
  if (!fs.existsSync(sprintsDir)) return [];
  return fs.readdirSync(sprintsDir)
    .filter((entry) => entry !== ARCHIVE_DIR)
    .map((entry) => path.join(sprintsDir, entry))
    .filter((dir) => fs.existsSync(path.join(dir, 'state.json')));
}

function serializeLike(raw, state) {
  const eol = raw.includes('\r\n') ? '\r\n' : '\n';
  const trailingNewline = /\r?\n$/.test(raw) ? eol : '';
  return JSON.stringify(state, null, INDENT).split('\n').join(eol) + trailingNewline;
}

function replaceFileAtomically(filePath, text) {
  const tempPath = `${filePath}.asd-migration.tmp`;
  fs.writeFileSync(tempPath, text, { encoding: 'utf8' });
  fs.renameSync(tempPath, filePath);
}

function verdictToken(value) {
  return String(value).trim().split(/\s+/)[0].replace(/[^A-Za-z]/g, '').toUpperCase();
}

function isSkipOrApprove(value) {
  return verdictToken(value) === 'APPROVE' || /^\s*skipped:/i.test(String(value));
}

/** Finding ids a review file lists in its `findings.json`, or null when that file is absent or unreadable. */
function findingIds(reviewDir, reviewer) {
  try {
    const findings = JSON.parse(fs.readFileSync(path.join(reviewDir, `${reviewer}.findings.json`), 'utf8'));
    return findings.map((finding) => String(finding.id));
  } catch (_) {
    return null;
  }
}

function resolvedIds(reviewDir, reviewer) {
  let text = '';
  try {
    text = fs.readFileSync(path.join(reviewDir, `${reviewer}.md`), 'utf8');
  } catch (_) {
    return new Set();
  }
  const ids = new Set();
  for (const line of text.split(/\r?\n/)) {
    const match = /^resolved:\s*(.+?)\s+[—-]+\s+\S/.exec(line);
    if (!match) continue;
    for (const id of match[1].split(',')) ids.add(id.trim());
  }
  return ids;
}

/** True when a bare `CONCERNS`/`FAIL` verdict has every finding named in its review file's `resolved:` lines. */
function isResolvedBare(reviewDir, reviewer) {
  const ids = findingIds(reviewDir, reviewer);
  if (!ids || ids.length === 0) return false;
  const resolved = resolvedIds(reviewDir, reviewer);
  return ids.every((id) => resolved.has(id));
}

function mostSevereToken(values) {
  const tokens = values.map(verdictToken);
  return SEVERITY_ORDER.find((token) => tokens.includes(token)) || tokens[0];
}

/** The `quality` verdict for one iteration, or null when it holds no legacy verdict or lacks one its node's roster required (a lost dispatch, blocking until re-dispatched). */
function mergedVerdict(iterationVerdicts, reviewDir, roster) {
  if (!roster.every((key) => key in iterationVerdicts)) return null;
  const unsatisfied = roster
    .filter((key) => !isSkipOrApprove(iterationVerdicts[key]) && !isResolvedBare(reviewDir, key))
    .map((key) => iterationVerdicts[key]);
  return unsatisfied.length === 0 ? 'APPROVE' : mostSevereToken(unsatisfied);
}

/** Adds `quality` verdicts and a `quality` latch to one review node whose legacy roster is `roster`; returns whether the node changed. */
function migrateReviewNode(node, reviewDirOf, roster) {
  if (!node || typeof node.verdicts !== 'object' || node.verdicts === null) return false;
  let changed = false;
  for (const [iteration, verdicts] of Object.entries(node.verdicts)) {
    if (!verdicts || MERGED_REVIEWER in verdicts) continue;
    const merged = mergedVerdict(verdicts, reviewDirOf(iteration), roster);
    if (merged === null) continue;
    verdicts[MERGED_REVIEWER] = merged;
    changed = true;
  }
  const latched = node.latched;
  if (latched && !(MERGED_REVIEWER in latched) && roster.every((key) => key in latched)) {
    latched[MERGED_REVIEWER] = Math.max(...roster.map((key) => latched[key]));
    changed = true;
  }
  return changed;
}

function migrateReviews(reviews, sprintDir) {
  if (!reviews) return false;
  const designDir = (iteration) => path.join(sprintDir, 'reviews', 'design', iteration);
  let changed = migrateReviewNode(reviews.design, designDir, DESIGN_ROSTER);
  const impl = reviews.impl;
  if (!impl) return changed;
  if (!Array.isArray(impl.waves)) {
    return migrateReviewNode(impl, (iteration) => path.join(sprintDir, 'reviews', 'impl', iteration), IMPL_ROSTER) || changed;
  }
  impl.waves.forEach((wave, index) => {
    const waveDir = (iteration) => path.join(sprintDir, 'reviews', 'impl', `wave-${index + 1}`, iteration);
    if (migrateReviewNode(wave, waveDir, IMPL_ROSTER)) changed = true;
  });
  return changed;
}

function migrateTaskRouting(routing) {
  if (!routing || typeof routing !== 'object') return false;
  let changed = false;
  for (const entry of Object.values(routing)) {
    if (entry && entry.tier === RETIRED_TIER) {
      entry.tier = REPLACEMENT_TIER;
      changed = true;
    }
  }
  return changed;
}

function migrateSprintState(repoRoot, sprintDir, report, warn) {
  const statePath = path.join(sprintDir, 'state.json');
  const rel = toRepoRel(repoRoot, statePath);
  const raw = fs.readFileSync(statePath, 'utf8');
  let state = null;
  try {
    state = JSON.parse(raw);
  } catch (_) {
    report.skipped.push(rel);
    warn(`${rel} is not parsable JSON - left untouched, add the "quality" verdicts and set critical tiers to standard by hand.`);
    return;
  }
  if (!state || state.phase === FINISHED_PHASE || state.archived_at) {
    report.skipped.push(rel);
    return;
  }
  const reviewsChanged = migrateReviews(state.reviews, sprintDir);
  const routingChanged = migrateTaskRouting(state.task_routing);
  if (!reviewsChanged && !routingChanged) {
    report.unchanged.push(rel);
    return;
  }
  replaceFileAtomically(statePath, serializeLike(raw, state));
  report.rewritten.push(rel);
}

/** Removes the nine retired agents' generated views and rewrites active sprint state to the merged reviewer and single task tier. */
module.exports = function migrate(ctx) {
  const repoRoot = ctx.repoRoot;
  const sync = require(path.join(repoRoot, '.asd', 'sync.js'));
  const warn = (m) => process.stdout.write(`asd-migration 14.0.0: warning: ${m}\n`);
  const report = { deleted: [], skippedUnmarked: [], missing: [], rewritten: [], unchanged: [], skipped: [] };

  for (const absPath of retiredAgentTargets(repoRoot)) {
    deleteMarkedView(sync, repoRoot, absPath, report);
  }
  for (const sprintDir of activeSprintDirs(repoRoot)) {
    migrateSprintState(repoRoot, sprintDir, report, warn);
  }
  if (report.skippedUnmarked.length > 0) {
    warn(
      `left ${report.skippedUnmarked.length} unmarked file(s) untouched (not ASD-generated, ` +
      `likely a consumer's own agent sharing a retired name): ${report.skippedUnmarked.join(', ')}`
    );
  }
  return report;
};

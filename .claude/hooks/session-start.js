// ASD generated. Edit .asd/hooks/session-start.js. source_digest=sha256:f0ebc50ed2cbe3557dd5100064feb4499091bdb14ffc2385c74ac2cd3b95eb23 content_digest=sha256:f0ebc50ed2cbe3557dd5100064feb4499091bdb14ffc2385c74ac2cd3b95eb23 asd_version=13.4.0 schema=1
// ASD SessionStart hook (canonical, provider-agnostic).
// No shebang: this file is never executed directly (`./session-start.js`),
// always invoked as `node <path> --provider ...`, and every generated
// provider-view target prepends an ownership-marker comment as line 1 - a
// shebang on line 2 would not be recognized by Node and breaks parsing.
// Detect active sprint, inject summary into agent context.
// Silent fail on any error (hooks must not block session).
//
// Invocation (wired via .claude/settings.json / .codex/hooks.json):
//   node .asd/hooks/session-start.js --provider claude|codex
// Repo root is resolved by walking up from this script's own location (then,
// as a fallback, from cwd) until a `.asd/` directory is found - never
// `process.cwd()` alone - so this works when invoked from a nested directory.
// The `--provider` argument is required by the wiring contract (no host
// heuristics) and changes the printed skill-invocation form: `/asd-*` for
// Claude Code, `$asd-*` for Codex (providers.md § semantic ops - Codex has
// no slash-command equivalent). Everything else in the summary is identical.

'use strict';

const fs = require('fs');
const path = require('path');

const WORKFLOW_NAME_RE = /^[a-z]+$/;

// Loads `<root>/.asd/workflows/<name>.json` and returns its `phases` array,
// or null on any missing/malformed/unknown definition (or invalid name) -
// the hook degrades silently: no chain info, never throws.
function loadWorkflowPhases(repoRoot, name) {
  if (typeof name !== 'string' || !WORKFLOW_NAME_RE.test(name)) return null;
  const filePath = path.join(repoRoot, '.asd', 'workflows', `${name}.json`);
  try {
    const def = JSON.parse(fs.readFileSync(filePath, 'utf8'));
    if (!def || typeof def !== 'object' || !Array.isArray(def.phases) || !def.phases.every(p => typeof p === 'string')) {
      return null;
    }
    return def.phases;
  } catch (_) {
    return null;
  }
}

// `state.workflow` absent reads as `standard` (`sprint-lifecycle.md` "Workflows").
function phasesForState(repoRoot, state) {
  const name = state && typeof state.workflow === 'string' ? state.workflow : 'standard';
  return loadWorkflowPhases(repoRoot, name);
}

function findUp(startDir) {
  let dir = path.resolve(startDir);
  for (;;) {
    if (fs.existsSync(path.join(dir, '.asd'))) return dir;
    const parent = path.dirname(dir);
    if (parent === dir) return null;
    dir = parent;
  }
}

// Prefer resolving from the script's own location (works no matter what cwd
// the host process used to invoke us); fall back to cwd walk-up; fall back to
// raw cwd so the hook never throws even in a non-ASD directory.
function resolveRepoRoot() {
  return findUp(__dirname) || findUp(process.cwd()) || process.cwd();
}

function parseProvider(argv) {
  const idx = argv.indexOf('--provider');
  if (idx === -1 || idx === argv.length - 1) return null;
  const value = argv[idx + 1];
  return value === 'claude' || value === 'codex' ? value : null;
}

// Current branch from `<root>/.git/HEAD` (offline). Null on a detached HEAD,
// a worktree `.git` file, or any missing/odd shape - callers degrade silently.
function currentBranch(repoRoot) {
  try {
    const m = /^ref: refs\/heads\/(.+)\s*$/.exec(fs.readFileSync(path.join(repoRoot, '.git', 'HEAD'), 'utf8'));
    return m ? m[1] : null;
  } catch (_) {
    return null;
  }
}

// Merged-unclosed, offline: phase `pr` with a PR number, and the sprint's
// branch is not the checked-out one (the sprint branch was left after merge).
function isMergedUnclosed(state, branch) {
  const pr = state.pr;
  return state.phase === 'pr' && Boolean(pr) && typeof pr === 'object' && pr.number != null
    && branch !== null && typeof state.branch === 'string' && state.branch !== branch;
}

function findActiveSprints(repoRoot) {
  const sprintsDir = path.join(repoRoot, '.asd', 'sprints');
  if (!fs.existsSync(sprintsDir)) return [];
  const active = [];
  const branch = currentBranch(repoRoot);
  const addState = (folder, statePath, archived) => {
    try {
      const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
      if (!state || typeof state !== 'object' || Array.isArray(state)) return;
      if (archived) {
        const phases = phasesForState(repoRoot, state);
        if ((phases && !phases.includes(state.phase)) || state.phase === 'done') return;
      }
      active.push({ folder, state, mergedUnclosed: isMergedUnclosed(state, branch) });
    } catch (_) {
      return;
    }
  };
  const entries = fs.readdirSync(sprintsDir, { withFileTypes: true });
  for (const entry of entries) {
    if (!entry.isDirectory() || entry.name === 'archived') continue;
    const statePath = path.join(sprintsDir, entry.name, 'state.json');
    if (!fs.existsSync(statePath)) continue;
    addState(entry.name, statePath, false);
  }
  const archivedDir = path.join(sprintsDir, 'archived');
  if (!fs.existsSync(archivedDir)) return active;
  for (const entry of fs.readdirSync(archivedDir, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const statePath = path.join(archivedDir, entry.name, 'state.json');
    if (!fs.existsSync(statePath)) continue;
    addState(entry.name, statePath, true);
  }
  return active;
}

function nextPhase(phases, current) {
  const idx = phases.indexOf(current);
  if (idx < 0 || idx >= phases.length - 1) return 'done';
  return phases[idx + 1];
}

// Design-block collapse test (sprint-lifecycle.md): every frozen design document is false.
function isDesignCollapsed(documents) {
  return Boolean(documents) && typeof documents === 'object' && ['prd', 'ux_spec', 'adr', 'c4'].every(name => documents[name] === false);
}

// reviews.impl is either wave-aware ({wave, waves: [node, ...]}) or the
// legacy flat node itself (no `waves` array), read as waves: [node], wave: 1.
// Guards every shape defect so the hook never throws on malformed state.
function normalizeImplReviews(reviews) {
  if (!reviews || typeof reviews !== 'object') return null;
  const impl = reviews.impl;
  if (!impl || typeof impl !== 'object') return null;
  if (Array.isArray(impl.waves) && impl.waves.length > 0) {
    const total = impl.waves.length;
    const wave = Number.isInteger(impl.wave) && impl.wave >= 1 && impl.wave <= total ? impl.wave : 1;
    const node = impl.waves[wave - 1] || null;
    return { node, wave, total };
  }
  return { node: impl, wave: 1, total: 1 };
}

// Pick the relevant review node for the current phase. In a review phase use
// that phase's node; otherwise impl-review's current wave node once any wave
// has iterated (impl-review runs after design-review, and a wave's counter
// restarts at 1, so comparing counters would pick design), else design's.
function reviewNodeForPhase(reviews, phase) {
  if (!reviews || typeof reviews !== 'object') return null;
  const implInfo = normalizeImplReviews(reviews);
  if (phase === 'design-review') return reviews.design || null;
  if (phase === 'impl-review') return implInfo ? implInfo.node : null;
  const implWaves = !implInfo ? [] : Array.isArray(reviews.impl.waves) ? reviews.impl.waves : [reviews.impl];
  if (implWaves.some(n => n && n.iteration > 0)) return implInfo.node;
  const d = reviews.design || null;
  return d && d.iteration > 0 ? d : null;
}

// `iter-NN` keys are zero-padded to two digits, so lexical order breaks past
// iter-99; extract the numeric suffix so the highest iteration is picked
// numerically, not lexically.
function iterNumber(key) {
  const m = typeof key === 'string' && /^iter-(\d+)$/.exec(key);
  return m ? parseInt(m[1], 10) : -1;
}

// Display-only session summary, never a gate. "APPROVE"-prefixed values (bare or availability-skip; the partial form is legacy only) count as satisfied.
function lastReviewVerdict(node) {
  if (!node || typeof node !== 'object') return 'n/a';
  const verdictsByIter = node.verdicts;
  if (!verdictsByIter || typeof verdictsByIter !== 'object') return 'n/a';
  const iters = Object.keys(verdictsByIter).sort((a, b) => iterNumber(a) - iterNumber(b));
  if (iters.length === 0) return 'n/a';
  const latest = verdictsByIter[iters[iters.length - 1]];
  if (!latest || typeof latest !== 'object') return 'n/a';
  const verdicts = Object.values(latest);
  const isSkipped = v => typeof v === 'string' && /^skipped:/.test(v);
  const approved = v => v === 'green' || (typeof v === 'string' && v.indexOf('APPROVE') === 0);
  if (verdicts.some(v => v === 'red' || v === 'FAIL')) return 'red';
  if (verdicts.some(v => v === 'yellow' || v === 'CONCERNS')) return 'yellow';
  if (verdicts.length > 0 && verdicts.some(approved) && verdicts.every(v => approved(v) || isSkipped(v))) return 'green';
  return 'mixed';
}

// Skill invocation form differs per provider (providers.md § semantic ops):
// Claude Code uses `/asd-*` slash commands; Codex has no such form and is
// invoked via `$asd-*` (or the `/skills` picker, or implicit description
// match) - printing `/asd-sprint` into a Codex session names a command that
// doesn't exist there.
function skillRef(provider, name) {
  return provider === 'codex' ? `$${name}` : `/${name}`;
}

function summary(active, provider, repoRoot) {
  if (active.length === 0) {
    return `[ASD] No active sprint. Run ${skillRef(provider, 'asd-sprint')} to begin, or ${skillRef(provider, 'asd-init')} to set up the workflow.`;
  }
  if (active.length > 1) {
    const ids = active.map(a => a.state.sprint_id || a.folder).join(', ');
    return `[ASD] WARNING: multiple active sprints found (${ids}). Manual cleanup needed in .asd/sprints/.`;
  }
  const { state, folder, mergedUnclosed } = active[0];
  const id = state.sprint_id || folder;
  const phase = state.phase || 'unknown';
  const reviewNode = reviewNodeForPhase(state.reviews, phase);
  const iter = reviewNode && reviewNode.iteration != null ? reviewNode.iteration : 0;
  const branch = state.branch || 'unknown';
  const verdict = lastReviewVerdict(reviewNode);
  const phases = phasesForState(repoRoot, state);
  const next = phase === 'pr' ? (mergedUnclosed ? 'done' : 'await-merge')
    : !phases ? null
    : (phase === 'audit' && phases.includes('design') && isDesignCollapsed(state.documents)) ? 'plan'
    : nextPhase(phases, phase);
  const implInfo = phase === 'impl-review' ? normalizeImplReviews(state.reviews) : null;
  const iterPart = !phase.endsWith('-review') ? ''
    : (implInfo && implInfo.total > 1) ? ` (wave ${implInfo.wave}/${implInfo.total}, iter ${iter})`
    : ` (iter ${iter})`;
  const lines = [
    `[ASD] Active sprint: ${id}`,
    `  Phase: ${phase}${iterPart}`,
    `  Branch: ${branch}`,
    `  Last review verdict: ${verdict}`,
  ];
  if (next !== null) lines.push(`  Next phase: ${next}`);
  lines.push(`  Continue with ${skillRef(provider, 'asd-sprint')}.`);
  return lines.join('\n');
}

(function main() {
  try {
    const provider = parseProvider(process.argv.slice(2)) || 'claude'; // defaults to claude's slash-command form if wiring ever omits the arg
    const repoRoot = resolveRepoRoot();
    const active = findActiveSprints(repoRoot);
    const text = summary(active, provider, repoRoot);
    const output = {
      hookSpecificOutput: {
        hookEventName: 'SessionStart',
        additionalContext: text,
      },
    };
    process.stdout.write(JSON.stringify(output));
    process.exit(0);
  } catch (_) {
    // silent fail
    process.exit(0);
  }
})();

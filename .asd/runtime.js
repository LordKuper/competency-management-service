'use strict';

const crypto = require('crypto');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawnSync } = require('child_process');
const { setTimeout: delay } = require('timers/promises');

const CACHE_SCHEMA = 1;
const PROBE_TIMEOUT_MS = 3000;
const MAX_NEGATIVE_TTL_MS = 3600000;
/** The longest a tool call may run before its silent transcript counts as a stall: the host's maximum command timeout. */
const TOOL_CALL_CEILING_MS = 600000;
const RESERVED_CHANGE_RISKS = ['security', 'authentication', 'migration', 'public contract', 'workflow gate'];
/** The single review-ledger row vocabulary: allowed statuses per row type, plus the one status carrying `p` and the one carrying `f`. Emitted into every manifest and enforced on every ledger from here, so published and enforced vocabulary cannot drift. */
const LEDGER_VOCABULARY = { files: ['checked', 'n/a'], rules: ['pass', 'n/a', 'finding'], sections: ['reviewed', 'n/a'], p: 'n/a', f: 'finding' };
/** One filled ledger row, published beside the vocabulary so a reviewer reads the row shape off its own input too. Its status is taken from the vocabulary constant and paired with the key that status requires, so the example cannot teach a row the validator rejects. */
const LEDGER_ROW_EXAMPLE = { i: '<manifest id>', s: LEDGER_VOCABULARY.p, p: '<allowed n/a predicate>' };
const ROW_TYPES = Object.keys(LEDGER_VOCABULARY).filter((key) => Array.isArray(LEDGER_VOCABULARY[key]));
/** The `n_a` shape: row type, then manifest id, then its allowed predicate list. Published beside the vocabulary under its own key, because `n_a` itself carries per-dispatch content. */
const LEDGER_NA_SHAPE = Object.fromEntries(ROW_TYPES.map((type) => [type, { [LEDGER_ROW_EXAMPLE.i]: [LEDGER_ROW_EXAMPLE.p] }]));
/** Changed lines (added plus deleted) one impl-review wave carries; a larger scope divides into more waves, so no review turn holds an oversized diff. */
const WAVE_THRESHOLD_LINES = 3000;
/** Most review waves one scope divides into, so an oversized scope still ends in a bounded number of sequential reviews. */
const MAX_REVIEW_WAVES = 3;
/** Files one impl-review wave carries, so a many-file scope divides into waves even when its diff is small. */
const WAVE_THRESHOLD_FILES = 34;
/** Diff bytes one impl-review wave carries; a larger scope divides into more waves, so no review turn reads an oversized patch. */
const WAVE_THRESHOLD_BYTES = 180000;
/** Files above which one review wave gets a turn plan in its reviewer payload. */
const LARGE_WAVE_FILES = 12;
/** An audit whose touched areas track more than this many files gets a batched-read plan in the architect payload. */
const AUDIT_BATCH_THRESHOLD_FILES = 200;
/** Internal reviewers, named as `emit-manifest --reviewer` takes them. */
const INTERNAL_REVIEWERS = ['correctness', 'efficiency', 'testing', 'documentation', 'combined'];
/** The `emit-manifest --reviewer` name that emits External Review's scope manifest instead of a coverage manifest. */
const EXTERNAL_REVIEWER = 'external';
/** The lite internal reviewer: its manifest composes these reviewers' rubrics by reference, in order, then its own agent's rubric. */
const COMBINED_REVIEWER = 'combined';
const COMBINED_RUBRICS = ['correctness', 'efficiency', 'documentation'];
/** Sprint workflow definitions, one `<name>.json` each, beside the phase orchestration bodies. */
const WORKFLOWS_DIR = path.join(__dirname, 'workflows');
/** The exact key set of a workflow definition; its semantics stay prose in the rule docs. */
const WORKFLOW_KEYS = ['name', 'next', 'phases', 'reviewers', 'rollback_reset'];
/** Review nodes a definition's `reviewers` and `rollback_reset` are keyed by, as `persist-review --phase` takes them. */
const REVIEW_NODES = ['design', 'impl'];
/** `NEXT:` targets that end the chain instead of naming a phase: `pr`'s open and merge exits; `done` names completion, the closure write follows at the next sprint's start. */
const CHAIN_EXITS = ['await-merge', 'done'];
/** Severities a finding row may carry (`review-policy.md` "Severity levels"). */
const SEVERITIES = ['low', 'medium', 'high', 'critical'];
/** The standing n/a predicates, each the exact text a ledger row records. The emitter authorizes one only where its condition holds; this is their sole home. */
const NA_PREDICATES = {
  phaseGate: 'outside phase gate',
  uiSurface: 'no UI surface in scope',
  perf: 'no perf budgets section and no executable file in scope',
  noBudgets: 'no budgets defined',
  noHtml: 'no HTML file in scope',
  noSelfHosting: 'self_hosting not enabled',
  noTemplated: 'no templated artefact in scope',
  pureRename: 'pure rename: identical content and mode',
  noDocs: 'no documentation file in scope',
};
/** Rubric entries each conditional predicate covers, by reviewer and id prefix, a combined manifest taking those of every rubric it composes; a prefix matching no entry fails the emit closed. A combined manifest's `no documentation file in scope` covers its whole composed Documentation part instead. */
const NA_TARGETS = {
  ui: { correctness: ['UI conformance'] },
  perf: { efficiency: ['Perf budget compliance', 'Perf anti-patterns', 'Algorithmic complexity', 'Regression detection', 'Hot path identification'] },
  budgetCompliance: { efficiency: ['Perf budget compliance'] },
  html: { documentation: ['HTML shell wrapping', 'Provenance', 'Traceability'] },
  selfHosting: { documentation: ['Framework mode'] },
  templated: { documentation: ['Template adherence'] },
};
const PHASES = ['design-review', 'impl-review'];
/** A retro row's acting side; like its row id, an English literal under any docs language, so intake can filter on it. */
const ACTS_ON = ['consumer', 'asd'];
/** The retro tables intake reads: row-id prefix and cell positions, fixed by the retrospective template because translated headers cannot locate them. Actions may be absent (empty friction log); systemic proposals never are. */
const RETRO_TABLES = [
  { section: 'actions', prefix: 'A', cells: 4, guardrail: 1, actsOn: 2, home: 3, required: false },
  { section: 'systemic-proposals', prefix: 'P', cells: 4, guardrail: 0, actsOn: 1, home: 2, required: true },
];
/** The retro backlog's one table header. */
const BACKLOG_HEADER = ['Row', 'Acts on', 'Disposition', 'Decided in', 'Guardrail'];
/** Retro backlog dispositions; `closed` is a row verified already resolved at HEAD. */
const BACKLOG_DISPOSITIONS = ['deferred', 'included', 'rejected', 'closed'];
/** The agent-memory tree as a git top-relative pathspec, so a check run from any directory scans the same files. */
const AGENT_MEMORY_PATHSPEC = ':/.claude/agent-memory';
/** Concrete work-history ordinals agent memory must not record: a sprint id, a Task, wave or iteration number, or a recorded review verdict. A placeholder such as `iter-NN` or `Task <N>` carries no digits and passes; a bare verdict word is not scanned. */
const MEMORY_HISTORY_PATTERNS = [
  /\b\d{3}-[a-z][a-z0-9]*(-[a-z0-9]+)+\b/i,
  /\bsprint[ -]?\d+\b/i,
  /\bTask \d+\b/,
  /\bwave[- ]\d+\b/i,
  /\biter-\d+\b/i,
  /\biteration \d+\b/i,
  /\[REVIEW-(design|impl)-[a-z]+\]: *(APPROVE|CONCERNS|FAIL)/,
];

function stable(value) {
  if (Array.isArray(value)) return '[' + value.map(stable).join(',') + ']';
  if (value && typeof value === 'object') return '{' + Object.keys(value).sort().map((key) => JSON.stringify(key) + ':' + stable(value[key])).join(',') + '}';
  return JSON.stringify(value);
}

/** Returns a stable SHA-256 identity for resumable runtime evidence. */
function fingerprint(value) {
  return crypto.createHash('sha256').update(stable(value)).digest('hex');
}

function fail(message) {
  throw new Error(message);
}

function stringArray(value, name) {
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'string' || item.includes('\0'))) fail(`${name} must be a string array`);
  return value;
}

/** Normalizes one declared risk; an untyped name carries the strictest target, a reserved class fails closed unless declared against the change. */
function riskEntry(value) {
  const entry = typeof value === 'string' ? { name: value, target: 'change' } : value;
  if (!entry || typeof entry !== 'object' || Array.isArray(entry)) fail('risks entry must be a name or a typed risk');
  if (typeof entry.name !== 'string' || entry.name.length === 0 || entry.name.includes('\0')) fail('risks entry name must be a non-empty string');
  if (entry.target !== 'change' && entry.target !== 'artifact') fail('risks entry target must be change or artifact');
  if (entry.target === 'artifact' && RESERVED_CHANGE_RISKS.includes(entry.name.toLowerCase().replace(/[\s_-]+/g, ' ').trim())) fail(`reserved risk class is change by definition: ${entry.name}`);
  return { name: entry.name, target: entry.target };
}

function riskArray(value, name) {
  if (!Array.isArray(value)) fail(`${name} must be an array of risks`);
  return value.map(riskEntry);
}

/** Reads and shape-validates one workflow definition, `.asd/workflows/<name>.json`; an unknown name or a malformed file throws. */
function loadWorkflow(name) {
  if (typeof name !== 'string' || !/^[a-z]+$/.test(name)) fail(`workflow name invalid: ${name}`);
  const file = path.join(WORKFLOWS_DIR, `${name}.json`);
  if (!fs.existsSync(file)) fail(`unknown workflow: ${name}`);
  const malformed = (what) => fail(`${file} malformed: ${what}`);
  let definition;
  try {
    definition = JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (error) {
    malformed(error.message);
  }
  const isMap = (value) => Boolean(value) && typeof value === 'object' && !Array.isArray(value);
  const distinct = (value, what, allowed, empty) => {
    if (!Array.isArray(value) || (!empty && value.length === 0) || new Set(value).size !== value.length || value.some((item) => typeof item !== 'string' || !allowed(item))) malformed(`${what} must be a list of distinct valid names`);
  };
  const keyed = (value, what, keys) => {
    if (!isMap(value) || stable(Object.keys(value).sort()) !== stable(keys.slice().sort())) malformed(`${what} keys must be exactly ${keys.join(', ')}`);
  };
  keyed(definition, 'definition', WORKFLOW_KEYS);
  if (definition.name !== name) malformed(`name must be ${name}`);
  const { phases } = definition;
  distinct(phases, 'phases', (phase) => /^[a-z]+(-[a-z]+)*$/.test(phase), false);
  keyed(definition.next, 'next', phases);
  phases.forEach((phase) => distinct(definition.next[phase], `next.${phase}`, (target) => phases.includes(target) || CHAIN_EXITS.includes(target), false));
  keyed(definition.reviewers, 'reviewers', REVIEW_NODES);
  keyed(definition.rollback_reset, 'rollback_reset', REVIEW_NODES);
  REVIEW_NODES.forEach((node) => {
    distinct(definition.reviewers[node], `reviewers.${node}`, (key) => /^[a-z]+$/.test(key), true);
    distinct(definition.rollback_reset[node], `rollback_reset.${node}`, (phase) => phases.includes(phase), true);
  });
  return definition;
}

/** The reviewer keys one review node accepts: the union of every definition's `reviewers[node]`. */
function reviewerKeys(node) {
  const names = fs.readdirSync(WORKFLOWS_DIR).filter((file) => file.endsWith('.json')).map((file) => file.slice(0, -'.json'.length));
  return [...new Set(names.flatMap((name) => loadWorkflow(name).reviewers[node]))];
}

/** Builds the spawn shape for a command: direct argv, or the Windows PowerShell JSON-stdin fallback. */
function buildInvocation(platform, command, args, viaPowerShell) {
  if (platform === 'win32' && (viaPowerShell || /\.(cmd|bat|ps1)$/i.test(command))) {
    return {
      file: 'powershell.exe',
      args: ['-NoLogo', '-NoProfile', '-NonInteractive', '-Command', '$ErrorActionPreference = "Stop"; try { $request = [Console]::In.ReadToEnd() | ConvertFrom-Json; $global:LASTEXITCODE = 0; & $request.command @($request.args); exit $LASTEXITCODE } catch { exit 1 }'],
      input: JSON.stringify({ command, args }),
    };
  }
  return { file: command, args };
}

function runLocal(command, args) {
  if (typeof command !== 'string' || command.length === 0 || command.includes('\0')) fail('command must be a non-empty executable path');
  stringArray(args, 'args');
  const direct = buildInvocation(process.platform, command, args);
  let result = direct.input === undefined ? spawnSync(direct.file, direct.args, {
    encoding: 'utf8', shell: false, timeout: PROBE_TIMEOUT_MS, windowsHide: true,
  }) : null;
  if (process.platform === 'win32' && (direct.input !== undefined || (result.error && result.error.code === 'ENOENT'))) {
    const plan = direct.input !== undefined ? direct : buildInvocation(process.platform, command, args, true);
    result = spawnSync(plan.file, plan.args, {
      encoding: 'utf8', input: plan.input, shell: false, timeout: PROBE_TIMEOUT_MS, windowsHide: true,
    });
  }
  return { ok: !result.error && result.status === 0 };
}

function defaultAuthArgs(provider) {
  if (provider === 'codex') return ['login', 'status'];
  if (provider === 'claude') return ['auth', 'status', '--json'];
  fail('provider must be codex or claude');
}

function readCache(cachePath, now) {
  if (!cachePath || !fs.existsSync(cachePath)) return { schema: CACHE_SCHEMA, entries: {} };
  let parsed;
  try {
    parsed = JSON.parse(fs.readFileSync(cachePath, 'utf8'));
  } catch (_) {
    return { schema: CACHE_SCHEMA, entries: {} };
  }
  if (!parsed || parsed.schema !== CACHE_SCHEMA || !parsed.entries || typeof parsed.entries !== 'object' || Array.isArray(parsed.entries)) return { schema: CACHE_SCHEMA, entries: {} };
  const cutoff = Number.isFinite(now) ? now : Date.now();
  const entries = {};
  for (const [key, entry] of Object.entries(parsed.entries)) {
    if (/^[a-f0-9]{64}$/.test(key) && entry && typeof entry.status === 'string' && Number.isFinite(entry.retry_after) && entry.retry_after > cutoff) entries[key] = { status: entry.status, retry_after: entry.retry_after };
  }
  return { schema: CACHE_SCHEMA, entries };
}

function writeCache(cachePath, cache) {
  if (!cachePath) return;
  fs.mkdirSync(path.dirname(cachePath), { recursive: true });
  fs.writeFileSync(cachePath, JSON.stringify(cache) + '\n', 'utf8');
}

function authGeneration(input) {
  if (typeof input.authGeneration === 'string') return input.authGeneration;
  if (typeof input.credentialPath !== 'string' || !fs.existsSync(input.credentialPath)) return 'unknown';
  try {
    const stat = fs.statSync(input.credentialPath);
    return `${stat.mtimeMs}:${stat.size}`;
  } catch (_) {
    return 'unknown';
  }
}

function cacheKey(input, authReady) {
  return fingerprint({ provider: input.provider, model: input.model, command: input.command, auth_args: defaultAuthArgs(input.provider), auth_ready: authReady, auth_generation: authGeneration(input) });
}

/** Checks executable and authentication locally without making a model request; reports the host platform External Review combines with its host shell to pick stdin syntax. */
function externalPreflight(input) {
  return { ...localReadiness(input), platform: process.platform };
}

function localReadiness(input) {
  if (!input || typeof input !== 'object') fail('preflight input required');
  const provider = input.provider;
  const model = input.model;
  if (provider !== 'codex' && provider !== 'claude') fail('provider must be codex or claude');
  if (input.authArgs !== undefined) fail('authArgs are not supported');
  if (typeof model !== 'string' || model.length === 0) fail('model required');
  const authArgs = defaultAuthArgs(provider);
  const version = runLocal(input.command, ['--version']);
  if (!version.ok) {
    return { status: 'command-unavailable', model_access: 'unknown', fingerprint: cacheKey(input, false) };
  }
  const auth = runLocal(input.command, authArgs);
  const key = cacheKey(input, auth.ok);
  if (!auth.ok) return { status: 'authentication-unavailable', model_access: 'unknown', fingerprint: key };
  const now = Number.isFinite(input.now) ? input.now : Date.now();
  const cached = readCache(input.cachePath, now).entries[key];
  if (cached) {
    return { status: 'negative-cache', reason: cached.status, retry_after: cached.retry_after, model_access: 'unknown', fingerprint: key };
  }
  return { status: 'local-ready', model_access: 'unknown', fingerprint: key };
}

/** Stores one sanitized external-model failure for later retry control: retry-after is the provider-reported reset, capped at one hour, and one hour when none was reported, so a quota never reads as reset minutes after it hit. */
function recordExternalFailure(input) {
  if (!input || !/^[a-f0-9]{64}$/.test(input.fingerprint || '')) fail('valid fingerprint required');
  if (!['authentication', 'quota', 'reachability', 'command'].includes(input.status)) fail('unsupported external failure status');
  const now = Number.isFinite(input.now) ? input.now : Date.now();
  const cap = now + MAX_NEGATIVE_TTL_MS;
  const reported = input.retryAfter === undefined || input.retryAfter === null ? cap : input.retryAfter;
  if (!Number.isFinite(reported) || reported <= now) fail('retryAfter outside bounded future');
  const retryAfter = Math.min(reported, cap);
  const cache = readCache(input.cachePath, now);
  cache.entries[input.fingerprint] = { status: input.status, retry_after: retryAfter };
  writeCache(input.cachePath, cache);
  return cache.entries[input.fingerprint];
}

/** Selects a task class without allowing a task to move to a weaker class. */
function routeTask(input) {
  if (!input || typeof input !== 'object') fail('routing input required');
  if (!['command', 'mechanical', 'standard'].includes(input.kind)) fail('routing kind invalid');
  if (typeof input.objectiveInputs !== 'boolean' || typeof input.failedObjectiveCheck !== 'boolean') fail('routing evidence incomplete');
  const risks = riskArray(input.risks, 'risks');
  const checks = stringArray(input.checks, 'checks');
  if (!Number.isInteger(input.correctionAttempts) || input.correctionAttempts < 0) fail('correctionAttempts invalid');
  const attempted = input.correctionAttempts;
  const ranks = { mechanical: 0, standard: 1, critical: 2 };
  if (input.priorTier !== undefined && !Object.prototype.hasOwnProperty.call(ranks, input.priorTier)) fail('priorTier invalid');
  const changeRisk = risks.find((risk) => risk.target === 'change');
  const artifactRisk = risks.find((risk) => risk.target === 'artifact');
  const deterministicCommand = input.kind === 'command' && input.objectiveInputs === true && checks.includes('deterministic-state');
  const mechanical = input.kind === 'mechanical' && input.objectiveInputs === true && checks.includes('deterministic-check') && checks.includes('exhaustive-match-validation');
  const computedTier = changeRisk || (input.failedObjectiveCheck && attempted >= 1) ? 'critical' : deterministicCommand || mechanical ? 'mechanical' : 'standard';
  const clamped = Boolean(input.priorTier) && ranks[input.priorTier] > ranks[computedTier];
  const tier = clamped ? input.priorTier : computedTier;
  const execution = deterministicCommand && tier === 'mechanical' && risks.length === 0 ? 'command' : 'agent';
  const reason = changeRisk ? `risk:${changeRisk.name}` : input.failedObjectiveCheck && attempted >= 1 ? 'failed-objective-check' : clamped ? 'no-downgrade' : artifactRisk ? `artifact-risk:${artifactRisk.name}` : execution === 'command' ? 'deterministic-command' : tier === 'mechanical' ? 'objective-mechanical' : 'normal';
  return { tier, execution, reason };
}

function rowsById(rows, expected, allowedNa, findings, label) {
  if (!Array.isArray(rows)) fail(`${label} rows must be an array`);
  const allowedStatuses = new Set(LEDGER_VOCABULARY[label]);
  const { p: naStatus, f: findingStatus } = LEDGER_VOCABULARY;
  const seen = new Set();
  for (const row of rows) {
    if (!row || typeof row.i !== 'string' || typeof row.s !== 'string') fail(`${label} row malformed`);
    if (!expected.has(row.i) || seen.has(row.i)) fail(`${label} row identity invalid: ${row.i}`);
    if (!allowedStatuses.has(row.s)) fail(`${label} status invalid: ${row.s}`);
    if (row.s === naStatus && (typeof row.p !== 'string' || !allowedNa.get(row.i).has(row.p))) fail(`${label} n/a predicate invalid: ${row.i}`);
    if (row.s !== naStatus && row.p !== undefined) fail(`${label} predicate only allowed for n/a: ${row.i}`);
    if (row.s === findingStatus && (typeof row.f !== 'string' || !findings.has(row.f))) fail(`${label} finding reference invalid: ${row.i}`);
    if (row.s !== findingStatus && row.f !== undefined) fail(`${label} finding reference only allowed for finding: ${row.i}`);
    seen.add(row.i);
  }
  if (seen.size !== expected.size) fail(`${label} rows incomplete`);
}

/** Returns the required manifest digest for a review coverage ledger: the manifest exactly as written, minus `digest` and the `ledger` skeleton, which carries the digest and so cannot be inside it. A manifest missing a published constant keeps the identity it was stamped with, so one written before that field existed still validates; a divergent one is digested as written and rejected on validation. */
function coverageManifestDigest(manifest) {
  if (!manifest || typeof manifest !== 'object') fail('manifest required');
  const copy = Object.assign({}, manifest);
  delete copy.digest;
  delete copy.ledger;
  return fingerprint(copy);
}

/** Validates one complete compact review ledger against phase-derived evidence. */
function validateCoverageLedger(manifest, ledger, actualFindings) {
  if (!manifest || !ledger) fail('manifest and ledger required');
  const digest = coverageManifestDigest(manifest);
  if (manifest.digest !== digest || ledger.manifest_digest !== digest) fail('ledger manifest identity invalid');
  if (manifest.vocabulary !== undefined && stable(manifest.vocabulary) !== stable(LEDGER_VOCABULARY)) fail('manifest vocabulary invalid');
  if (manifest.row_example !== undefined && stable(manifest.row_example) !== stable(LEDGER_ROW_EXAMPLE)) fail('manifest row example invalid');
  if (manifest.n_a_shape !== undefined && stable(manifest.n_a_shape) !== stable(LEDGER_NA_SHAPE)) fail('manifest n_a shape invalid');
  if (manifest.n_a !== undefined) {
    if (!manifest.n_a || typeof manifest.n_a !== 'object' || Array.isArray(manifest.n_a)) fail('manifest n_a invalid');
    for (const key of Object.keys(manifest.n_a)) if (!ROW_TYPES.includes(key)) fail(`manifest n_a unknown row type: ${key}`);
  }
  const ids = (name) => {
    if (!Array.isArray(manifest[name]) || manifest[name].some((item) => typeof item !== 'string')) fail(`manifest ${name} invalid`);
    const set = new Set(manifest[name]);
    if (set.size !== manifest[name].length) fail(`manifest ${name} duplicates`);
    return set;
  };
  const allowedNa = (label, expected) => {
    const entries = manifest.n_a && manifest.n_a[label] || {};
    if (!entries || typeof entries !== 'object' || Array.isArray(entries)) fail(`manifest n_a.${label} invalid`);
    const out = new Map();
    for (const id of expected) {
      const predicates = entries[id] || [];
      if (!Array.isArray(predicates) || predicates.some((value) => typeof value !== 'string' || value.length === 0)) fail(`manifest n_a.${label}.${id} invalid`);
      out.set(id, new Set(predicates));
    }
    for (const id of Object.keys(entries)) if (!expected.has(id)) fail(`manifest n_a.${label} unknown id: ${id}`);
    return out;
  };
  const findingIds = Array.isArray(ledger.findings) ? ledger.findings : [];
  if (!Array.isArray(actualFindings) || actualFindings.some((id) => typeof id !== 'string' || id.length === 0)) fail('actual findings invalid');
  if (findingIds.some((id) => typeof id !== 'string' || id.length === 0) || new Set(findingIds).size !== findingIds.length || stable(findingIds.slice().sort()) !== stable(actualFindings.slice().sort())) fail('ledger findings invalid');
  const findings = new Set(findingIds);
  const files = ids('files');
  const rules = ids('rules');
  const sections = ids('sections');
  rowsById(ledger.files, files, allowedNa('files', files), findings, 'files');
  rowsById(ledger.rules, rules, allowedNa('rules', rules), findings, 'rules');
  rowsById(ledger.sections || [], sections, allowedNa('sections', sections), findings, 'sections');
  return { ok: true };
}

/** Stamps every published constant into an emitted manifest, sets its digest, and adds the ledger skeleton the reviewer completes: the digest pre-filled, no findings, one row per manifest id with its status left to fill. */
function stampManifest(manifest) {
  const stamped = Object.assign({}, manifest, { vocabulary: LEDGER_VOCABULARY, row_example: LEDGER_ROW_EXAMPLE, n_a_shape: LEDGER_NA_SHAPE });
  const digest = coverageManifestDigest(stamped);
  const rows = (ids) => ids.map((id) => ({ i: id }));
  return Object.assign(stamped, { digest, ledger: { manifest_digest: digest, findings: [], files: rows(stamped.files), rules: rows(stamped.rules), sections: rows(stamped.sections) } });
}

/** Parses a reviewer's `## Review rubric`: rule ids are its `###` headings, else its bullets' bold lead-in labels; section ids are its `###` headings. */
function rubricIds(markdown) {
  if (typeof markdown !== 'string') fail('rubric markdown required');
  const rubric = markdown.replace(/\r\n/g, '\n').split(/^## Review rubric *$/m)[1];
  if (rubric === undefined) fail('reviewer has no ## Review rubric');
  const body = rubric.split(/^## /m)[0];
  const sections = [...body.matchAll(/^### (.+)$/gm)].map((match) => match[1].trim());
  const rules = sections.length > 0 ? sections : [...body.matchAll(/^- \*\*(.+?)\*\*/gm)].map((match) => match[1]);
  if (rules.length === 0) fail('reviewer rubric has no entries');
  return { rules, sections };
}

/** A UI surface: `.html`/`.htm` outside `.asd/` or under `.asd/templates/`, a stylesheet, component-framework or Unity UI Toolkit file, or any file under a `ui`/`components`/`views`/`pages` path segment, matched case-insensitively. */
function isUiSurface(file) {
  const lower = file.toLowerCase();
  if (/\.html?$/.test(lower)) return !lower.startsWith('.asd/') || lower.startsWith('.asd/templates/');
  return /\.(css|scss|less|jsx|tsx|vue|svelte|uxml|uss|tss)$/i.test(file) || /(^|\/)(ui|components|views|pages)\//i.test(file);
}

/** An executable file is anything that is not prose, config or markup, so an unrecognised extension keeps the performance sections reviewed. */
function isExecutable(file) {
  return !/\.(md|json|ya?ml|toml|html?|txt)$/i.test(file);
}

/** A templated artefact: basename equal to a template name (so `AGENTS.md`/`CLAUDE.md` at any depth), or a path under `.asd/templates/`, `docs/` or `.asd/sprints/`. */
function isTemplated(file, templates) {
  return templates.includes(file.split('/').pop()) || /^(\.asd\/templates\/|docs\/|\.asd\/sprints\/)/.test(file);
}

/** Every `t_<name>` file under a templates directory, at any depth, as `<name>`. */
function templateNames(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => (entry.isDirectory() ? templateNames(path.join(dir, entry.name)) : entry.name.startsWith('t_') ? [entry.name.slice(2)] : []));
}

/** A documentation file: prose (Markdown, reStructuredText, AsciiDoc, plain text) or a templated artefact, whatever its extension. */
function isDocumentation(file, templates) {
  return /\.(md|mdx|markdown|rst|adoc|txt)$/i.test(file) || isTemplated(file, templates);
}

/** Maps every rule id to the standing n/a predicates its condition authorizes for this dispatch; `parts` maps each reviewer whose rubric the manifest holds to its rule ids. */
function standingPredicates(input, parts, ids, customRules, templates) {
  const rubrics = Object.keys(parts);
  const granted = new Map(ids.map((id) => [id, []]));
  const targets = (key) => rubrics.flatMap((name) => NA_TARGETS[key][name] || []).map((prefix) => ids.find((id) => id.startsWith(prefix)) || fail(`rubric entry missing for n/a predicate: ${prefix}`));
  const grant = (key, predicate) => targets(key).forEach((id) => granted.get(id).push(predicate));
  const otherPhase = PHASES.find((phase) => phase !== input.phase);
  const hasBudgets = Object.entries(customRules).some(([file, text]) => file.endsWith('custom-coding-rules.md') && /^#+ [^\n]*perf[^\n]*budget/im.test(text));
  Object.keys(NA_TARGETS).forEach(targets);
  ids.filter((id) => id.includes(otherPhase) && !id.includes(input.phase)).forEach((id) => granted.get(id).push(NA_PREDICATES.phaseGate));
  if (!input.files.some((file) => /\.html?$/i.test(file))) grant('html', NA_PREDICATES.noHtml);
  if (input.selfHosting !== true) grant('selfHosting', NA_PREDICATES.noSelfHosting);
  if (!input.files.some((file) => isTemplated(file, templates))) grant('templated', NA_PREDICATES.noTemplated);
  if (rubrics.includes(COMBINED_REVIEWER) && !input.files.some((file) => isDocumentation(file, templates))) (parts.documentation || fail('combined manifest composes no documentation rubric')).forEach((id) => granted.get(id).push(NA_PREDICATES.noDocs));
  if (input.phase === 'design-review') {
    if (!input.files.some((file) => /(^|\/)(ux-spec\.html|design-md-delta\.yaml)$/.test(file))) grant('ui', NA_PREDICATES.phaseGate);
    return granted;
  }
  if (!input.files.some(isUiSurface)) grant('ui', NA_PREDICATES.uiSurface);
  if (!hasBudgets && !input.files.some(isExecutable)) grant('perf', NA_PREDICATES.perf);
  if (!hasBudgets) grant('budgetCompliance', NA_PREDICATES.noBudgets);
  return granted;
}

/** Emits one reviewer's stamped coverage manifest over its whole file list; `rubric` is its agent markdown, or a combined reviewer's `{reviewer: markdown}` map whose rubrics compose in order. */
function emitCoverageManifest(input) {
  if (!input || typeof input !== 'object') fail('emit input required');
  if (!PHASES.includes(input.phase)) fail('phase must be design-review or impl-review');
  const files = stringArray(input.files, 'files');
  const customRules = input.customRules || {};
  const composed = input.rubric !== null && typeof input.rubric === 'object' ? input.rubric : { [input.reviewer]: input.rubric };
  const rubrics = Object.values(composed).map(rubricIds);
  const rubric = { rules: rubrics.flatMap((part) => part.rules), sections: rubrics.flatMap((part) => part.sections) };
  const rules = rubric.rules.concat(Object.keys(customRules));
  if (new Set(rules).size !== rules.length) fail('rubric and custom-rule ids must be distinct');
  const templates = input.templates === undefined ? [] : stringArray(input.templates, 'templates');
  const granted = standingPredicates(input, Object.fromEntries(Object.keys(composed).map((name, i) => [name, rubrics[i].rules])), rules, customRules, templates);
  const naFor = (ids) => Object.fromEntries(ids.map((id) => [id, granted.get(id)]).filter(([, predicates]) => predicates.length > 0));
  const renamed = new Set(input.pureRenames === undefined ? [] : stringArray(input.pureRenames, 'pureRenames'));
  return stampManifest({
    reviewer: input.reviewer,
    phase: input.phase,
    files,
    rules,
    sections: rubric.sections,
    n_a: { files: Object.fromEntries(files.filter((file) => renamed.has(file)).map((file) => [file, [NA_PREDICATES.pureRename]])), rules: naFor(rules), sections: naFor(rubric.sections) },
  });
}

/** A test file: under a `test`/`tests`/`__tests__`/`spec`/`specs` path segment or a dotted one carrying it (`Core.Tests/`, `Game.Tests.Unit/`), or a basename in a common test naming convention. Heuristic, so Correctness's full list stays the backstop for a miss. */
function isTest(file) {
  return /(^|\/)([^/]*\.)?(test|tests|__tests__|spec|specs)(\.[^/]*)?\//i.test(file) || /\.(test|spec)\.|^test_|_test\.|Tests?\./.test(file.split('/').pop());
}

/** One reviewer's file list, the single selector every manifest is built from: impl-review Testing narrows to test files plus the explicitly passed test-plan paths, which the scope pathspec excludes; every other reviewer gets the whole scope. */
function reviewerFiles(phase, reviewer, files, testPlan) {
  if (phase !== 'impl-review' || reviewer !== 'testing') return files;
  return [...new Set(files.filter(isTest).concat(testPlan))];
}

/** Reads a reviewer ledger from bare JSON, or from the one fenced block carrying `manifest_digest` inside the reviewer's returned text. */
function ledgerFromText(text) {
  const parse = (candidate) => {
    try { return JSON.parse(candidate); } catch (_) { return undefined; }
  };
  const bare = parse(text);
  if (bare !== undefined) return bare;
  const blocks = [...text.matchAll(/^```[^\n]*\r?\n([\s\S]*?)^```/gm)].map((match) => parse(match[1])).filter((value) => value && typeof value === 'object' && value.manifest_digest !== undefined);
  if (blocks.length !== 1) fail('ledger must be JSON or returned text with exactly one fenced ledger block');
  return blocks[0];
}

/** Splits one markdown table row into trimmed cells, honouring `\|` escapes and dropping one enclosing backtick pair. */
function tableCells(line) {
  return line.trim().replace(/^\||\|$/g, '').split(/(?<!\\)\|/).map((cell) => cell.trim().replace(/\\\|/g, '|').replace(/^`([^`]*)`$/, '$1'));
}

/** A review's findings as `{id, severity, location}`, read by column position from its first table: `t_review.md`'s Findings or the external report's Kept findings, whose column order the templates fix while headings follow the docs language. A `—` id row is the empty table's placeholder. */
function reviewFindings(text) {
  const lines = text.split(/\r?\n/).map((line) => line.trim());
  const start = lines.findIndex((line) => line.startsWith('|'));
  if (start === -1) fail('review has no findings table');
  const end = lines.findIndex((line, i) => i > start && !line.startsWith('|'));
  const [header, separator, ...rows] = lines.slice(start, end === -1 ? lines.length : end).map(tableCells);
  if (header.length < 3 || separator === undefined || separator.length !== header.length || !separator.every((cell) => /^:?-+:?$/.test(cell))) fail('findings table header or separator malformed');
  const seen = new Set();
  return rows.filter((cells) => cells[0] !== '—').map((cells) => {
    const [id, severity, location] = cells;
    if (cells.length !== header.length || id === '' || seen.has(id)) fail(`findings row malformed or id repeated: ${cells.join(' | ')}`);
    if (!SEVERITIES.includes(severity)) fail(`finding ${id} severity must be one of ${SEVERITIES.join(', ')}: ${severity}`);
    seen.add(id);
    return { id, severity, location };
  });
}

/** Validates one reviewer's returned text and persists it, from its verdict token on, as `<reviewer>.md` (`<reviewer>.late.md` for a late return) plus `<stem>.findings.json`; an internal reviewer's ledger must first validate against its manifest (default `<outDir>/<reviewer>.manifest.json`). A written review is never overwritten, so the orchestrator's later appends survive. */
function persistReview(input) {
  const { phase, reviewer, text, outDir } = input;
  if (!REVIEW_NODES.includes(phase)) fail(`--phase must be ${REVIEW_NODES.join(' or ')}`);
  const keys = reviewerKeys(phase);
  if (!keys.includes(reviewer)) fail(`--reviewer must be one of ${keys.join(', ')}`);
  if (typeof text !== 'string' || typeof outDir !== 'string') fail('review text and output dir required');
  const start = /^[ \t]*\[REVIEW-/m.exec(text);
  if (start === null) fail('returned text carries no verdict token: an interrupted dispatch, not a verdict');
  const body = text.slice(start.index).trimStart();
  const skip = reviewer === EXTERNAL_REVIEWER ? '|APPROVE \\(skipped: .+\\)' : '';
  const token = new RegExp(`^\\[REVIEW-${phase}-${reviewer}\\]: (APPROVE|CONCERNS|FAIL${skip})$`).exec(body.split(/\r?\n/, 1)[0].trimEnd());
  if (token === null) fail(`first line must be [REVIEW-${phase}-${reviewer}]: APPROVE|CONCERNS|FAIL`);
  const verdict = token[1];
  const findings = verdict.startsWith('APPROVE (') ? [] : reviewFindings(body);
  if (!verdict.startsWith('APPROVE') && findings.length === 0) fail(`${verdict} verdict lists no finding`);
  if (verdict === 'APPROVE' && findings.length > 0) fail('APPROVE verdict lists findings');
  if (INTERNAL_REVIEWERS.includes(reviewer)) {
    const manifest = JSON.parse(fs.readFileSync(input.manifest || path.join(outDir, `${reviewer}.manifest.json`), 'utf8'));
    validateCoverageLedger(manifest, ledgerFromText(body), findings.map((finding) => finding.id));
  }
  const stem = path.join(outDir, input.late ? `${reviewer}.late` : reviewer);
  if (fs.existsSync(`${stem}.md`)) fail(`${stem}.md already persisted`);
  fs.writeFileSync(`${stem}.findings.json`, JSON.stringify(findings) + '\n', 'utf8');
  fs.writeFileSync(`${stem}.md`, body.endsWith('\n') ? body : `${body}\n`, 'utf8');
  return { token: verdict, findings };
}

/** Compares the code-defect identity sets (file path without line, runner failure line, failing test) of the last two impl-test entries that routed defects in a test plan's `Defects` table, a stalemate only when those entry numbers are consecutive; `D-N` ids and `impl-review` rows never take part. `digest` identifies the latest set, so a recorded answer can be keyed to it. */
function defectStalemate(markdown) {
  if (typeof markdown !== 'string') fail('test-plan markdown required');
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  const headings = lines.flatMap((line, i) => (/^## Defects\b/.test(line) ? [i + 1] : []));
  if (headings.length === 0) fail('test-plan has no ## Defects section');
  if (headings.length > 1) fail(`test-plan has ${headings.length} ## Defects sections at lines ${headings.join(', ')}`);
  const end = lines.findIndex((line, i) => i >= headings[0] && /^## /.test(line));
  const [header, separator, ...rows] = lines.slice(headings[0], end === -1 ? lines.length : end).flatMap((line, i) => (line.trim().startsWith('|') ? [{ at: `line ${headings[0] + i + 1}`, cells: tableCells(line) }] : []));
  if (header === undefined) fail(`line ${headings[0]}: Defects table missing`);
  const [entry, location, symptom, test] = ['Entry', 'Location', 'Symptom', 'Failing test'].map((name) => (header.cells.includes(name) ? header.cells.indexOf(name) : fail(`${header.at}: Defects table has no ${name} column`)));
  if (separator === undefined || separator.cells.length !== header.cells.length || !separator.cells.every((cell) => /^:?-+:?$/.test(cell))) fail(`${(separator || header).at}: Defects table separator malformed`);
  const byEntry = new Map();
  for (const { at, cells } of rows) {
    if (cells.length !== header.cells.length) fail(`${at}: Defects row malformed: ${cells.join(' | ')}`);
    if (cells[entry] === 'impl-review' || /^\{\{.*\}\}$/.test(cells[entry])) continue;
    if (!/^\d+$/.test(cells[entry])) fail(`${at}: Defects row Entry must be an Entry log number or impl-review: ${cells.join(' | ')}`);
    const tuples = byEntry.get(Number(cells[entry])) || new Set();
    tuples.add(stable([cells[location].replace(/:\d+(?::\d+)?$/, ''), cells[symptom], cells[test]]));
    byEntry.set(Number(cells[entry]), tuples);
  }
  const [latestEntry, previousEntry] = [...byEntry.keys()].sort((a, b) => b - a);
  if (latestEntry === undefined) return { stalemate: false, digest: null };
  const [latest, previous] = [latestEntry, previousEntry].map((key) => [...(byEntry.get(key) || [])].sort());
  return { stalemate: previousEntry === latestEntry - 1 && stable(latest) === stable(previous), digest: fingerprint(latest) };
}

function htmlText(fragment) {
  return fragment.replace(/<[^>]*>/g, '').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&amp;/g, '&').replace(/\s+/g, ' ').trim();
}

function retroRow(table, attributes, content, ordinal) {
  const id = `${table.prefix}-${ordinal}`;
  const declared = /(?:^|\s)id="([^"]*)"/.exec(attributes);
  if (declared !== null && declared[1] !== id) fail(`${table.section} row ${ordinal} declares id ${declared[1]}, expected ${id}`);
  const cells = [...content.matchAll(/<td\b[^>]*>([\s\S]*?)<\/td>/g)].map((cell) => cell[1]);
  if (cells.length !== table.cells) fail(`${table.section} row ${id} has ${cells.length} cells, expected ${table.cells}`);
  const actsOn = htmlText(cells[table.actsOn]);
  if (!ACTS_ON.includes(actsOn)) fail(`${table.section} row ${id} Acts on must be ${ACTS_ON.join(' or ')}: ${actsOn}`);
  return { id, acts_on: actsOn, guardrail: htmlText(cells[table.guardrail].replace(/<\/?code>/g, '`')), home: htmlText(cells[table.home]) };
}

/** Parses a retrospective's Action and Systemic-proposal rows: each id is `A-N`/`P-N` from its 1-based `<tbody>` ordinal, `covered by:` rows counted, so a retro with or without `<tr id>` yields the same ids; a declared id disagreeing with its ordinal fails. */
function retroRows(html) {
  if (typeof html !== 'string') fail('retrospective html required');
  return RETRO_TABLES.flatMap((table) => {
    const section = new RegExp(`<section id="${table.section}"[^>]*>([\\s\\S]*?)</section>`).exec(html);
    if (section === null) return table.required ? fail(`retrospective has no ${table.section} section`) : [];
    const body = /<tbody\b[^>]*>([\s\S]*?)<\/tbody>/.exec(section[1]);
    if (body === null) fail(`retrospective ${table.section} table has no tbody`);
    return [...body[1].matchAll(/<tr\b([^>]*)>([\s\S]*?)<\/tr>/g)].map((row, index) => retroRow(table, row[1], row[2], index + 1));
  });
}

/** Parses the retro backlog's one table, one row per `<NNN-slug>#A-N`/`#P-N` address; an unknown disposition, acting side or repeated address fails. */
function backlogRows(markdown) {
  if (typeof markdown !== 'string') fail('retro backlog markdown required');
  const lines = markdown.replace(/\r\n/g, '\n').split('\n').map((line) => line.trim());
  const headers = lines.flatMap((line, i) => (line.startsWith('|') && stable(tableCells(line)) === stable(BACKLOG_HEADER) ? [i] : []));
  if (headers.length !== 1) fail(`retro backlog must hold exactly one | ${BACKLOG_HEADER.join(' | ')} | table, found ${headers.length}`);
  const after = lines.slice(headers[0] + 1);
  const end = after.findIndex((line) => !line.startsWith('|'));
  const [separator, ...rows] = after.slice(0, end === -1 ? after.length : end).map(tableCells);
  if (separator === undefined || separator.length !== BACKLOG_HEADER.length || !separator.every((cell) => /^:?-+:?$/.test(cell))) fail('retro backlog table separator malformed');
  const seen = new Set();
  return rows.map((cells) => {
    const [row, actsOn, disposition, decidedIn, guardrail] = cells;
    if (cells.length !== BACKLOG_HEADER.length) fail(`retro backlog row malformed: ${cells.join(' | ')}`);
    if (!/^\d+-[a-z0-9-]+#[AP]-[1-9]\d*$/.test(row) || seen.has(row)) fail(`retro backlog Row invalid or repeated: ${row}`);
    if (!ACTS_ON.includes(actsOn)) fail(`retro backlog ${row} Acts on must be ${ACTS_ON.join(' or ')}: ${actsOn}`);
    if (!BACKLOG_DISPOSITIONS.includes(disposition)) fail(`retro backlog ${row} Disposition must be one of ${BACKLOG_DISPOSITIONS.join(', ')}: ${disposition}`);
    seen.add(row);
    return { row, acts_on: actsOn, disposition, decided_in: decidedIn, guardrail };
  });
}

function sprintPhase(dir) {
  const file = path.join(dir, 'state.json');
  let state;
  try {
    state = JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (error) {
    fail(`${file} unreadable: ${error.message}`);
  }
  if (!state || typeof state.phase !== 'string') fail(`${file} has no phase`);
  return state.phase;
}

/** The highest-numbered archived sprint that reached `done` with a retrospective, or null. */
function latestRetroSprint(archived) {
  if (!fs.existsSync(archived)) return null;
  const sprints = fs.readdirSync(archived, { withFileTypes: true }).filter((entry) => entry.isDirectory()).map((entry) => entry.name).sort((a, b) => b.localeCompare(a, 'en', { numeric: true }));
  return sprints.find((sprint) => fs.existsSync(path.join(archived, sprint, 'retrospective.html')) && sprintPhase(path.join(archived, sprint)) === 'done') || null;
}

/** Retro intake candidates as `{row, acts_on, guardrail, home}`: the latest closed retrospective's rows the backlog does not dispose, then the backlog's deferred rows as their retrospective states them (the backlog's Acts on/Guardrail are human-readable copies); `covered by:` rows drop. Outside a self-hosting project the latest retrospective's `asd` rows stay tagged `upstream: true`, proposals for the framework that are never dispositioned, while a deferred `asd` row drops. An absent backlog reads as empty. */
function retroCandidates(sprintsDir, backlogPath, selfHosting) {
  if (!fs.statSync(sprintsDir).isDirectory()) fail(`--sprints is not a directory: ${sprintsDir}`);
  const archived = path.join(sprintsDir, 'archived');
  const backlog = fs.existsSync(backlogPath) ? backlogRows(fs.readFileSync(backlogPath, 'utf8')) : [];
  const disposed = new Set(backlog.map((entry) => entry.row));
  const rowsOf = (sprint) => retroRows(fs.readFileSync(path.join(archived, sprint, 'retrospective.html'), 'utf8')).map((row) => ({ row: `${sprint}#${row.id}`, acts_on: row.acts_on, guardrail: row.guardrail, home: row.home }));
  const isOpen = (candidate) => !/^covered by:/i.test(candidate.guardrail);
  const isOwn = (candidate) => selfHosting || candidate.acts_on === 'consumer';
  const latest = latestRetroSprint(archived);
  const fresh = (latest === null ? [] : rowsOf(latest).filter((candidate) => !disposed.has(candidate.row) && isOpen(candidate))).map((candidate) => (isOwn(candidate) ? candidate : { ...candidate, upstream: true }));
  const deferred = backlog.filter((entry) => entry.disposition === 'deferred').map((entry) => rowsOf(entry.row.split('#')[0]).find((candidate) => candidate.row === entry.row) || fail(`retro backlog row not in its retrospective: ${entry.row}`));
  return fresh.concat(deferred.filter((candidate) => isOpen(candidate) && isOwn(candidate)));
}

function readFileList(file) {
  return fs.readFileSync(file, 'utf8').split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
}

/** Where an iteration directory keeps its snapshot copy of a draft: the draft's path mirrored under `snapshot/`, an absolute path with its root dropped; a path climbing out with `..` fails, so a copy never lands outside the snapshot. */
function snapshotCopyPath(iterationDir, file) {
  const normalized = path.normalize(file);
  const relative = path.isAbsolute(normalized) ? path.relative(path.parse(normalized).root, normalized) : normalized;
  if (relative === '..' || relative.startsWith(`..${path.sep}`)) fail(`draft path must not climb out of its tree: ${file}`);
  return path.join(iterationDir, 'snapshot', relative);
}

/** Copies each draft under the `snapshot/` directory of design-review iteration dir `out`, so the next iteration can diff against it, and returns the drafts whose content differs from their copy under the `previous` iteration dir; a missing copy counts as changed, so a missing snapshot widens scope rather than dropping a draft. */
function draftSnapshot(files, out, previous) {
  const changed = files.filter((file) => {
    const before = previous === undefined ? null : snapshotCopyPath(previous, file);
    return before === null || !fs.existsSync(before) || !fs.readFileSync(before).equals(fs.readFileSync(file));
  });
  files.forEach((file) => {
    const copy = snapshotCopyPath(out, file);
    fs.mkdirSync(path.dirname(copy), { recursive: true });
    fs.copyFileSync(file, copy);
  });
  return changed;
}

/** Added lines of a zero-context unified diff as `{file, line, text}`, each at its line number in the new file. */
function addedLines(diff) {
  const added = [];
  let file = null;
  let line = 0;
  let inHunk = false;
  diff.split('\n').forEach((text) => {
    if (text.startsWith('diff --git ')) inHunk = false;
    else if (!inHunk && text.startsWith('+++ ')) file = text.slice(4).replace(/\t.*$/, '');
    else if (text.startsWith('@@')) {
      inHunk = true;
      line = Number(/\+(\d+)/.exec(text)[1]);
    } else if (inHunk && text.startsWith('+')) {
      added.push({ file, line, text: text.slice(1) });
      line += 1;
    }
  });
  return added;
}

/** Work-history violations among the added lines of a staged agent-memory diff and the names of its newly added files, as `{path, line, token}`; `line` is null for a file name, whose `_` separators count as word breaks so `project_<ordinal>-topic.md` is caught. */
function memoryViolations(diff, names) {
  const scanned = names.map((name) => ({ file: name, line: null, text: name.replace(/_/g, ' ') })).concat(addedLines(diff));
  return scanned.flatMap(({ file, line, text }) => MEMORY_HISTORY_PATTERNS.map((pattern) => pattern.exec(text)).filter(Boolean).map((match) => ({ path: file, line, token: match[0] })));
}

/** Runs the work-history check over the staged agent-memory changes. */
function memoryCheck(flags) {
  if (Object.keys(flags).length > 0) fail('memory-check takes no flags');
  const diff = runGit(['diff', '--cached', '-U0', '--no-color', '--no-ext-diff', '--no-prefix', '--', AGENT_MEMORY_PATHSPEC]);
  const names = runGit(['diff', '--cached', '--name-only', '-z', '--diff-filter=ACR', '--', AGENT_MEMORY_PATHSPEC]).split('\0').filter(Boolean);
  return memoryViolations(diff, names);
}

/** Sums added plus deleted lines of `git diff --numstat -z -M` output over the listed paths, a rename counted at its destination; a binary file (`-`) and a pure rename (0 and 0) add nothing. */
function numstatLines(output, files) {
  const listed = new Set(stringArray(files, 'files'));
  const tokens = output.split('\0');
  let lines = 0;
  let at = 0;
  while (at < tokens.length - 1) {
    const entry = /^(-|\d+)\t(-|\d+)\t([\s\S]*)$/.exec(tokens[at]);
    if (!entry) fail(`numstat entry malformed: ${tokens[at]}`);
    const [, added, deleted, file] = entry;
    const isRename = file === '';
    if (added !== '-' && listed.has(isRename ? tokens[at + 2] : file)) lines += Number(added) + Number(deleted);
    at += isRename ? 3 : 1;
  }
  return lines;
}

/** Review waves a scope of `files` files, `lines` changed lines and `bytes` of diff needs: the most of one per WAVE_THRESHOLD_LINES, WAVE_THRESHOLD_FILES and WAVE_THRESHOLD_BYTES begun, at least one, at most MAX_REVIEW_WAVES and never more than `files`, so every wave can hold a file. */
function reviewWaveCount(lines, files, bytes = 0) {
  if (!Number.isInteger(lines) || lines < 0) fail('lines must be a non-negative integer');
  if (!Number.isInteger(files) || files < 0) fail('files must be a non-negative integer');
  if (!Number.isInteger(bytes) || bytes < 0) fail('bytes must be a non-negative integer');
  const needed = Math.max(Math.ceil(lines / WAVE_THRESHOLD_LINES), Math.ceil(files / WAVE_THRESHOLD_FILES), Math.ceil(bytes / WAVE_THRESHOLD_BYTES));
  return Math.min(MAX_REVIEW_WAVES, Math.max(1, files), Math.max(1, needed));
}

/** Accepts a review-wave division only as exactly `count` file lists, disjoint and together equal to the scope, so every scope file is reviewed in exactly one wave; a list may be empty only when the scope is, as the one wave `[[]]`. */
function validateWaveDivision(division, scope, count) {
  if (!Array.isArray(division) || division.length !== count) fail(`division must hold exactly ${count} waves`);
  const inScope = new Set(scope);
  const placed = new Set();
  division.forEach((wave, index) => {
    if (stringArray(wave, `division wave ${index + 1}`).length === 0 && inScope.size > 0) fail(`division wave ${index + 1} is empty`);
    wave.forEach((file) => {
      if (placed.has(file)) fail(`division places a file twice: ${file}`);
      placed.add(file);
    });
  });
  const outside = [...placed].find((file) => !inScope.has(file));
  if (outside !== undefined) fail(`division file outside the scope: ${outside}`);
  const unplaced = [...inScope].find((file) => !placed.has(file));
  if (unplaced !== undefined) fail(`scope file in no wave: ${unplaced}`);
  return division;
}

/** Runs git without a shell, returning its stdout, or streaming it into an open file descriptor so a large patch never hits a buffer cap; `okStatuses` admits git's documented non-error exits, such as `--no-index` reporting a difference with 1. */
function runGit(args, outFd, okStatuses = [0]) {
  const result = spawnSync('git', args, { encoding: 'utf8', maxBuffer: Infinity, stdio: ['ignore', outFd === undefined ? 'pipe' : outFd, 'pipe'], windowsHide: true });
  if (result.error || !okStatuses.includes(result.status)) fail(`git ${args.join(' ')} failed: ${result.error ? result.error.message : result.stderr.trim()}`);
  return result.stdout;
}

function gitRef(value, name) {
  if (typeof value !== 'string' || !/^[^-\s]\S*$/.test(value)) fail(`${name} <sha> required`);
  return value;
}

/** Every rename in the range, destination to source; `pure` only when git reports the identical blob and mode on both sides, so the rename is behaviour-neutral by proof, never by assertion. */
function rangeRenames(base, head) {
  const tokens = runGit(['diff', '--raw', '-z', '-M', '--no-abbrev', `${base}...${head}`]).split('\0');
  const renames = new Map();
  let at = 0;
  while (at < tokens.length - 1) {
    const [srcMode, dstMode, srcBlob, dstBlob, status] = tokens[at].slice(1).split(' ');
    if (status.startsWith('R')) renames.set(tokens[at + 2], { source: tokens[at + 1], pure: srcBlob === dstBlob && srcMode === dstMode });
    at += /^[RC]/.test(status) ? 3 : 2;
  }
  return renames;
}

/** A patch pathspec for files one range diffs, each rename paired with its source so the patch shows a rename rather than an add. */
function patchPaths(files, renames) {
  return files.flatMap((file) => (renames.has(file) ? [renames.get(file).source, file] : [file]));
}

/** An impl-review manifest's commit ranges: `--base...--head` for the scope, and `--full-base...--head` for `--full-files`, whose entries join the list and take that wider range even when also in the scope. Null without `--base/--head`. */
function manifestRanges(flags) {
  const hasRange = flags.base !== undefined || flags.head !== undefined;
  const hasFull = flags['full-files'] !== undefined || flags['full-base'] !== undefined;
  if (hasRange && flags.phase !== 'impl-review') fail('--base/--head apply to impl-review only');
  if (hasFull && !hasRange && (flags.snapshot === undefined || flags['full-base'] !== undefined)) fail('--full-files/--full-base need --base/--head; in design-review --full-files needs --snapshot and takes no --full-base');
  if (!hasRange) return null;
  const head = gitRef(flags.head, '--head');
  const base = gitRef(flags.base, '--base');
  const scopeRange = { base, head, renames: rangeRenames(base, head) };
  if (!hasFull) return { scope: scopeRange, full: null, fullFiles: new Set() };
  if (typeof flags['full-files'] !== 'string') fail('--full-files <path> required with --full-base');
  const fullBase = gitRef(flags['full-base'], '--full-base');
  return { scope: scopeRange, full: { base: fullBase, head, renames: rangeRenames(fullBase, head) }, fullFiles: new Set(readFileList(flags['full-files'])) };
}

/** A design-review manifest's snapshot diff source: the previous iteration dir, and the `--full-files` drafts left out of the diff so each is read whole. Null without `--snapshot`. */
function snapshotSource(flags) {
  if (flags.snapshot === undefined) return null;
  if (flags.phase !== 'design-review') fail('--snapshot applies to design-review only');
  return { dir: flags.snapshot, whole: new Set(flags['full-files'] === undefined ? [] : readFileList(flags['full-files'])) };
}

/** The range a listed file's patch hunks come from; a file on neither list, such as an appended test-plan path, takes the manifest range. */
function rangeOf(ranges, file) {
  return ranges.full !== null && ranges.fullFiles.has(file) ? ranges.full : ranges.scope;
}

/** Listed files each range renames with identical content and mode. */
function pureRenames(ranges, files) {
  if (ranges === null) return [];
  return files.filter((file) => {
    const rename = rangeOf(ranges, file).renames.get(file);
    return rename !== undefined && rename.pure;
  });
}

/** Git invocations writing one patch for `files`, each file's hunks over its own range. */
function rangePatchInvocations(ranges, files) {
  return [ranges.scope, ranges.full].filter((range) => range !== null).flatMap((range) => {
    const paths = patchPaths(files.filter((file) => rangeOf(ranges, file) === range), range.renames);
    return paths.length === 0 ? [] : [{ args: ['--literal-pathspecs', 'diff', '-M', '--no-color', '--no-ext-diff', '--no-textconv', `${range.base}...${range.head}`, '--', ...paths] }];
  });
}

/** Git invocations diffing each draft against its copy in the previous iteration's snapshot; a draft that snapshot lacks gets none. */
function snapshotPatchInvocations(previousDir, files) {
  return files.flatMap((file) => {
    const copy = snapshotCopyPath(previousDir, file);
    return fs.existsSync(copy) ? [{ args: ['diff', '--no-index', '--no-color', '--no-ext-diff', '--no-textconv', '--', copy, file], okStatuses: [0, 1] }] : [];
  });
}

/** Streams each git invocation's stdout, in order, into one freshly written file. */
function writeGitOutput(file, invocations) {
  const fd = fs.openSync(file, 'w');
  try {
    invocations.forEach((invocation) => runGit(invocation.args, fd, invocation.okStatuses));
  } finally {
    fs.closeSync(fd);
  }
}

/** Writes the diff of a manifest's file list into `dir` and returns its path: over the commit ranges in impl-review, against the previous snapshot in design-review, minus the drafts it leaves whole; null when there is nothing to diff against, as in design-review iteration 1. Named by the fingerprint of the git invocations producing it, so manifests sharing a list and range share one file; rewritten on every call, since those invocations name refs and paths rather than content and a reused directory may hold an older file under the same name, and renamed into place so it is never seen half-written. */
function writeManifestDiff(dir, files, ranges, snapshot) {
  if (ranges === null && snapshot === null) return null;
  const invocations = ranges !== null ? rangePatchInvocations(ranges, files) : snapshotPatchInvocations(snapshot.dir, files.filter((file) => !snapshot.whole.has(file)));
  const diff = path.join(dir, `${fingerprint(invocations).slice(0, 16)}.diff`);
  const partial = `${diff}.${process.pid}.tmp`;
  writeGitOutput(partial, invocations);
  fs.renameSync(partial, diff);
  return diff;
}

function positiveInteger(value, name) {
  if (typeof value !== 'string' || !/^[1-9]\d*$/.test(value)) fail(`${name} <n> must be a positive integer`);
  return Number(value);
}

/** Writes External Review's scope manifest and its diff: the same explicit list and precomputed change content an internal reviewer gets, with no rubric and no ledger. */
function emitExternalScope(flags, files, ranges, snapshot) {
  const isImplReview = flags.phase === 'impl-review';
  if (isImplReview && ranges === null) fail('--base/--head required for impl-review External Review');
  if (isImplReview !== (flags.wave !== undefined)) fail('--wave <k> is required for impl-review and applies to it only');
  const stem = path.join(flags.out, EXTERNAL_REVIEWER);
  const scope = Object.assign(
    { phase: flags.phase, iteration: positiveInteger(flags.iteration, '--iteration') },
    isImplReview ? { wave: positiveInteger(flags.wave, '--wave') } : {},
    { files, diff: writeManifestDiff(flags.out, files, ranges, snapshot) },
  );
  fs.writeFileSync(`${stem}.scope.json`, JSON.stringify(scope) + '\n', 'utf8');
  return scope.diff === null ? { scope: `${stem}.scope.json` } : { scope: `${stem}.scope.json`, diff: scope.diff };
}

/** Writes one internal reviewer's coverage manifest and its diff. */
function emitInternalManifest(flags, files, ranges, snapshot) {
  if (flags.iteration !== undefined || flags.wave !== undefined) fail(`--iteration/--wave apply to --reviewer ${EXTERNAL_REVIEWER} only`);
  const customPaths = typeof flags['custom-rules'] === 'string' ? flags['custom-rules'].split(',') : [];
  const testPlan = typeof flags['test-plan'] === 'string' ? flags['test-plan'].split(',') : [];
  const reviewed = reviewerFiles(flags.phase, flags.reviewer, files, testPlan);
  const rubricOf = (reviewer) => fs.readFileSync(path.join(__dirname, 'agents', `asd-reviewer-${reviewer}.md`), 'utf8');
  const manifest = emitCoverageManifest({
    reviewer: flags.reviewer,
    phase: flags.phase,
    rubric: flags.reviewer === COMBINED_REVIEWER ? Object.fromEntries(COMBINED_RUBRICS.concat(COMBINED_REVIEWER).map((reviewer) => [reviewer, rubricOf(reviewer)])) : rubricOf(flags.reviewer),
    files: reviewed,
    customRules: Object.fromEntries(customPaths.map((file) => [file, fs.readFileSync(file, 'utf8')])),
    selfHosting: flags['self-hosting'] === true,
    templates: templateNames(path.join(__dirname, 'templates')),
    pureRenames: pureRenames(ranges, reviewed),
  });
  const stem = path.join(flags.out, flags.reviewer);
  fs.writeFileSync(`${stem}.manifest.json`, JSON.stringify(manifest) + '\n', 'utf8');
  const diff = writeManifestDiff(flags.out, manifest.files, ranges, snapshot);
  return diff === null ? { manifest: `${stem}.manifest.json`, digest: manifest.digest } : { manifest: `${stem}.manifest.json`, digest: manifest.digest, diff };
}

function emitManifestCommand(flags) {
  if (typeof flags.files !== 'string' || typeof flags.out !== 'string') fail('--files <path> and --out <dir> required');
  if (!PHASES.includes(flags.phase)) fail('phase must be design-review or impl-review');
  const keys = reviewerKeys(flags.phase.replace(/-review$/, ''));
  if (!keys.includes(flags.reviewer)) fail(`--reviewer <name> required, one of ${keys.join(', ')}`);
  const snapshot = snapshotSource(flags);
  const ranges = manifestRanges(flags);
  const whole = ranges !== null ? ranges.fullFiles : snapshot !== null ? snapshot.whole : new Set();
  const files = [...new Set(readFileList(flags.files).concat([...whole]))];
  return flags.reviewer === EXTERNAL_REVIEWER ? emitExternalScope(flags, files, ranges, snapshot) : emitInternalManifest(flags, files, ranges, snapshot);
}

/** Bytes of the patch a reviewer reads for `files` over `base...head`, a rename paired with its source so a move is not counted as an add; zero for no files. */
function patchBytes(base, head, files) {
  const ranges = { scope: { base, head, renames: rangeRenames(base, head) }, full: null, fullFiles: new Set() };
  return rangePatchInvocations(ranges, files).reduce((total, invocation) => total + Buffer.byteLength(runGit(invocation.args)), 0);
}

function reviewWavesCommand(flags) {
  if (typeof flags.files !== 'string') fail('--files <path> required');
  if ((flags.division === undefined) !== (flags.out === undefined)) fail('--division <json path> and --out <path> go together');
  const base = gitRef(flags.base, '--base');
  const head = gitRef(flags.head, '--head');
  const scope = readFileList(flags.files);
  const files = new Set(scope).size;
  const lines = numstatLines(runGit(['diff', '--numstat', '-z', '-M', `${base}...${head}`]), scope);
  const bytes = patchBytes(base, head, scope);
  const thresholds = { lines: WAVE_THRESHOLD_LINES, files: WAVE_THRESHOLD_FILES, bytes: WAVE_THRESHOLD_BYTES };
  const measured = { lines, files, bytes, thresholds, waves: reviewWaveCount(lines, files, bytes) };
  if (flags.division === undefined) return measured;
  const waves = validateWaveDivision(JSON.parse(fs.readFileSync(flags.division, 'utf8')), scope, measured.waves);
  fs.mkdirSync(path.dirname(flags.out), { recursive: true });
  fs.writeFileSync(flags.out, JSON.stringify({ base, head, lines, files, bytes, thresholds, waves }) + '\n', 'utf8');
  return measured;
}

/** Wave `k`'s list from a `waves.json` division, each path a later commit renamed mapped to its destination at `head` (renames over the division's `head...head`), so a file renamed after the division is reviewed under its current path. */
function waveFiles(division, k, head) {
  if (!division || typeof division !== 'object' || !Array.isArray(division.waves)) fail('waves.json must hold a waves array');
  if (k > division.waves.length) fail(`--wave ${k} exceeds the ${division.waves.length} waves of the division`);
  const moved = new Map([...rangeRenames(gitRef(division.head, 'waves.json head'), head)].map(([destination, rename]) => [rename.source, destination]));
  return stringArray(division.waves[k - 1], `wave ${k}`).map((file) => moved.get(file) || file);
}

/** Writes a review wave's iteration-1 list: its current-path `waves.json` list unioned with the `--files` list, one path per line. */
function waveFilesCommand(flags) {
  if (typeof flags.waves !== 'string' || typeof flags.out !== 'string') fail('--waves <path> and --out <path> required');
  const listed = waveFiles(JSON.parse(fs.readFileSync(flags.waves, 'utf8')), positiveInteger(flags.wave, '--wave'), gitRef(flags.head, '--head'));
  const files = [...new Set(listed.concat(typeof flags.files === 'string' ? readFileList(flags.files) : []))];
  fs.writeFileSync(flags.out, files.map((file) => `${file}\n`).join(''), 'utf8');
  return { out: flags.out, files: files.length };
}

/** The git-ignored directory every helper file lives in, created on first use with a `.gitignore` of `*` so it ignores itself without a root `.gitignore` entry; returns its absolute path. */
function scratchDir() {
  const dir = path.join(__dirname, 'tmp');
  const ignore = path.join(dir, '.gitignore');
  fs.mkdirSync(dir, { recursive: true });
  if (!fs.existsSync(ignore)) fs.writeFileSync(ignore, '*\n', 'utf8');
  return dir;
}

/** What a liveness decision reads off a Claude subagent transcript (JSONL): when the agent started, when its newest still-unanswered tool call began (null when none is open), and whether its last message is a final assistant answer. An unparseable line, such as a half-written tail, is skipped. */
function transcriptProgress(text) {
  const parse = (line) => {
    try { return JSON.parse(line); } catch (_) { return null; }
  };
  const messages = text.split('\n').map(parse).filter((entry) => entry && (entry.type === 'assistant' || entry.type === 'user') && entry.message);
  const blocks = (entry, type) => (Array.isArray(entry.message.content) ? entry.message.content.filter((block) => block && block.type === type) : []);
  const answered = new Set(messages.flatMap((entry) => blocks(entry, 'tool_result').map((block) => block.tool_use_id)));
  const open = messages.flatMap((entry) => blocks(entry, 'tool_use').filter((block) => !answered.has(block.id)).map(() => Date.parse(entry.timestamp)));
  const last = messages[messages.length - 1];
  return {
    startedAt: messages.length === 0 ? NaN : Date.parse(messages[0].timestamp),
    openCallSince: open.length === 0 ? null : Math.max(...open),
    done: last !== undefined && last.type === 'assistant' && last.message.stop_reason === 'end_turn' && blocks(last, 'tool_use').length === 0,
  };
}

/** One liveness check of one agent, as `{status, reason?}`: `done` once its last message is a final answer; `stalled` when over `budgetMs` since it started (`over-budget`), or when its transcript neither grew nor changed mtime since `previous` while no tool call is open (`no-progress`) or its open call has passed the host's command ceiling (`tool-call-overrun`); `unobservable` when the transcript is missing on this and the previous check; else `running`. `previous` is the last check's `{size, mtimeMs}`, null when the transcript was missing then, undefined before the first check; `current` is `{size, mtimeMs, text}`, or null when missing. A first miss waits a check, since a just-dispatched agent may not have written its transcript yet. */
function agentLiveness(previous, current, now, budgetMs) {
  if (current === null) return previous === null ? { status: 'unobservable' } : { status: 'running' };
  const progress = transcriptProgress(current.text);
  if (progress.done) return { status: 'done' };
  if (budgetMs !== undefined && now - progress.startedAt > budgetMs) return { status: 'stalled', reason: 'over-budget' };
  if (!previous || previous.size !== current.size || previous.mtimeMs !== current.mtimeMs) return { status: 'running' };
  if (progress.openCallSince !== null && now - progress.openCallSince < TOOL_CALL_CEILING_MS) return { status: 'running' };
  return { status: 'stalled', reason: progress.openCallSince === null ? 'no-progress' : 'tool-call-overrun' };
}

/** The transcript of subagent `id` under `<projectsDir>/<project>/<session>/subagents/`, or null. */
function findTranscript(projectsDir, id) {
  if (!fs.existsSync(projectsDir)) return null;
  const subdirs = (dir) => fs.readdirSync(dir, { withFileTypes: true }).filter((entry) => entry.isDirectory()).map((entry) => path.join(dir, entry.name));
  return subdirs(projectsDir).flatMap(subdirs).map((session) => path.join(session, 'subagents', `agent-${id}.jsonl`)).find((file) => fs.existsSync(file)) || null;
}

function transcriptSnapshot(file) {
  if (file === null || !fs.existsSync(file)) return null;
  const stat = fs.statSync(file);
  return { size: stat.size, mtimeMs: stat.mtimeMs, text: fs.readFileSync(file, 'utf8') };
}

/** Checks each `--agents` Claude subagent's transcript, under `$CLAUDE_CONFIG_DIR/projects` when that is set and `~/.claude/projects` otherwise, every `--interval` seconds and prints one `STALL <id> <reason>` line per stalled agent, which it then stops watching; silent while agents progress, it returns once every agent is done or stalled. A transcript missing on two consecutive checks fails the command with a non-zero exit instead of a stall line, so a host whose transcripts it cannot find never gets a healthy agent stopped. */
async function agentLivenessCommand(flags) {
  if (typeof flags.agents !== 'string') fail('--agents <id,...> required');
  const ids = [...new Set(flags.agents.split(','))];
  const invalid = ids.find((id) => !/^[A-Za-z0-9_-]+$/.test(id));
  if (invalid !== undefined) fail(`--agents id invalid: ${invalid}`);
  const intervalMs = positiveInteger(flags.interval, '--interval') * 1000;
  const budgetMs = flags['budget-min'] === undefined ? undefined : positiveInteger(flags['budget-min'], '--budget-min') * 60000;
  const projectsDir = path.join(process.env.CLAUDE_CONFIG_DIR || path.join(os.homedir(), '.claude'), 'projects');
  const watched = new Map(ids.map((id) => [id, { file: null, previous: undefined }]));
  for (;;) {
    for (const [id, agent] of watched) {
      agent.file = agent.file || findTranscript(projectsDir, id);
      const current = transcriptSnapshot(agent.file);
      const verdict = agentLiveness(agent.previous, current, Date.now(), budgetMs);
      if (verdict.status === 'unobservable') fail(`agent-liveness: no transcript for agent ${id} under ${projectsDir} ($CLAUDE_CONFIG_DIR/projects, else ~/.claude/projects)`);
      if (verdict.status === 'stalled') process.stdout.write(`STALL ${id} ${verdict.reason}\n`);
      if (verdict.status !== 'running') watched.delete(id);
      agent.previous = current === null ? null : { size: current.size, mtimeMs: current.mtimeMs };
    }
    if (watched.size === 0) return 0;
    await delay(intervalMs);
  }
}

function parseFlagArgs(argv, booleanFlags) {
  const bools = booleanFlags || [];
  const out = {};
  for (let i = 0; i < argv.length; i += 1) {
    if (!argv[i].startsWith('--')) fail('flags require values');
    const name = argv[i].slice(2);
    if (bools.includes(name)) { out[name] = true; continue; }
    if (argv[i + 1] === undefined) fail('flags require values');
    out[name] = argv[i + 1];
    i += 1;
  }
  return out;
}

function inputJson(flags) {
  if (typeof flags.input !== 'string') fail('--input <path|-> required');
  return JSON.parse(flags.input === '-' ? fs.readFileSync(0, 'utf8') : fs.readFileSync(flags.input, 'utf8'));
}

async function main(argv) {
  const command = argv[2];
  const flags = parseFlagArgs(argv.slice(3), ['self-hosting', 'late']);
  if (command === 'manifest-digest') {
    process.stdout.write(coverageManifestDigest(JSON.parse(fs.readFileSync(flags.manifest, 'utf8'))) + '\n');
    return 0;
  }
  if (command === 'emit-manifest') {
    process.stdout.write(JSON.stringify(emitManifestCommand(flags)) + '\n');
    return 0;
  }
  if (command === 'validate-ledger') {
    const result = validateCoverageLedger(JSON.parse(fs.readFileSync(flags.manifest, 'utf8')), ledgerFromText(fs.readFileSync(flags.ledger, 'utf8')), JSON.parse(fs.readFileSync(flags.findings, 'utf8')));
    process.stdout.write(JSON.stringify(result) + '\n');
    return 0;
  }
  if (command === 'persist-review') {
    if (typeof flags.in !== 'string' || typeof flags['out-dir'] !== 'string') fail('--in <path> and --out-dir <iteration dir> required');
    const result = persistReview({ phase: flags.phase, reviewer: flags.reviewer, text: fs.readFileSync(flags.in, 'utf8'), outDir: flags['out-dir'], late: flags.late === true, manifest: flags.manifest });
    process.stdout.write(JSON.stringify(result) + '\n');
    return 0;
  }
  if (command === 'external-preflight') {
    const result = externalPreflight(inputJson(flags));
    process.stdout.write(JSON.stringify(result) + '\n');
    return result.status === 'local-ready' ? 0 : 1;
  }
  if (command === 'external-record-failure') {
    process.stdout.write(JSON.stringify(recordExternalFailure(inputJson(flags))) + '\n');
    return 0;
  }
  if (command === 'route-task') {
    process.stdout.write(JSON.stringify(routeTask(inputJson(flags))) + '\n');
    return 0;
  }
  if (command === 'defect-stalemate') {
    if (typeof flags.plan !== 'string') fail('--plan <path> required');
    process.stdout.write(JSON.stringify(defectStalemate(fs.readFileSync(flags.plan, 'utf8'))) + '\n');
    return 0;
  }
  if (command === 'memory-check') {
    const violations = memoryCheck(flags);
    process.stdout.write(JSON.stringify(violations) + '\n');
    return violations.length === 0 ? 0 : 1;
  }
  if (command === 'draft-snapshot') {
    if (typeof flags.files !== 'string' || typeof flags.out !== 'string') fail('--files <path> and --out <iteration dir> required');
    process.stdout.write(draftSnapshot(readFileList(flags.files), flags.out, flags.previous).map((file) => `${file}\n`).join(''));
    return 0;
  }
  if (command === 'review-waves') {
    process.stdout.write(JSON.stringify(reviewWavesCommand(flags)) + '\n');
    return 0;
  }
  if (command === 'wave-files') {
    process.stdout.write(JSON.stringify(waveFilesCommand(flags)) + '\n');
    return 0;
  }
  if (command === 'retro-candidates') {
    if (typeof flags.sprints !== 'string' || typeof flags.backlog !== 'string') fail('--sprints <dir> and --backlog <path> required');
    process.stdout.write(JSON.stringify(retroCandidates(flags.sprints, flags.backlog, flags['self-hosting'] === true)) + '\n');
    return 0;
  }
  if (command === 'scratch-dir') {
    process.stdout.write(`${scratchDir()}\n`);
    return 0;
  }
  if (command === 'agent-liveness') return agentLivenessCommand(flags);
  fail('usage: emit-manifest, manifest-digest, validate-ledger, persist-review, external-preflight, external-record-failure, route-task, defect-stalemate, memory-check, draft-snapshot, review-waves, wave-files, retro-candidates, scratch-dir, or agent-liveness');
}

if (require.main === module) {
  main(process.argv).then((code) => { process.exitCode = code; }, (error) => { process.stderr.write(`${error.message}\n`); process.exitCode = 2; });
}

module.exports = { AUDIT_BATCH_THRESHOLD_FILES, COMBINED_REVIEWER, EXTERNAL_REVIEWER, INTERNAL_REVIEWERS, LARGE_WAVE_FILES, LEDGER_NA_SHAPE, LEDGER_ROW_EXAMPLE, LEDGER_VOCABULARY, MAX_REVIEW_WAVES, NA_PREDICATES, WAVE_THRESHOLD_BYTES, WAVE_THRESHOLD_FILES, WAVE_THRESHOLD_LINES, agentLiveness, backlogRows, buildInvocation, coverageManifestDigest, defectStalemate, draftSnapshot, emitCoverageManifest, externalPreflight, isDocumentation, isTest, loadWorkflow, numstatLines, persistReview, recordExternalFailure, retroCandidates, retroRows, reviewFindings, reviewWaveCount, reviewerFiles, reviewerKeys, routeTask, validateCoverageLedger, validateWaveDivision, waveFiles, fingerprint };

---
name: no-shell-doc-review-method
description: How documentation review runs in this framework repo - no shell, manifest-driven ledger vocabulary (emitter-produced list plus one fingerprint-named .diff, review-policy.md "Scope hand-off"), and the defect shapes that actually pay off (acting-site scope contradicting the cited SSoT, partial mirror updates when a rule gains a trigger, sole-home claims wider than the code, authority bounds narrower than their only instance, a fix narrowing code while the file's second description of it stays wide, new failure branches missing from an exhaustive blocker list, a new record-and-carry rule bound only on its read side, a duty moved off an agent onto "the orchestrator" with no acting site, a universal grant claim leaving a sibling fallback branch unreachable, new in-body comments in Node sources and hooks, README FAQ answers left stale by a new feature, and agent-memory claims stale at HEAD or contradicting the writer's definition)
metadata:
  type: feedback
---

Dispatches give no Bash and no reviewer runs git (`review-policy.md` "Scope hand-off"). The manifest's file
list is the only normative scope (ledger file rows, valid finding locations); the `<fingerprint>.diff`
`emit-manifest` writes beside it for exactly that list is the change content - impl-review every iteration,
design-review from the second iteration (none on the first: read each draft whole). Whole files = context only.
Intent behind a hunk: the sprint's `audit.md` "Gaps" and `plan.md` task lines cite `file:line`.
The impl-test decisions-log entry often carries "Tester notes for impl-review" naming drift worth checking.
Ledger statuses, `n/a` predicates and `p`/`f` placement come off the dispatched manifest's
`vocabulary`/`n_a` fields, copied byte-identically; `review-policy.md` "Coverage ledger" is the shape rule
only. Files-row vocabulary is `checked`/`n/a` with no `finding` status, so a file carrying a
finding is still `checked` and the finding id hangs off the rules row (one `f` per row: spread findings
over the rubric ids they best fit, or merge same-kind drift into one finding). An empty `n_a` means no row
may be `n/a` at all. Sha256 freshness (`upstream_hashes`) cannot be recomputed - say it was corroborated
structurally. The return is persisted by `runtime.js persist-review`, which parses the
Findings table by column position: keep the `#` cell a bare id and the Severity cell exactly
`low|medium|high|critical`. Later iterations: the decisions-log
"impl fix for <id>: findings resolved" entry names what changed (impl-review
<id> = `wave-<K>/iter-NN`); other iterations' `reviews/` files, any wave's, stay unread - glob only the
current `reviews/impl/wave-<K>/iter-NN/` (design: `reviews/design/iter-NN/`), and scope greps to canon
(a grep over the sprint folder hits sibling reviewers' files).

**Why:** a Claude reviewer holds no command-runner grant and no reviewer runs git (write scope and its
policy bound: `review-policy.md` "Gate Verdict Format"), and an invalid ledger is not a verdict - the
phase rejects and re-dispatches.

**How to apply — the highest-yield checks in this repo:**
- **Acting-site scope vs cited SSoT.** A rule doc's branch and its binding in `.asd/workflows/asd-phase-*.md`
  must agree on *reach* and on quoted literals (e.g. a scope step's log line dropping the SSoT's `<doc>`;
  the test only checks the substring). Read the step header, not only the bullet. A new agent mode is
  usually written twice - `artifact-layout.md`'s grant and the agent's own mode paragraph - with different
  reach (e.g. the impl-review in-place tester, "live rows its findings name" vs "risk and added-test rows").
- **A duty moved off an agent needs a new acting site.** "Never run X — the orchestrator runs it" is only
  true if a workflow/skill step says so; grep X across workflows and skills (e.g. UX losing
  `designmd-install`, with no design workflow or `asd-design-system` step picking it up).
- **A rule gaining a second trigger/site leaves unnamed mirrors stale.** Grep the old attribution phrase
  across the whole phase's files - sibling steps AND the skill `description` (always-loaded, in no
  manifest). Valid under the change-surface exception (change made unchanged text wrong). A new reviewer
  or workflow variant: grep README for "always dispatched", web-grant lists and "one reviewer per concern".
- **README FAQ vs a new feature.** A feature adding an exception to a stated invariant leaves the FAQ that
  asks exactly that question stale (e.g. a per-sprint document skip vs "Can I skip PRD/UX-spec/ADR/C4 for a
  lean sprint?"). Grep README FAQ for the feature's question, not only tables and folder map.
- **Record-and-carry rules: check both sides.** When a rule says "iteration X records list L, next
  iteration reads L", find who writes L in every case the rule names. Anything keyed to "the reviewer's
  ledger" misses External Review, which returns no ledger. Also check step order at the reader: e.g.
  "next impl-test entry performs the removal" vs impl-test step 4 rotating the live tables away
  before step 5 reads them.
- **Universal grant claims vs fallback branches.** A sentence saying the host serves *every* X a tool
  leaves a sibling "an X with no tool" branch dead; check agent frontmatter (e.g. every
  `memory: project` agent keeps `Write`, yet the MEMORY-FIX fallback stayed).
- **Accepted flagged choices vs exhaustive lists.** A decisions-log "Accepted flagged choices" line that
  routes a new failure must land in the dispatching workflow's closed enumerations.
- **Sole-home claims wider than their home.** Check "lives only in X" against the code and grep for
  restatements. New facts added to both a rule's prose and `checkpoints.md`'s gate table are a common
  two-home duplication (e.g. a gate's `dispatches` field stated in both).
- **Authority bounds narrower than their only instance.** A sanctioned writer's "limited to X" vs what the
  one script actually does. Read the code.
- **Fix narrows code, file's other description stays wide.** Grep the file header for every statement of it.
- **In-body comments in Node sources** (`tests/run.js`, `.asd/runtime.js`, `.asd/hooks/*.js`): the dev
  agents leave them (`// ponytail:` or plain narration); each is §7 high. Grep the diff for `^\+\s+// `
  AND `^\+.*\S\s+// ` (trailing comment after an expression, e.g. in a hook script); in `tests/run.js` keep
  only hits inside tests named for the current sprint, and in `.asd/runtime.js` only inside functions the diff adds. A member
  doc citing a sprint-plan anchor ("t1-contract") is the same finding.
- Migration comments in `.asd/migrations/*.js` follow the `6.0.0.js` precedent (member docs carrying WHY);
  that style alone is not a §7 finding.
- **Agent-memory files are reviewable source** (`artifact-layout.md` "Agent memory"): verify durable claims
  against HEAD AND against the writer's own definition. A stale line in *your own* memory in scope is
  raised like any other finding; its fix comes in your later memory-fix dispatch (`review-policy.md`
  "Autofix vs escalation"), not mid-review. Writing new memory while reviewing stays allowed; the
  orchestrator commits it with the review file ("Diff reachability"). A memory line is in Documentation economy's Reach. A sibling
  memory written in the same diff range as a code fix often cites the defect that range already fixed
  (e.g. testing memory vs a `tests/run.js` AC message) - check each memory "re-read X" against the diff.
  When a rule changes, grep ALL `.claude/agent-memory/` for the old wording: a sibling memory outside the
  manifest list stays stale (e.g. a correctness memory kept "return `QUESTION`") - pass it on as a note only.
- The documentation-economy preserve-list keeps per-case tables whole: a logically subsumed clause in a
  table row is not a cut candidate.
- The session-start AGENTS.md/CLAUDE.md snapshot in context can predate the branch's last sync - grep the
  file before raising managed-block drift.
- A sprint plan's accepted rewording (e.g. "never compact mid-gate" → "write gate answer first",
  since host compaction is automatic) is not an AC violation; grep `plan.md` before raising.

# Artifact Layout

## Optional documents

`prd.html`/`ux-spec.html`/`adr.html`/`c4-full`+`c4/` and `audit.md` are omitted entirely (sprint draft, promoted persistent doc, and any promotion folder) when their `documents.*` flag is disabled — see `sprint-lifecycle.md` "Optional documents" for the flags, defaults, and no-op phase rule. A disabled document is never written as an empty placeholder file. The subsystem registry and `<subsystem>.md` are no `documents.*` document: they follow `project.subsystem_decomposition` alone ("Subsystem registry"), and `/asd-init`'s empty registry seed is not such a placeholder.

## Subsystem decomposition modes

Set by `project.subsystem_decomposition` in config (`enabled` | `disabled`). Layout differs.

## Paths (decomposition enabled)

```
<repo root>/
├── .asd/
│   ├── rules/
│   ├── templates/
│   ├── project/
│   │   ├── config.yaml
│   │   ├── commands.yaml
│   │   ├── custom-common-rules.md
│   │   ├── custom-design-rules.md
│   │   ├── custom-coding-rules.md
│   │   ├── retro-backlog.md                 # lazy, "Retro backlog"
│   │   └── stubs.md
│   ├── tmp/                                 # git-ignored helper files, "Scratch directory"
│   └── sprints/
│       ├── <NNN-slug>/
│       │   ├── sprint.md
│       │   ├── state.json
│       │   ├── decisions-log.md
│       │   ├── decisions-log.NNN.md          # rotated segments, "Decisions log"
│       │   ├── audit.md
│       │   ├── design/
│       │   │   ├── prd.html
│       │   │   ├── ux-spec.html
│       │   │   ├── adr.html             # sprint-scoped only; never a standalone persistent document (folds at design-promote)
│       │   │   ├── design-md-delta.yaml
│       │   │   └── c4-full/                     # diagram_tool not none only; delta patch vs persistent diagram; full schema only when it is absent; never build dist/ here
│       │   │       # likec4: model/*.c4, views.c4 · mermaid: subsystems.md
│       │   ├── plan.md
│       │   ├── test-plan.md
│       │   ├── test-plan.entry-NN.md          # rotated narrative segments, "Test plan"
│       │   ├── manual-steps.md
│       │   ├── friction-log.md
│       │   ├── timing.jsonl                   # orchestrator-owned; written only via runtime.js timing*, sprint-lifecycle.md "Operation timing"; archived with the sprint
│       │   ├── retrospective.html
│       │   └── reviews/
│       │       ├── design/iter-NN/<reviewer>.md, <reviewer>.late.md
│       │       ├── impl/waves.json                # review-wave division, sprint-lifecycle.md "Review iteration counters"
│       │       └── impl/wave-<K>/iter-NN/<reviewer>.md, <reviewer>.late.md   # legacy impl/iter-NN/ read as wave 1
│       └── archived/<NNN-slug>/
├── .claude/{agents/, skills/, hooks/}                  # generated provider view
├── .claude/settings.json, .codex/hooks.json            # JSON-merge: ASD owns only its hook entry (providers.md)
├── .claude/agent-memory/<agent>/                       # hand-authored, never generated — see "Agent memory"
├── .codex/{agents/, hooks/}                            # generated provider view
├── .agents/skills/<name>/SKILL.md                      # generated provider view (Codex reads skills only here)
├── docs/
│   ├── product/
│   │   ├── concept.html
│   │   └── requirements/<subsystem>.html
│   ├── architecture/
│   │   ├── stack.html
│   │   ├── subsystems.md                # sole subsystem registry; mermaid mode: + inline diagram
│   │   ├── <subsystem>.md               # purpose + key paths, one per registered subsystem
│   │   ├── c4/                          # diagram_tool likec4 only: model/*.c4, views.c4 (dist/ is gitignored build output)
│   │   └── tech-reference/<tech>-<version>.md
│   └── ux/
│       ├── DESIGN.md
│       ├── design-system.html
│       ├── accessibility.html
│       └── <subsystem>.html             # ux-spec per subsystem
└── CLAUDE.md
```

A sprint folder holds **only** the artifacts named above, plus the per-reviewer coverage evidence `review-policy.md` "Coverage ledger" persists beside each `<reviewer>.md`. Nothing else is written under `.asd/sprints/<NNN-slug>/` — the tree is archived read-only at closure, so any stray file is frozen there and lost to whoever wrote it.

## Paths (decomposition disabled)

`docs/` becomes flat:

```
docs/
├── product/{concept.html, requirements.html}
├── architecture/
│   ├── stack.html
│   └── tech-reference/<tech>-<version>.md
└── ux/{DESIGN.md, design-system.html, accessibility.html, ux-spec.html}
```

No registry, no `<subsystem>.md`, no `c4/` directory. No subsystem subfolders.

## Agent memory

Agent memory lives at the provider-view root — `.claude/agent-memory/<agent>/` (`MEMORY.md` index + one file per memory) — always, whatever working directory a dispatch names. Never inside a sprint tree (path map above). One directory per dispatched agent name, tier variants included: distinct agents never share a memory file, so co-authorship arises only between concurrent dispatches of the same agent, which share that directory and its single `MEMORY.md`. Before a write lands, the writing agent checks it against its own definition (declared tool policy: `providers.md` "Role-scoped context"); a practice that contradicts the definition is never recorded.

**Content**: memory holds method only; it never records a sprint id, a Task, a wave, an iteration or a review verdict — that history belongs in the sprint's artefacts. Checked at the orchestrator's memory commit: `git-strategy.md` "Commit before review".

**Carve-out to the read-only generated-view rule**: `agent-memory/` has no canonical source under `.asd/` and `sync.js` neither generates nor reconciles it (no row in `providers.md` "Canonical path -> per-provider path"), so the read-only rule does not reach it. Everything else under `.claude/`, `.codex/` and `.agents/skills/` stays read-only — edit canon, then sync.

**In the review surface, both modes**: agent memory is hand-authored source, not generated output, so it is never an exclusion in any review scope — `self_hosting` enabled or disabled alike. Sole statement of the property; `sprint-lifecycle.md` "Self-hosting", `external-review.md` "Phase-scoped payload" and `t_prompt-external-impl.md` cite it, never restate it. A memory file loads on every dispatch of its agent, so a false line in one is paid again per dispatch until a review catches it. How such a write reaches a reviewed diff at all: `review-policy.md` "Change-surface rule".

**Leftover-term check**: a sprint that removes a mechanism or term searches, at its first `impl-test` entry, for every remaining mention of it anywhere in the repo, `.claude/agent-memory/**` included, directories of no longer existing agents too. The check pins the exact sentences and terms the sprint removed, taken from its own diff's removed lines, never a free-phrasing regex.

## Scratch directory

`.asd/tmp/` holds every helper file a workflow or agent writes that is no artifact — path lists, the review-wave division, release notes, the reviewer return file (`review-policy.md` "Coverage ledger") — never OS temp, never a sprint folder. Resolve it with `node .asd/runtime.js scratch-dir`, which creates it on first use and prints its absolute path. It ignores itself (its own `.gitignore` holds `*`), so nothing in it is committed or reviewed.

## Subsystem registry

When decomposition enabled, `docs/architecture/subsystems.md` (`t_subsystems.md`) is the sole subsystem registry — which subsystems exist and their ids — whatever `project.diagram_tool`. Each entry links `docs/architecture/<id>.md` (`t_subsystem.md`: purpose, key paths), and every registered subsystem has one. Reserved, never a subsystem id (they collide in flat `docs/architecture/`): `subsystems`, `stack`, `c4`, `tech-reference`.

`/asd-init` seeds the registry empty; only Architect fills it. A subsystem is added at `design-promote` (`sprint-lifecycle.md` "Design-promote phase"), or at `audit` when the registry is absent (`sprint-lifecycle.md` "Audit phase") — each addition with explicit user approval (hard gate).

Diagram per `project.diagram_tool`; with `none`, no diagram is written and `docs/architecture/c4/` is never created:

- **likec4**: `c4/model/*.c4`, `views.c4` (LikeC4 DSL) — diagram source only; container/component ids match registry ids. `likec4 build` produces `dist/` interactive HTML — build output, gitignored, never committed; run the `commands.yaml` `c4-build` command to render.
- **mermaid**: a Mermaid C4 block inline in `subsystems.md`. No `c4/` folder, no build output.

## Document provenance

User-facing artifacts may carry a `provenance` frontmatter field:

- `original` (default) — designed within an ASD sprint from scratch
- `reverse-engineered` — built from existing code without source docs
- `migrated` — translated from an external doc in another format/location

When `provenance != original`, set `source: <path or URL>`. HTML shows a provenance badge. Reviewers apply lighter checks on absent alternatives for non-original docs.

## Document representation rule

User-facing artifacts are HTML only. No parallel Markdown source. Exceptions:

- `DESIGN.md` — Google Labs format (YAML + Markdown), machine source for the design system. Spec: https://github.com/google-labs-code/design.md — agents fetch current spec from upstream when creating/editing it.
- `commands.yaml` — machine source, not user-facing
- LikeC4 `.c4` files — DSL source
- `docs/architecture/subsystems.md`, `<subsystem>.md` — agent-facing registry, read by `audit` and `plan` to locate subsystem code

`design-system.html` generated from DESIGN.md by the Documentation agent: all tokens/rules with live examples (color swatches with hex, typography samples, spacing scale, component previews). Regenerated when DESIGN.md changes.

## HTML shell wrapping (mandatory)

Every user-facing HTML artifact (prd, ux-spec, adr, concept, stack, accessibility, design-system, retrospective) MUST be wrapped in `t_html-shell.html`. The fragment template (`t_prd.html`, …) supplies the `<section>` content filling `{{CONTENT}}`. Creators emit a complete HTML document, not a bare fragment. Each artifact stays a **self-contained single file** — no `docs/assets/*` stylesheet or other sibling-file dependency; the shell inlines its own `<style>`.

The shell trims two blocks per document instead of always emitting them: the mermaid CDN script (only when the fragment actually contains a diagram) and the auto-TOC nav (only when the fragment has enough sections to need one). Both are ordinary computed placeholders, filled by the creator at write time — see table below.

**Placeholder fill** — creators compute and inline when writing:

| Placeholder | Source / value |
|---|---|
| `{{DOC_TYPE}}` | one of `PRD`, `ADR`, `UX-spec`, `Concept`, `Stack`, `Accessibility`, `Design-system`, `Retrospective` |
| `{{SUBSYSTEM}}` | subsystem id when persistent per-subsystem; `sprint` for any sprint-scoped artifact (drafts, `retrospective.html`); `project` for project-wide docs |
| `{{SPRINT_ID}}` | active `state.json.sprint_id` for any sprint-scoped artifact (drafts, `retrospective.html`); empty for persistent docs |
| `{{STATUS}}` | `draft` (design) / `in-review` (design-review) / `approved` (post design-promote, `lite`'s unreviewed write from the accepted implementation included) / `locked` (archived); `final` for a terminal report with no draft/review lifecycle (`retrospective.html`). `adr.html` is a set of decisions (one `<article>` each, `t_adr.html` "repeat this article per decision") — `{{STATUS}}` here is this document-lifecycle value, not an individual ADR's `proposed`/`accepted` status, which lives solely on that ADR's `.status-chip` |
| `{{UPDATED_AT}}` | ISO date (YYYY-MM-DD) of last write |
| `{{RESPONSIBILITY}}` | the `owns:` line from the fragment's responsibility frontmatter |
| `{{PROVENANCE}}` | `original` \| `reverse-engineered` \| `migrated` (from fragment frontmatter) |
| `{{SOURCE}}` | the `source:` field; empty when provenance=original |
| `{{SOURCE_SUFFIX}}` | ` (from {{SOURCE}})` in the provenance badge when source non-empty; else empty |
| `{{TITLE}}` | doc title — e.g. `PRD — Sprint 001 · <slug>` or `ADRs — Sprint 001 · <slug>` (set-level; ADR doc holds one or more decisions, so no single decision title fits doc-level `{{TITLE}}` — the individual decision title lives on each ADR's own `<h2>`/chip inside `{{CONTENT}}`) |
| `{{STATS}}` | doc-type chip strip; PRD sprint draft (`SUBSYSTEM=sprint`): `N stories · N AC · updated …`; PRD persistent doc: `N goals · N stories · N AC · N non-goals · updated …`; ADR: `N decisions · subsystems · updated …` (set-level; per-ADR status/subsystem stays on that ADR's own chips, not doc-level STATS); UX-spec: `N flows · N mockups`; Stack: `N langs · N frameworks · N components`; others: at least `updated …` |
| `{{TOC_NAV}}` | full `<nav class="toc">…</nav>` block (title + `<ol>` of links to each `<section id>` in fragment order, auto-generated from `<h2>` text) when the fragment has **3 or more** `<h2>` sections; empty string below that threshold — the nav is omitted entirely, not emitted empty. The shell derives the two-column layout purely from this via CSS (`.layout:has(> nav.toc)`) — no separate layout-class placeholder needed. For a multi-ADR `adr.html` (one `<article class="adr">` per decision), each decision's `<h2 id="adr-{{N}}-title">` (the per-decision title heading, first child after `.meta`) is the TOC-entry source — one entry per ADR article (linking `#adr-{{N}}-title`), not one entry per `<h2>` inside it. That article's own Context/Decision/Consequences/… headings are `<h3>`, below the TOC's `<h2>`-counting threshold, and stay unlinked in the TOC — the id-prefixing already disambiguates them for direct anchors |
| `{{MERMAID_SCRIPT}}` | the mermaid CDN `<script src>` + init `<script>` tag pair when `{{CONTENT}}` contains a `.mermaid` diagram block; empty string when the fragment has no diagram |
| `{{CONTENT}}` | fragment body (everything after the frontmatter comment) |
| `{{GENERATED_BY}}` | `ASD workflow` |
| `{{GENERATED_AT}}` | same as `{{UPDATED_AT}}` |

**Badge omission**: omit the provenance badge when `PROVENANCE == original` (do not emit the `<span class="provenance-original">` block).

**Fragment invariants**: fragment files in `.asd/templates/` (`t_prd.html` etc.) must NOT include `<html>`, `<head>`, `<body>`, `<style>`, `<script>` — content-only, rely on shell for chrome/styling. Reviewers FAIL fragments that duplicate shell chrome.

## Review verdict placeholder namespace

The verdict-token line in `t_review.md`/`t_review-report.md` (`[REVIEW-{{REVIEW_PHASE}}-{{REVIEWER}}]: ...`) uses `{{REVIEW_PHASE}}`, a distinct placeholder from core.md's `{{PHASE}}` template variable (full phase name, e.g. `impl-review`). `{{REVIEW_PHASE}}` is restricted to `design` \| `impl` only (`review-policy.md` verdict-token format) — literal substitution of `{{PHASE}}` there would emit an unparseable `impl-review`/`design-review` token. Dispatching workflow fills `{{REVIEW_PHASE}}` from its own phase (`impl-review`→`impl`, `design-review`→`design`), never copies `{{PHASE}}` verbatim.

## Tech reference docs (mandatory for every chosen tech)

`docs/architecture/tech-reference/<tech>-<version>.md` per `t_tech-reference.md`. Owner: Architect. Created for every chosen library, framework, runtime, external service. Includes canonical source URL, API surface used, version specifics, deprecations, project conventions.

**Refuse-to-implement rule**: Dev, Tester MUST verify `tech-reference/<tech>-<version>.md` exists before implementing with a tech. If missing → emit `FAILED — tech-reference missing for <tech>@<version>` and request it from Architect. No implementation without verified reference.

## Manual steps

`<sprint>/manual-steps.md` per `t_manual-steps.md`. Per-sprint, created lazily. Owners: dev agents (append entries); main orchestrator (validates necessity).

Manual step = operational action a human must perform for the plan to complete (provision a secret, create a cloud resource, hand-run a migration, set an env var, register a third-party account). NOT a code stub (`stubs.md`) nor manual QA verification (reviews `testing.md`).

- When a subtask cannot proceed without a human-only operational action, the dev appends an `MS-N` entry (full step-by-step instructions + a `Verification` field) and marks the subtask `BLOCKED: MS-N` in `plan.md`.
- `Verification` mandatory: states how the workflow confirms the action was done (a `commands.yaml` check, observable state, or explicit user confirmation).
- The main orchestrator validates every new entry before the phase halts. Kept only when the action genuinely cannot be done autonomously (needs access, a secret, an external account, an authority the agent lacks). Else rejected, returned to the dev to implement directly.
- Status `pending` → `done`. The registering dev flips to `done` only after running `Verification`.
- Sprint-scoped; archived with the sprint.

## Test plan

`<sprint>/test-plan.md` per `t_test-plan.md`. Per-sprint: entry 1 writes it fresh; every re-entry amends it (Defects section carried over with resolved entries kept for the record). Owner: the `impl-test` Tester — it alone appends the `Entry log` and rotates. A `Defects` row's `Status` is flipped by its test-fix fixer; for a memory `D-N`, by the orchestrator (`review-policy.md` "Autofix vs escalation"). A review-fix tester (`asd-phase-impl.md` step 5) amends only `Risk → check decisions` and `Added tests` rows. It never deletes a test: it records a removal finding as a `Risk → check decisions` row, and the next `impl-test` entry, which always follows `impl`, performs the removal and records its reason under `Removed tests`. An impl-review in-place tester (`review-policy.md` "Low-severity test-only findings") has the same reach and reports a finding outside it unfixed.

**Rotation**: at a re-entry's strategy pass, before any new row and never when resuming an interrupted current entry (`asd-phase-impl-test.md` step 1), the Tester moves the `Risk → check decisions`, `Removed tests` and `Added tests` rows of the previous `Entry log` entry N, if any, plus the review-fix and impl-review in-place tester rows added since (they have no `Entry log` row of their own), into `test-plan.entry-NN.md` (same section headings, N zero-padded to 2) when that file is absent, leaving those tables empty in the live file. Live `test-plan.md` keeps the `Entry log`, `Suite run`, `Defects` and `Manual verification`. A segment is never edited: a fix that changes a rotated row's risk gets a superseding row in the live file. Readers: "Decisions log" below.

SSoT for two things invisible in the diff: **why** a test was removed, and **why** a change needed no new test. Also the handoff channel for code defects to `impl` test-fix mode (`Defects` section). Not a task list (that is `plan.md`) and not a review verdict (that is `reviews/impl/wave-<K>/iter-NN/testing.md`).

**Manual verification — single home.** The optional `Manual verification` table (AC, steps, expected observation) is authored only here, by the Tester, when automation is impossible (visual UI, third-party live integration, ux feel). The user smoke check of its rows runs at `impl-test`'s first green entry (`asd-phase-impl-test.md`), and its result is recorded in this file, where reviewers read it. No review file duplicates or re-authors this spec; `asd-reviewer-testing` judges whether the spec is justified and reports any result as an ordinary finding, never as a persisted section of its own.

## Friction log

`<sprint>/friction-log.md` per `t_friction-log.md`. Workflow/machine Markdown — same class as `plan.md`, `test-plan.md`, `manual-steps.md`, so the HTML-only representation rule above does not reach it. Owner: the dispatching phase workflow (appends). Scope, entry format, boundary against adjacent owners and writer mechanism are normative in `sprint-lifecycle.md` "Friction log".

## Retrospective

`<sprint>/retrospective.html` per `t_retrospective.html`. User-facing HTML, shell-wrapped like every other. **Derived analysis, never a rendering of the log** — references `F-N` ids, never a second copy of the entries. Owner: `retro` phase; semantics in `sprint-lifecycle.md` "Retro phase".

## Retro backlog

`.asd/project/retro-backlog.md` per `t_retro-backlog.md`: the cross-sprint disposition of each retro row, one line per row, updated in place. Owner: main orchestrator. Created lazily at the first retro-intake write — `asd-init` seeds none, no migration creates it, `/asd-update` never touches it. Semantics: `sprint-lifecycle.md` "Retro intake".

## Single Source of Truth (iron rule)

Each fact has exactly one home file. Other files link to it, never copy. Violation = `FAIL` from Documentation reviewer.

## Documentation economy (iron rule)

A line of agent-facing text is re-paid on every dispatch that loads it, so it earns its place only by changing what a reading agent does; one that does not is deleted, not shortened. Reach: framework canon (rules, agents, skills, workflows, templates) and every artifact a later agent reads (`audit.md`, `plan.md`, `test-plan.md`, decisions-log entries, review files, retrospectives) — through the templates, in every consumer project too. **removal** decides and is necessary: cut only when no agent acts differently without the line. **provenance** — traces to no recorded defect, friction entry or decision — and **enforcement** — a tool grant, validator or suite assertion already imposes it, so the prose only restates it — corroborate a cut removal already allows; neither authorises one alone, and removal false = keep whatever the other two say. The tests bind while authoring: every agent that writes such text applies them before the line lands, not only when it is reviewed. Violation = `FAIL` from Documentation reviewer.

Cut on sight: prose stating no rule; rationale for a rule already stated; an example disambiguating nothing; a prohibition that only negates a positive rule already stated beside it; emphasis so frequent it marks nothing; a fact whose home is another file (SSoT above).

Never cut, whatever the length: text whose exact form is the contract (machine-parsed token, validated literal, parsed grammar); an enumeration whose completeness is the rule (a predicate's members, a per-role or per-case table); a case distinction a shorter phrasing collapses; a non-obvious failure mode stated with its symptom; normative text at its home stating a gate, safety boundary, ownership assignment, precondition or recovery duty — including a standalone safety, security, authority or irreversible-action prohibition with no positive rule beside it. Length is never the test.

## Document responsibility

Every template in `.asd/templates/` MUST declare its responsibility in frontmatter:

```yaml
---
responsibility:
  owns: <SSoT scope>
  excludes: <what belongs elsewhere>
  delegates_to: <other docs>
---
```

Agents preserve the block. Reviewers verify content respects the declared scope.

## Naming

- kebab-case English filenames
- Sprint slug derived from scope, max 30 chars
- Sprint number zero-padded to 3 digits
- ADR numbering is sprint-local (`ADR-1`, `ADR-2`, …) inside `<sprint>/design/adr.html` — unique only within the sprint, may repeat across sprints; ADRs are never promoted as a standalone persistent document, so no persistent ADR filename convention exists

## Sprint archival

Archived path: `.asd/sprints/archived/<NNN-slug>/`. The archive move rides the next sprint's branch; the sequence is owned by `sprint-lifecycle.md` "PR phase".

## State file

`<sprint>/state.json` carries only the keys `t_state.json` defines — machine state, never prose. `gate_decisions[].reason` and `.evidence` are short refs (a path, id or one-line pointer), not narrative; the narrative goes to `decisions-log.md`.

## Decisions log

Every user or adaptive orchestrator decision appends one entry to `<sprint>/decisions-log.md`. Per-sprint file, created at `scope` from `t_decisions-log.md`, archived with the sprint. Owner: main orchestrator. Append-only, never edited or removed. Entry format and durability rule are normative in `t_decisions-log.md`.

**Rotation**: before delegating a phase skill whose phase differs from `state.json.phase`, `asd-sprint` renames the live `decisions-log.md` to `decisions-log.NNN.md` (next ordinal, zero-padded to 3) when it holds an entry, recreates the live file from `t_decisions-log.md`, and commits both — unless both phases are in {`impl`, `impl-test`, `impl-review`}, so the cycle rotates once on entry (plan→impl, a rollback re-entry included) and once on exit (impl-review → its successor). A resume or re-run of the phase in `state.json.phase` never rotates, nor does a transition inside that cycle, so a within-cycle reader — the interrupted-attempt count (`review-policy.md` "Interrupted dispatch"), the failed-dispatch routing line (`sprint-lifecycle.md` "State recovery"), an impl-test stalemate answer — reads the live file alone.

**Readers of both rotated files**: a current-fact reader reads the live file; a cross-span reader reads every segment in ordinal order, then the live file. No segment present = a legacy single file, read as is.

**Legacy log**: `.asd/project/decisions-log.md` is historical only — the project-wide log used before this rule, frozen as of sprint `002-lean-workflow`. Never appended to again.

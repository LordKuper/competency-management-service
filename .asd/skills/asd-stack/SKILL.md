---
{
  "name": "asd-stack",
  "description": "Forms or edits the project tech stack document at docs/architecture/stack.html via asd-architect, branching by silent detection into one of four flows (clean slate / constraints / clear stack / brownfield extraction). Verifies versions via WebFetch, runs knowledge-gap analysis, and maintains a tech-reference doc per chosen tech. Use when the user runs /asd-stack, when asd-init or asd-concept detects a missing stack.html and suggests this skill, or when the user asks to define, draft, refine, edit, upgrade, or reverse-engineer the project technology stack.",
  "claude": {
    "allowed-tools": "Read Glob Grep AskUserQuestion Task"
  }
}
---

# ASD Stack

## Preconditions
- `.asd/project/config.yaml` exists (run `/asd-init` first)
- `docs/product/concept.html` exists (run `/asd-concept` first; concept is mandatory input)
- No active sprint required

## Operations used
- Read files — `.asd/project/config.yaml`, concept.html, existing stack.html, manifests, source files
- Search repo — silent scan for brownfield signals (manifests, lockfiles, Dockerfile, CI configs)
- Request user decision/input — variant choice, constraints, section approvals, lock-in/revise loop
- Delegate to agents — `asd-architect` (author, fetch external doc to verify versions, create tech-references)

## Phase 1 — silent detection (NO asking)

Scan in order:
1. `docs/architecture/stack.html` exists, non-empty → mode = **edit**, skip to Edit-mode flow
2. Manifests / lockfiles / Dockerfile / CI configs detected → brownfield candidate (default variant D)
3. No code, no manifests → greenfield candidate (no default)
4. Continue to Phase 2

## Phase 2 — variant choice (only if Phase 1 did not route to edit)

Request user decision (4 options):
- **A** — Clean slate, architect proposes from concept
- **B** — I have constraints (language, runtime, hosting, budget)
- **C** — Clear stack, will describe
- **D** — Brownfield, extract from existing code

Phase 1 brownfield candidates auto-suggest D as default.

## Phase 3 — flow per variant (each delegates to `asd-architect`)

**Variant A — clean slate**
- Delegate to agent `asd-architect` with payload: concept.html, language settings, target = `t_stack.html`
- Architect reads concept (vision, target users, value prop, declared constraints)
- Proposes 2-3 candidate combinations per relevant section
- Request user decision: pick combination or request alternatives
- Proceed to Phase 4

**Variant B — constrained**
- Request user input, multi-field one-shot:
  - Primary language preference (free-text or "no preference")
  - Runtime/platform target (web / desktop / mobile / server / cli / embedded / no preference)
  - Hosting preference (cloud / self-host / local-only / no preference)
  - Budget tier (zero-cost / low / standard / no constraint)
- Delegate to agent `asd-architect` with concept + constraints
- Architect proposes within constraints
- Proceed to Phase 4

**Variant C — clear stack**
- Request user input: describe the chosen stack
- Delegate to agent `asd-architect` with concept + that description
- Architect validates compatibility with concept; flags conflicts (e.g., "concept implies mobile but stack is server-only")
- Architect fills unstated gaps per `t_stack.html` (proposes defaults; user approves)
- Proceed to Phase 4

**Variant D — brownfield extraction**
- Delegate to agent `asd-architect` with manifest/source paths (repo-search results)
- Architect extracts stack content from package.json / Cargo.toml / pyproject.toml / go.mod / Makefile / Dockerfile / CI configs
- Draft sets `provenance: reverse-engineered` + `source: <primary manifest>` in frontmatter
- Proceed to Phase 4

## Phase 4 — convergence (universal across variants)

- Create mode only (target file does not yet exist): before the first section, write skeleton `docs/architecture/stack.html` per `t_stack.html`, with placeholder sections. Edit mode (target file already exists) skips this write and enters the per-section loop directly against the existing file.
- Section-by-section:
  - Per entry: verify current latest version via fetching external doc; flag if user's choice lags or is ahead
  - Architect drafts the section, translates to `language.docs`, writes it into `docs/architecture/stack.html` on disk
  - Post the file path + a short delta summary of what the section now says in `language.chat` (never the full section body) per `language-policy.md`
  - Apply `checkpoints.md`: adaptive pass is allowed only for already-authorized factual/routine content; new or material stack direction remains a hard decision.
  - on B: collect feedback, architect revises, rewrites the section in place, re-posts delta summary, re-ask
  - on C: remove that section (heading + placeholder content) from the on-disk file entirely, then continue
  - repeat until A
  - next section per `t_stack.html` order

## Phase 5 — knowledge gap analysis

Per technology entry in approved stack:
- Architect compares chosen version against LLM training cutoff
- Risk:
  - **LOW** — version pre-dates cutoff, well-known
  - **MEDIUM** — version close to or shortly after cutoff
  - **HIGH** — version released after cutoff; may have breaking changes LLM does not know
- MEDIUM/HIGH: architect fetches official changelog / release notes (external doc); records breaking changes and deprecations in tech-reference doc

## Phase 6 — tech-reference creation/update

Per technology in approved stack:
- Architect creates or updates `docs/architecture/tech-reference/<tech>-<version>.md` per `t_tech-reference.md`
- Includes canonical source URL, API surface used, version-specific notes, deprecations, project conventions, "Last verified" ISO date
- Request user decision before each persistent write

## Phase 7 — final artifact-level gate: stack.html

- `docs/architecture/stack.html` already reflects every locked-in section from Phase 4 (content write is not deferred — only this gate's `accept` is)
- write-then-review-accept per `checkpoints.md` mechanic (delta summary includes risk summary): post the path + a combined delta summary in chat; user replies `accept` (advance) or feedback (revise in place, re-post) — feedback naming a specific section may re-enter Phase 4 for that section before rewriting
- if feedback changes a tech entry or its version (not just prose in an already-chosen section): after Phase 4's re-entry, also re-run Phases 5-6 for the affected tech entries (including the per-tech approve-before-write micro-gate) before the delta summary is re-posted and before COMPLETED can be emitted
- emit COMPLETED

## Phase 8 — handoff

- Print handoff suggestion: "Next: run `/asd-sprint` to start the first sprint"
- NO auto-dispatch

## Edit mode (Phase 1 routed here)

- Show existing stack summary + risk re-assessment (versions may have aged since last write)
- Request user decision: multi-select which sections to edit; OR "refresh — re-verify all versions"
- per chosen section: enter Phase 4 loop
- Phase 5-8 as usual

## User-input request shapes

- Multi-field one-shot (constraints) → request as a single multi-field input
- Single-choice branching (A/B/C/D, lock-in/revise) → request as an options choice
- Never mix the two shapes in one request

## Hard rules

- NEVER guess a version — always verify via fetching external doc (architect handles)
- Only add tech to stack when actively integrated, not speculatively
- Speculative additions belong in stack.html's own `#considered` section (`t_stack.html` "Considered / not adopted") or `.asd/project/stubs.md`, not the main stack list (ADRs are sprint-scoped and lost at archival — not a durable home)
- Every tech in stack MUST have a matching tech-reference doc before COMPLETED

## Artefacts produced
- `docs/architecture/stack.html` (created, edited, or reverse-engineered)
- `docs/architecture/tech-reference/<tech>-<version>.md` for every chosen tech

## Agents dispatched
- `asd-architect` (author / scanner / version verifier / tech-reference creator)

## Skills dispatched
None.

## Return contract (single line)
```
STACK: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|D|edit> | TECHS: <count> | RISK: <low|medium|high> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References
- `.asd/templates/t_stack.html` (required vs optional sections — SSoT)
- `.asd/templates/t_tech-reference.md`
- `.asd/rules/core.md`
- `.asd/rules/language-policy.md`
- `.asd/rules/artifact-layout.md` (stack.html path, tech-reference path, provenance)

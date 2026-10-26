---
{
  "name": "asd-stack",
  "description": "Forms or edits the project tech stack document at docs/architecture/stack.html inline, branching by silent detection into one of four flows (clean slate / constraints / clear stack / brownfield extraction). Verifies versions via WebFetch, runs knowledge-gap analysis, and maintains a tech-reference doc per chosen tech. Use when the user runs /asd-stack, when asd-init or asd-concept detects a missing stack.html and suggests this skill, or when the user asks to define, draft, refine, edit, upgrade, or reverse-engineer the project technology stack.",
  "claude": {
    "allowed-tools": "Read Write Edit Glob Grep WebFetch WebSearch AskUserQuestion Bash(node .asd/runtime.js:*)"
  }
}
---

# ASD Stack

## Purpose

Create or edit `docs/architecture/stack.html` and its tech-reference docs, authored by the main orchestrator itself.

## Preconditions

```json
{"requires": [".asd/project/config.yaml exists", "docs/product/concept.html exists", "no active sprint required"], "else": "no config: run /asd-init first; no concept: run /asd-concept first"}
```

## Modes

```json
{"edit": "Phase 1 step 1", "fresh": "Phase 2 variant A|B|C", "reverse-engineered": "Phase 2 variant D"}
```

## Steps

Build the orchestrator bundle first and read it: `node .asd/runtime.js context --role orchestrator --phase stack --out <scratch-dir>/orchestrator-stack.context.md` (`agent-protocol.md` "Dispatch payload").

### Phase 1 — silent detection (NO asking)

Scan in order:
1. `docs/architecture/stack.html` exists, non-empty → mode = **edit**, skip to Edit-mode flow
2. Manifests / lockfiles / Dockerfile / CI configs detected → brownfield candidate (default variant D)
3. No code, no manifests → greenfield candidate (no default)
4. Continue to Phase 2

### Phase 2 — variant choice (only if Phase 1 did not route to edit)

Request user decision (a brownfield candidate defaults D):
- **A** — Clean slate, propose from the concept
- **B** — I have constraints (language, runtime, hosting, budget)
- **C** — Clear stack, will describe
- **D** — Brownfield, extract from existing code

### Phase 3 — flow per variant

Read `concept.html` first (vision, target users, value prop, declared constraints).

**A — clean slate**
- Propose 2-3 candidate combinations per relevant section; request user decision: pick or request alternatives

**B — constrained**
- Request user input, multi-field one-shot: primary language, runtime/platform target (web / desktop / mobile / server / cli / embedded), hosting (cloud / self-host / local-only), budget tier (zero-cost / low / standard), each with "no preference"
- Propose within the constraints

**C — clear stack**
- Request user input: the chosen stack; validate against the concept and flag conflicts (e.g. concept implies mobile, stack is server-only); fill unstated gaps per `t_stack.html` with proposed defaults the user approves

**D — brownfield extraction**
- Extract from package.json / Cargo.toml / pyproject.toml / go.mod / Makefile / Dockerfile / CI configs; frontmatter `provenance: reverse-engineered`, `source: <primary manifest>`

### Phase 4 — convergence (universal across variants)

- Create mode only (file absent): write a skeleton per `t_stack.html` with placeholder sections first. Edit mode enters the loop against the existing file.
- Per section, per `core.md` "Incremental writing": per entry, verify the current latest version from external docs and flag a lagging or leading choice; draft, translate to `language.docs`, write to disk, post the path and a short delta summary in `language.chat` (never the body, `language-policy.md`).
  - Apply `checkpoints.md`: an adaptive pass only for already-authorized factual content; new or material stack direction stays a hard decision. Otherwise request **Lock in / Revise this section / Skip**.
  - Revise: collect feedback, rewrite in place, re-post. Skip: remove the section (heading and placeholder) from the file.
  - Next section per `t_stack.html` order.

### Phase 5 — knowledge gap analysis

Per technology entry compare the chosen version with the training cutoff: **LOW** pre-dates it, well-known; **MEDIUM** close to or shortly after it; **HIGH** released after it, possibly breaking. MEDIUM/HIGH: fetch the official changelog or release notes, record breaking changes and deprecations in the tech-reference doc.

### Phase 6 — tech-reference creation/update

Per technology create or update `docs/architecture/tech-reference/<tech>-<version>.md` per `t_tech-reference.md` and `artifact-layout.md` "Tech reference docs". Request user decision before each persistent write: approve | revise.

### Phase 7 — final artifact-level gate: stack.html

- The file already holds every locked-in section. Write-then-review-accept per `checkpoints.md` (delta summary includes the risk summary): `accept` advances, feedback revises in place (a feedback naming a section may re-enter Phase 4 for it)
- Feedback changing a tech entry or its version re-runs Phases 5-6 for the affected entries (per-tech approve micro-gate included) before the delta summary is re-posted and before COMPLETED
- emit COMPLETED

### Phase 8 — handoff

- Print: "Next: run `/asd-sprint` to start the first sprint". NO auto-dispatch

### Edit mode (Phase 1 routed here)

- Show the existing stack summary with a risk re-assessment (versions age); request user decision: multi-select sections to edit, or "refresh — re-verify all versions"
- Per chosen section enter Phase 4; Phases 5-8 as usual

## Rules

- never: guess a version — always verify from external docs
- never: add tech speculatively — only when actively integrated; speculative additions belong in stack.html's own `#considered` section (`t_stack.html` "Considered / not adopted") or `.asd/project/stubs.md`, not the main stack list (ADRs are sprint-scoped and lost at archival)
- must: give every tech in stack a matching tech-reference doc before COMPLETED
- never: mix the multi-field and options request shapes in one request.

## Delegates

```json
{"agents": [], "skills": []}
```

## Outputs

```json
["docs/architecture/stack.html (created, edited, or reverse-engineered)", "docs/architecture/tech-reference/<tech>-<version>.md for every chosen tech"]
```

## Return

```
SKILL: asd-stack | MODE: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|D|edit> | TECHS: <count> | RISK: <low|medium|high> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References

- `.asd/templates/t_stack.html` (required vs optional sections — SSoT)
- `.asd/templates/t_tech-reference.md`
- `.asd/rules/core.md`
- `.asd/rules/language-policy.md`
- `.asd/rules/artifact-layout.md` (stack.html path, tech-reference path, provenance)

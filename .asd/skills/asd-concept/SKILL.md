---
{
  "name": "asd-concept",
  "description": "Forms or edits the project concept document inline, branching by silent detection into one of four flows (no idea / vague idea / clear vision / brownfield extraction) and converging through a per-section lock-in loop. Use when the user runs /asd-concept, when asd-init detects a missing concept.html and suggests this skill, or when the user asks to define, draft, refine, edit, rewrite, or reverse-engineer the project concept, vision, target users, or value proposition.",
  "claude": {
    "allowed-tools": "Read Write Edit Glob Grep Bash AskUserQuestion"
  }
}
---

# ASD Concept

## Purpose

Create, edit or reverse-engineer `docs/product/concept.html`, authored by the main orchestrator itself.

## Preconditions

```json
{"requires": [".asd/project/config.yaml exists", "no active sprint required (concept is project-lifetime artefact)"], "else": "run /asd-init first"}
```

## Modes

```json
{"edit": "Phase 1 step 1", "fresh": "Phase 2 variant A|B|C", "reverse-engineered": "Phase 2 variant D"}
```

## Steps

Build the orchestrator bundle first and read it: `node .asd/runtime.js context --role orchestrator --phase concept --out <scratch-dir>/orchestrator-concept.context.md` (`agent-protocol.md` "Dispatch payload").

### Phase 1 — silent detection (NO asking)

Scan in order:
1. `docs/product/concept.html` exists, non-empty → mode = **edit**, skip to Edit-mode flow
2. Brownfield signals: `README*`, `docs/**`, root `*.md`, any source files, recent git commits → brownfield candidate (default variant D)
3. Greenfield (no code, no docs) → greenfield candidate (no default)
4. Continue to Phase 2

### Phase 2 — variant choice (only if Phase 1 did not route to edit)

Request user decision, four options (a brownfield candidate defaults D; the user may override):
- **A** — No idea yet, want to explore
- **B** — Vague idea, need shaping
- **C** — Clear vision, will describe
- **D** — Work already started (brownfield), extract what exists

### Phase 3 — flow per variant

**A — divergent brainstorm**
- Request user input, multi-field one-shot: Lens (Problem-first / User-first / Market-first / Tech-first / Capability-first), Horizon (weeks / months / 1-2 years / multi-year), Maturity (first product / shipped before / domain expert)
- Generate 3 distinct concept directions anchored on the lens; request user decision: pick 1 of 3 / regenerate

**B — seeded brainstorm**
- Skip the lens question; generate 3 directions from the raw user hint; request user decision: pick 1 of 3 / regenerate

**C — clear vision**
- Request user input: the concept in the user's own words; draft per `t_concept.html` (required sections first; optional offered per section)

**D — brownfield extraction**
- Scan the repo-search results; draft per template, each filled section tagged `source: <path:line>` or `source: inferred`
- Frontmatter: `provenance: reverse-engineered`, `source: <primary origin>`

### Phase 4 — convergence (universal across variants)

- Create mode only (`docs/product/concept.html` absent): write a skeleton per `t_concept.html` with placeholder sections first. Edit mode enters the loop against the existing file.
- Per section, per `core.md` "Incremental writing": draft, translate to `language.docs`, write it to disk, post the path and a short delta summary in `language.chat` (never the body, `language-policy.md`).
  - Apply `checkpoints.md`: lock in automatically only when adaptive authority/evidence covers this routine section; otherwise request **Lock in / Revise this section / Skip**.
  - Revise: collect feedback, rewrite in place, re-post. Skip: remove the section (heading and placeholder) from the file.
  - Next section per `t_concept.html` order (required first, then per-optional inclusion choice).

### Phase 5 — final artifact-level gate

- The file already holds every locked-in section. Write-then-review-accept per `checkpoints.md`: post the path and a combined delta summary (never the body); `accept` advances, feedback revises in place (a feedback naming a section may re-enter Phase 4 for it)
- emit COMPLETED

### Phase 6 — handoff

- Print in `language.chat`: `docs/architecture/stack.html` absent → "Next: run `/asd-stack` to define the tech stack"; else "Next: run `/asd-sprint` to start a sprint". NO auto-dispatch

### Edit mode (Phase 1 routed here)

- Show the existing concept summary; request user decision: multi-select sections to edit (per template) or add optional ones
- Per chosen section enter Phase 4; Phases 5-6 as usual

## Rules

- never: mix the multi-field and options request shapes in one request.

## Delegates

```json
{"agents": [], "skills": []}
```

## Outputs

```json
["docs/product/concept.html (created, edited, or reverse-engineered)"]
```

## Return

```
SKILL: asd-concept | MODE: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|D|edit> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References

- `.asd/templates/t_concept.html` (required vs optional sections — SSoT)
- `.asd/rules/core.md` (QODDA, Simplicity Default, Incremental writing)
- `.asd/rules/language-policy.md` (chat-vs-docs)
- `.asd/rules/artifact-layout.md` (concept.html path, provenance)

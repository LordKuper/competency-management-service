---
{
  "name": "asd-design-system",
  "description": "Forms or edits the project design system (docs/ux/DESIGN.md, design-system.html, accessibility.html) inline, branching by silent detection into one of three flows (greenfield / constraints / brownfield extraction). Fetches the Google Labs DESIGN.md spec, lints tokens, regenerates design-system.html previews, and authors the accessibility baseline. Use when the user runs /asd-design-system, when asd-init or asd-phase-design detects missing DESIGN.md/design-system.html/accessibility.html and suggests this skill, or when the user asks to define, draft, refine, edit, augment, or reverse-engineer the project design system, design tokens, or accessibility baseline.",
  "claude": {
    "allowed-tools": "Read Write Edit Glob Grep Bash WebFetch AskUserQuestion"
  }
}
---

# ASD Design System

## Purpose

Create or edit the project design system (`docs/ux/DESIGN.md`, `design-system.html`, `accessibility.html`), authored by the main orchestrator itself.

## Preconditions

```json
{"requires": [".asd/project/config.yaml exists", "docs/product/concept.html exists", "docs/architecture/stack.html exists", "no active sprint required"], "else": "no config: run /asd-init first; no concept: run /asd-concept first; no stack: run /asd-stack first"}
```

## Modes

```json
{"edit": "Phase 1 step 1", "fresh": "Phase 2 variant A|B", "reverse-engineered": "Phase 2 variant C"}
```

## Steps

Build the orchestrator bundle first and read it: `node .asd/runtime.js context --role orchestrator --phase design-system --out <scratch-dir>/orchestrator-design-system.context.md` (`agent-protocol.md` "Dispatch payload"). Authoring, token and tool rules: `design-system.md` §12.

### Phase 1 — silent detection (NO asking)

Scan in order:
1. `docs/ux/DESIGN.md` exists, non-empty → mode = **edit**, skip to Edit-mode flow
2. CSS / SCSS / theme files / Tailwind config / styled-components / design-system package / design exports → brownfield candidate (default variant C)
3. No code, no styles → greenfield candidate (no default)
4. Continue to Phase 2

### Phase 2 — variant choice (only if Phase 1 did not route to edit)

Request user decision (a brownfield candidate defaults C):
- **A** — Greenfield, propose from concept + stack
- **B** — I have constraints (brand color, typography, density, platform)
- **C** — Brownfield, extract from existing code/styles

### Phase 3 — flow per variant

Read `concept.html` (vision, target users, value prop, tone) and `stack.html` (UI platform, framework constraints).

**A — greenfield**
- Propose 2-3 candidate token sets per section (color palette, typography scale, spacing scale, radii, motion); request user decision: pick or request alternatives

**B — constrained**
- Request user input, multi-field one-shot: brand color (hex), typography (system / serif / sans-serif / monospace / family), density (compact / comfortable / spacious), target platform (web / native-mobile / native-desktop / cli / mixed), each with "no preference"
- Propose within the constraints

**C — brownfield extraction**
- Extract tokens from CSS variables, Tailwind config, theme objects, styled-components themes, design exports; frontmatter `provenance: reverse-engineered`, `source: <primary file>`

### Phase 4 — convergence (DESIGN.md, per Google Labs spec order)

Sections: colors, typography, spacing, radii/borders, shadows/elevation, motion/timing, components (only those the concept needs).

- Create mode only (`docs/ux/DESIGN.md` absent): write a skeleton (Google Labs format) with placeholder sections first. Edit mode enters the loop against the existing file.
- Fetch the latest Google Labs DESIGN.md spec before the first section, cached for the session.
- Per section, per `core.md` "Incremental writing": draft, write to `DESIGN.md` on disk, post the path and a short delta summary in `language.chat` (never the body, `language-policy.md`).
  - Apply `checkpoints.md`: an adaptive pass only for mechanical or already-authorized content; initial or material UX, brand or accessibility direction stays hard. Otherwise request **Lock in / Revise this section / Skip**.
  - Revise: collect feedback, rewrite in place, re-post. Skip: remove the section (heading and placeholder) from the file.
- After all sections: run `designmd-install` once per session on Windows, then `designmd-lint` (`commands.yaml`); pass criteria `design-system.md` §11. Fail → fix on disk, re-lint; per persistent warning request user decision: exclude | fix, an approved exclusion recorded with rationale in the DESIGN.md lint-exclusions block.

### Phase 5 — design-system.html regeneration

- Render and write `docs/ux/design-system.html` per `t_design-system.html` from the approved DESIGN.md now (only the artifact-level `accept` waits for Phase 7), wrapped in `t_html-shell.html` (DOC_TYPE=Design-system, SUBSYSTEM=project), as `design-system.md` §12 "Regeneration" states.

### Phase 6 — accessibility baseline

- Create mode only (`docs/ux/accessibility.html` absent): write a skeleton wrapped in `t_html-shell.html` (DOC_TYPE=Accessibility, SUBSYSTEM=project) with placeholder sections first.
- Sections: visual (contrast, color-blind, motion), motor (target size, keyboard), cognitive (language, predictability), auditory (captions, transcripts), platform (focus order, ARIA, screen reader).
- Per section, per `core.md` "Incremental writing": draft, translate to `language.docs`, write to disk, post the path and delta summary as in Phase 4; material accessibility direction stays hard, otherwise request **Lock in / Revise this section**.

### Phase 7 — final artifact-level gate: all three files

- The three files already hold every locked-in section. Write-then-review-accept (`checkpoints.md`): post the three absolute paths and one combined delta summary (never the bodies); `accept` advances, feedback revises in place and may re-enter Phase 4 or 6 for a section. Any DESIGN.md revision re-runs `designmd-lint` to a clean pass and Phase 5 before the summary is re-posted; `accept` may not be requested again until both are done. One loop covers all three files until explicit `accept`
- emit COMPLETED

### Phase 8 — handoff

- Print: "Next: run `/asd-sprint` to start the first sprint" (or continue the current sprint when invoked from `asd-phase-design`). NO auto-dispatch

### Edit mode (Phase 1 routed here)

- Show the DESIGN.md token summary, design-system.html freshness, accessibility.html sections
- Force-include any of the three files missing on disk, whatever the user selects — a gate-triggered invocation must not reach COMPLETED without producing it
- Request user decision: multi-select files / sections to edit (DESIGN.md sections, regenerate design-system.html only, accessibility.html sections); per chosen section enter Phase 4 or 6; Phases 5, 7, 8 as usual

## Rules

- must: render EVERY user-decision/input request (question text, header, option labels and descriptions, multi-field labels and hints), control options included, in `language.chat` (`language-policy.md` "User-decision options").
- never: author accessibility rules without checking the concept's target users
- must: bind token authoring and review by `design-system.md`; UX shaping by `ux-principles.md`
- must: regenerate design-system.html from the approved DESIGN.md within this session before Phase 7's `accept` — never stale; orthogonal to the in-sprint cadence (`design-system.md` §10)
- must: reach a clean `designmd-lint` (`design-system.md` §11) before Phase 5 and before Phase 7's `accept`; section writes are not gated on it; a warning exclusion needs user approval and recorded rationale
- must: give every component listed in DESIGN.md a live preview in design-system.html
- never: mix the multi-field and options request shapes in one request.

## Delegates

```json
{"agents": [], "skills": []}
```

## Outputs

```json
["docs/ux/DESIGN.md (created, edited, or reverse-engineered)", "docs/ux/design-system.html (regenerated from DESIGN.md)", "docs/ux/accessibility.html (created or edited)"]
```

## Return

```
SKILL: asd-design-system | MODE: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|edit> | TOKENS: <count> | COMPONENTS: <count> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References

- `.asd/templates/t_design-system.html`, `t_accessibility.html`, `t_html-shell.html`
- `.asd/rules/core.md`
- `.asd/rules/language-policy.md` (binding for every user-decision/input request in this skill)
- `.asd/rules/artifact-layout.md` (DESIGN.md path, design-system.html path, accessibility.html path)
- Google Labs DESIGN.md spec: https://github.com/google-labs-code/design.md

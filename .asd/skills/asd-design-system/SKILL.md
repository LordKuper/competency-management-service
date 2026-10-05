---
{
  "name": "asd-design-system",
  "description": "Forms or edits the project design system (docs/ux/DESIGN.md, design-system.html, accessibility.html) via asd-ux, branching by silent detection into one of three flows (greenfield / constraints / brownfield extraction). Fetches the Google Labs DESIGN.md spec, lints tokens, regenerates design-system.html previews, and authors the accessibility baseline. Use when the user runs /asd-design-system, when asd-init or asd-phase-design detects missing DESIGN.md/design-system.html/accessibility.html and suggests this skill, or when the user asks to define, draft, refine, edit, augment, or reverse-engineer the project design system, design tokens, or accessibility baseline.",
  "claude": {
    "allowed-tools": "Read Glob Grep AskUserQuestion Task"
  }
}
---

# ASD Design System

## Preconditions
- `.asd/project/config.yaml` exists (run `/asd-init` first)
- `docs/product/concept.html` exists (run `/asd-concept` first; concept seeds visual direction)
- `docs/architecture/stack.html` exists (run `/asd-stack` first; stack constrains UI platform — web/native/cli)
- No active sprint required

## Operations used
- Read files — `.asd/project/config.yaml`, concept.html, stack.html, existing DESIGN.md/design-system.html/accessibility.html, source CSS/components, theme files
- Search repo — silent scan for brownfield signals (CSS, SCSS, Tailwind config, theme.ts, styled-components, design exports)
- Request user decision/input — variant choice, constraints, section approvals, lock-in/revise loop
- Run command — `designmd-install` (Windows, once per session)
- Delegate to agents — `asd-ux` (author, fetch external spec, lint, render previews, accessibility baseline)

## Phase 1 — silent detection (NO asking)

Scan in order:
1. `docs/ux/DESIGN.md` exists, non-empty → mode = **edit**, skip to Edit-mode flow
2. CSS / SCSS / theme files / Tailwind config / styled-components / design-system package detected → brownfield candidate (default variant C)
3. No code, no styles → greenfield candidate (no default)
4. Continue to Phase 2

## Phase 2 — variant choice (only if Phase 1 did not route to edit)

Request user decision (3 options) — question text, header, all option labels/descriptions in `language.chat` per `language-policy.md` §User-decision options:
- **A** — Greenfield, designer proposes from concept + stack
- **B** — I have constraints (brand color, typography, density, platform)
- **C** — Brownfield, extract from existing code/styles

Phase 1 brownfield candidates auto-suggest C as default.

## Phase 3 — flow per variant (each delegates to `asd-ux`)

**Variant A — greenfield**
- Delegate to agent `asd-ux` with payload: concept.html, stack.html, language settings, targets = `t_design-system.html`, `t_accessibility.html`
- Designer reads concept (vision, target users, value prop, tone) and stack (UI platform, framework constraints)
- Proposes 2-3 candidate token sets per section (color palette, typography scale, spacing scale, radii, motion)
- Request user decision: pick set or request alternatives — labels/descriptions in `language.chat`
- Proceed to Phase 4

**Variant B — constrained**
- Request user input, multi-field one-shot — every field label, hint, option in `language.chat`:
  - Brand color (hex / "no preference")
  - Typography preference (system / serif / sans-serif / monospace / specific family / "no preference")
  - Density (compact / comfortable / spacious / "no preference")
  - Target platform (web / native-mobile / native-desktop / cli / mixed)
- Delegate to agent `asd-ux` with concept + stack + constraints
- Designer proposes within constraints
- Proceed to Phase 4

**Variant C — brownfield extraction**
- Delegate to agent `asd-ux` with style/component paths (repo-search results)
- Designer extracts tokens from CSS variables, Tailwind config, theme objects, styled-components themes, design exports
- Draft sets `provenance: reverse-engineered` + `source: <primary file>` in frontmatter
- Proceed to Phase 4

## Phase 4 — convergence (universal across variants)

Order per Google Labs DESIGN.md spec:
1. Colors (palette + semantic tokens)
2. Typography (scale + families)
3. Spacing scale
4. Radii / borders
5. Shadows / elevation
6. Motion / timing
7. Components (button, input, card, etc.) — only those needed per concept

- Before the first section, ONLY when `docs/ux/DESIGN.md` does not yet exist — write a skeleton (Google Labs format) with placeholder sections. When the file already exists, skip this step and enter the per-section loop directly against the existing file.
- Fetch latest Google Labs DESIGN.md spec (external doc) on first section; cache for session
- For each section:
  - Designer drafts the section, writes it into `docs/ux/DESIGN.md` on disk
  - Post the file path + a short delta summary of what the section now says in `language.chat` (never the full section body) per `language-policy.md`
  - Apply `checkpoints.md`: adaptive pass only for mechanical/already-authorized content; initial or material UX, brand or accessibility direction remains hard.
  - on B: collect feedback, designer revises, rewrites the section in place, re-posts delta summary, re-ask
  - on C: remove that section (heading + placeholder content) from the on-disk file entirely, then continue to the next section
  - repeat until A
  - next section

After all DESIGN.md sections approved:
- Orchestrator: on Windows, run command `designmd-install` once per session before dispatching `asd-ux` for the lint. Designer runs `designmd-lint` via command execution (`commands.yaml` alias).
- Pass criteria per `.asd/rules/design-system.md` §11: ≥1 error OR ≥1 un-excluded warning = fail.
- Fail → designer fixes on disk, re-lint. Per persistent warning, request user decision to exclude; on approval record decision + rationale in DESIGN.md lint-exclusions block, written on disk.
- Clean pass → continue

## Phase 5 — design-system.html regeneration

- Designer renders and writes `docs/ux/design-system.html` per `t_design-system.html` from approved DESIGN.md now (write is not deferred — only the artifact-level `accept` gate is deferred to Phase 7's combined gate)
- Live previews: color swatches with hex, typography samples, spacing scale, component previews using applied tokens
- Wrap in `t_html-shell.html` (DOC_TYPE=Design-system, SUBSYSTEM=project)

## Phase 6 — accessibility baseline

- Before the first section, ONLY when `docs/ux/accessibility.html` does not yet exist — write a skeleton (wrapped in `t_html-shell.html`, DOC_TYPE=Accessibility, SUBSYSTEM=project) with placeholder sections. When the file already exists, skip this step and enter the per-section loop directly against the existing file.
- Sections: visual (contrast, color-blind, motion), motor (target size, keyboard), cognitive (language, predictability), auditory (captions, transcripts), platform (focus order, ARIA, screen reader)
- For each section:
  - Designer drafts the section, translates to `language.docs`, writes it into `docs/ux/accessibility.html` on disk
  - Post the file path + a short delta summary of what the section now says in `language.chat` (never the full section body) per `language-policy.md`
  - Apply `checkpoints.md`; material accessibility direction remains hard.
  - on B: collect feedback, designer revises, rewrites the section in place, re-posts delta summary, re-ask
  - repeat until A
  - next section

## Phase 7 — final artifact-level gate: all three files

- `docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html` already reflect every locked-in section from Phases 4-6
- write-then-review-accept (`checkpoints.md` mechanic): post the three absolute paths + one combined delta summary in chat (never the body); user reviews the files and replies `accept` (advance) or feedback (revise in place, re-post) — feedback naming a specific section may re-enter Phase 4 or Phase 6 for that section before rewriting; if the re-entered phase is Phase 4 (any DESIGN.md section revised), re-run `designmd-lint` to a clean pass AND Phase 5's design-system.html regeneration before re-posting the combined summary — `accept` may not be requested again until both are done; single loop covers all three files, loop until explicit `accept`
- emit COMPLETED

## Phase 8 — handoff

- Print handoff suggestion: "Next: run `/asd-sprint` to start the first sprint" (or continue current sprint if dispatched from `asd-phase-design`)
- NO auto-dispatch

## Edit mode (Phase 1 routed here)

- Show existing DESIGN.md token summary, design-system.html freshness, accessibility.html sections
- Force-include in the edit/section-loop set any of the three artifacts (`docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html`) that is missing on disk, regardless of user selection — a gate-triggered dispatch (this skill was invoked because one file was missing) must not reach COMPLETED without producing it
- Request user decision: multi-select which files / sections to edit (DESIGN.md sections, regenerate design-system.html only, accessibility.html sections) — labels/descriptions in `language.chat`
- per chosen section: enter Phase 4 or Phase 6 loop
- Phase 5, 7, 8 as usual

## User-input request shapes

- Multi-field one-shot (constraints) → request as a single multi-field input
- Single-choice branching (A/B/C, lock-in/revise) → request as an options choice
- Never mix the two shapes in one request

## Hard rules

- EVERY user-decision/input request (question text, header, all option labels, all option descriptions, multi-field labels and hints) MUST be rendered in `language.chat` from `.asd/project/config.yaml`. Applies to control options too (Lock in / Revise / Skip / Approve / etc.). Per `.asd/rules/language-policy.md` §User-decision options. Internal signal tokens (`COMPLETED`, `FAILED`, `QUESTION`, `ABORT`) stay English — machine signals.
- NEVER author accessibility rules without checking concept's target users
- Token authoring + review bound by `.asd/rules/design-system.md`; UX shaping bound by `.asd/rules/ux-principles.md`
- Within this skill's own session, design-system.html MUST be regenerated from the currently-approved DESIGN.md before Phase 7's `accept` gate — never left stale (see Phase 7's re-run requirement on any Phase-4 re-entry); this is orthogonal to the in-sprint cadence (`.asd/rules/design-system.md` §10: once per sprint, at design-promote, only if DESIGN.md was actually touched that sprint) — this skill runs standalone or via the design-system gate, not per token edit
- `designmd-lint` MUST reach a clean pass (per `.asd/rules/design-system.md` §11) before Phase 5 regeneration and before Phase 7's `accept` gate — section writes to DESIGN.md are not gated on it (write-first per `.asd/rules/checkpoints.md`); warning exclusions need user approval + recorded rationale
- Every component listed in DESIGN.md MUST have a live preview in design-system.html

## Artefacts produced
- `docs/ux/DESIGN.md` (created, edited, or reverse-engineered)
- `docs/ux/design-system.html` (regenerated from DESIGN.md)
- `docs/ux/accessibility.html` (created or edited)

## Agents dispatched
- `asd-ux` (author / scanner / lint / preview render / accessibility baseline)

## Skills dispatched
None.

## Return contract (single line)
```
DESIGN-SYSTEM: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|edit> | TOKENS: <count> | COMPONENTS: <count> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References
- `.asd/templates/t_design-system.html`, `t_accessibility.html`, `t_html-shell.html`
- `.asd/rules/core.md`
- `.asd/rules/language-policy.md` (binding for every user-decision/input request in this skill)
- `.asd/rules/artifact-layout.md` (DESIGN.md path, design-system.html path, accessibility.html path)
- Google Labs DESIGN.md spec: https://github.com/google-labs-code/design.md

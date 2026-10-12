---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-08 — design-promote (lite): scope = prd, ux_spec, adr (fold), c4 (mermaid ## Diagram in subsystems.md); decomposition: no new subsystem, user-management changed (adaptive, audit.md Subsystems map); design-system gate passes; DESIGN.md token changes, if any, wait for the user
- 2026-10-08 — design-promote: BA (requirements user-management.html AC-34…43, audit.html AC-15), architect (subsystems.md + first mermaid ## Diagram, user-management.md, audit.md, stack.html rev 9, tech-reference mailkit/identity/vs-launch/mailpit) and UX (user-management.html, app-shell.html) written; user decisions: DESIGN.md gets typography token brand-wordmark (45 px, 700) and size token brand-logo-size-auth (80 px) with a sign-in card note (recommended options); code binding to tokens is a later change
- 2026-10-08 — design-promote done: requirements user-management.html (AC-34…43, revised AC-2/8/9/11/15/16/20/22/27/32, «Вне рамок»), audit.html (AC-6, AC-9, AC-15); architecture subsystems.md (+ first mermaid ## Diagram), user-management.md, audit.md, stack.html rev 9, tech-reference mailkit/mailpit/identity/vs-launch; UX user-management.html, app-shell.html; DESIGN.md + brand-wordmark, brand-logo-size-auth, sign-in card note, design-system.html regenerated; designmd-lint 0 errors, 4 warnings (all excluded). Noted for later: nsubstitute tech-reference/stack row still say «sprint 001» state; code binding of AuthCard to the new tokens. NEXT: retro

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
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

## 2026-10-05 — Workflow выбран: lite

- **Decision**: Спринт 001 идёт по workflow `lite` (scope → audit → plan → impl ⇄ impl-test → impl-review → design-promote → retro → pr), зафиксирован в `state.json.workflow`.
- **Rationale**: Явный выбор пользователя на жёстком gate `workflow choice`; рекомендация была `standard`.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/state.json`

- 2026-10-05 — retro intake skipped: no archived sprint with retrospective, candidates `[]`
- 2026-10-05 — audit: frozen `true` (greenfield scope, behaviour and contract impact)

## 2026-10-05 — Scope принят: каркас + user-management + org-structure

- **Decision**: Спринт включает каркас проекта, подсистему `user-management` и подсистему `org-structure` (AC-1..14). User→Employee 1:1 необязательно; две роли — глобальный админ (без привязки) и пользователь; штатная единица хранит должность текстом (Career Framework вне спринта).
- **Rationale**: Пользователи привязываются к оргштатке, поэтому подсистемы поставляются вместе (решение пользователя на scope gate); предложение выделить каркас в отдельный спринт отклонено.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`

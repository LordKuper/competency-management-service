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
- 2026-10-13 — design-promote decomposition (оркестратор, адаптивно): подсистема desk одобрена пользователем 2026-10-13 (hard gate) — регистрируется; изменены career-framework, org-structure, user-management, audit; новых подсистем и границ нет
- 2026-10-13 — design-promote: asd-ba и asd-architect COMPLETED; asd-ux остановлен по лимиту 100 ходов (interrupted) с частичным результатом на диске — закоммичено как промежуточное, продолжение тем же агентом
- 2026-10-13 — design-promote: asd-ux COMPLETED после продолжения (desk.html; app-shell, career-framework, user-management; дефект .ds исправлен в career-framework.html и правило светлых превью в тёмной схеме во всех docs/ux с превью; DESIGN.md по дельте, designmd-lint 0 ошибок / 4 известных предупреждения; design-system.html перегенерирован); оркестратор добавил ссылку на requirements/desk.html в desk.md
- 2026-10-13 — design-promote: решение UX вне ux-spec (по замечанию correctness iter-05): версия без запомненного шага (builderStep = null) открывает мастер на шаге 2 без фразы «где вы остановились» — принято адаптивно (оркестратор) как исправление ниже порога в границах принятого UX
- 2026-10-13 — design-promote COMPLETED: требования (desk.html новый; career-framework, user-management, org-structure, audit), архитектура (реестр + desk.md, свёртка ADR-1…6, stack.html, диаграмма, tech-reference), UX (desk.html новый, app-shell, career-framework, user-management, DESIGN.md, design-system.html) → NEXT: plan. Для plan: AC-18 career-framework («черновая запись — только в матрицах её автора») против матрицы по черновому треку, созданному HR; автор, переставший быть руководителем, теряет путь к отправке

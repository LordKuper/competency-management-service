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
- YYYY-MM-DD — cleanup checkpoint <sha>
- YYYY-MM-DD — cleanup: removed <paths/symbols>; reverted <ids or none>; verified <build, lint, tests @sha>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

## 2026-10-26 — Граница прав HR (AC-8)

- **Decision**: HR меняет только оргструктуру: подразделения, сотрудников, совмещения, перечни должностей; карточки стола «Добавить первое подразделение», «Принять сотрудника», «Навести порядок в оргструктуре» и источник стола оргструктуры — HR как администратору. Закрепление ответственного HR за подразделением (пункт «HR» карточки) и «Подключить подразделение» остаются за глобальным администратором.
- **Rationale**: Выбор пользователя на вопросе аудита (неоднозначность, решаемая только предпочтением): права карьерной модели не расширяются, HR не назначает себя согласующим матриц.
- **Affected docs**: `audit.md` (Subsystems map), `sprint.md` AC-8

## 2026-10-26 — Аудит принят

- **Decision**: Аудит записан в `audit.md` и принят адаптивно (user_gates=adaptive). Единственное противоречие (требования оргструктуры vs принятый скоуп) решено принятием скоупа пользователем; неоднозначность AC-8 решена пользователем. Неоднозначность «кто считается сотрудником подразделения» решена по источникам: счётчики, деактивация подразделения, impact и находки стола считают все назначения (AC-4, AC-5), руководитель и потребители career-framework — основное (AC-1).
- **Rationale**: Все проверки аудита выполнены на HEAD 2185295, незакрытых противоречий и вопросов нет; спринт lite — далее plan.
- **Affected docs**: `audit.md`

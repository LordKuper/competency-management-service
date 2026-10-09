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

## 2026-10-09 — plan decisions: subsystem, deletion principle, UI

- **Decision**: Пользователь утвердил подсистему `career-framework` («Карьерная модель», модуль `Competency.CareerFramework`; закрепление HR и вычисление ролей процесса — в ней; порты `IEmployeeDirectory` (расширение) и новый `IUserDirectory`); строки-связи (должность в перечне подразделения, закрепление HR) удаляются физически с событием журнала — принцип `stack.html` уточняется на design-promote; перечень должностей и HR подразделения — модальные окна из меню карточки подразделения; тексты интерфейса утверждаются в составе plan (правило AC-20 применяется к этому спринту).
- **Rationale**: Ответы пользователя на hard gates «новая подсистема» и «material architecture» (audit R-10, R-14) и на выбор UX (audit Gaps, R-13); рекомендованные варианты.
- **Affected docs**: plan.md; на design-promote — docs/architecture/subsystems.md, career-framework.md, stack.html

## 2026-10-09 — plan accepted

- **Decision**: `plan.md` принят пользователем (accept): Task 1–9 в шести волнах, порядок блокировок, раздел «Тексты интерфейса» утверждён (правило AC-20).
- **Rationale**: Явное решение пользователя на гейте plan; дополнение `sql-postgresql-18.6.md` (advisory-блокировки, частичные индексы) выполняется до перехода в impl.
- **Affected docs**: plan.md

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

## 2026-10-30 — Workflow спринта: lite

- **Decision**: Спринт 007 идёт по workflow `lite` (выбор пользователя, рекомендованный вариант).
- **Rationale**: Переименование механическое, без нового поведения; полный дизайн-блок standard избыточен.
- **Affected docs**: [state.json](./state.json)

## 2026-10-30 — Граница и подход переименования

- **Decision**: Меняется только имя продукта (префикс `Competency.*`, решение, инфраструктурные и пакетные имена); доменный термин «компетенция» остаётся. Имена с состоянием (БД/пользователь, том dev-БД, namespace, Secret, путь ключей и ApplicationName DataProtection, cookie) переименовываются в kalibr, dev-БД начинается с нуля без инструкции переноса. Подход A — одно сквозное переименование; корневой `package.json` → `kalibr`, репозиторий GitHub и папку пользователь переименует сам; память агентов обновляется точечно.
- **Rationale**: Ответы пользователя на стадиях (a) и (c), все варианты — рекомендованные, кроме dev-БД (выбран вариант «с нуля» вместо инструкции переноса). До 1.0 обратная совместимость не поддерживается.
- **Affected docs**: [research.md](./research.md)

## 2026-10-30 — Ретро-intake: перепроверка строк 006 на HEAD

- **Decision**: На HEAD 3e5594d2 строки `006-promotion-waves#A-1` и `#P-1` не закрыты (в памяти `asd-tester*` нет правила о пороге как контракте, в `custom-*-rules.md` нет правила о макетах); `#A-5` закрыта наполовину — закрытие операции `suite` сразу после прогона уже записано (`asd-tester-critical/project_competency-test-harness.md`), сужена до открытия перед каждым прогоном. Upstream-строки A-3, A-4, A-6, A-7, P-2, P-3 показаны пользователю как предложения для фреймворка, без решения.
- **Rationale**: Ретро-строки отражают HEAD своего спринта; переносится только нерешённое (`sprint-lifecycle.md` «Orchestration and adaptive gates»).
- **Affected docs**: [retro-backlog.md](../../project/retro-backlog.md)

## 2026-10-30 — Scope принят

- **Decision**: Пользователь принял `sprint.md` (AC-1..AC-13) без правок; ретро-строки 006 A-1, A-5 (суженная), P-1 включены как AC-14, AC-15, AC-16. Стоимость каждого включённого критерия: 0 итераций, 0 раундов исправлений (новые).
- **Rationale**: Явный ответ пользователя на шлюзе scope и retro intake dispositions; все выбраны рекомендованные варианты.
- **Affected docs**: [sprint.md](./sprint.md), [retro-backlog.md](../../project/retro-backlog.md)

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

## 2026-10-26 — Выбран workflow lite

- **Decision**: Спринт 005 идёт по workflow `lite` (без фаз design и design-review; документы пишутся в design-promote по готовой реализации).
- **Rationale**: Выбор пользователя на жёстком гейте workflow choice; оркестратор рекомендовал `standard` из-за нового формата файла и правил слияния, пользователь выбрал `lite`.
- **Affected docs**: `state.json` (`workflow`)

## 2026-10-26 — Подход к импорту: без состояния

- **Decision**: Импорт без серверного состояния: сервер разбирает файл и строит план для предпросмотра; решения по отсутствующим живут в мастере; применение отправляет файл, решения и отпечаток плана, сервер пересчитывает план и отклоняет применение, если он разошёлся с показанным.
- **Rationale**: Пользователь отказался хранить незавершённый импорт (выход из мастера — отмена), поэтому серверный пакет потерял главный плюс; выбор пользователя на гейте approach choice по рекомендации оркестратора.
- **Affected docs**: `research.md` (Requirements Refined, Approaches Chosen)

## 2026-10-26 — Приём ретро-строк спринта 004

- **Decision**: Проверены на HEAD 8c4c1a6: строки 004#A-4, A-7, P-1, P-2 не записаны ни в `custom-common-rules.md`, ни в `custom-coding-rules.md` — не решены. Пользователь: A-4 и A-7 отклонить и вместо них записать правило о закрытии блокирующих процессов проекта (AC-25); P-1 и P-2 включить (AC-26, AC-27). Строки A-1, A-3, A-5, A-6, A-8, A-9, P-3 — предложения для фреймворка ASD (upstream), без решения.
- **Rationale**: Явный выбор пользователя по каждой строке на гейте scope; стоимость каждого критерия — 0 итераций ревью и 0 раундов исправлений (новый спринт).
- **Affected docs**: `.asd/project/retro-backlog.md`, `sprint.md` (AC-25…AC-27)

## 2026-10-26 — Скоуп принят

- **Decision**: Пользователь принял скоуп AC-1…AC-27 одним спринтом (разбиение на 005/006 отклонено); документы этого спринта не пропускаются (prd, design_system, adr, c4 остаются включены; ux_spec выключен конфигом). Audit: `documents.audit: auto`, скоуп не механический (меняются модель, контракт, права, миграция) — audit выполняется.
- **Rationale**: Импорт без совмещений и юрлиц нельзя принять; меняются требования оргструктуры и рабочего стола.
- **Affected docs**: `sprint.md`, `research.md`, `state.json`

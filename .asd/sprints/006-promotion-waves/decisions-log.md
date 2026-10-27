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

## 2026-10-27 — Workflow спринта: lite

- **Decision**: Спринт 006 идёт по workflow `lite` (выбор пользователя; оркестратор рекомендовал `standard`).
- **Rationale**: Пользователь предпочёл более быстрый цикл без черновиков дизайна; persistent-доки пишутся в design-promote после impl-review.
- **Affected docs**: [state.json](./state.json)

## 2026-10-27 — Подход: итоговые оценки ставит председатель (B)

- **Decision**: Выбран подход B: самооценка, круговая оценка и артефакты — входные данные комиссии; итоговые оценки по компетенциям вносит председатель по итогам обсуждения, без независимых предварительных оценок членов; рейтинг и ранжирование — по итоговым оценкам и формуле матрицы; решение фиксирует председатель. Артефакт — текст и необязательные ссылки, без файлов.
- **Rationale**: Выбор пользователя (рекомендован A — независимые оценки членов до заседания); B даёт меньше состояний и экранов.
- **Affected docs**: [research.md](./research.md)

## 2026-10-27 — Ответы на шлюзе scope (до принятия)

- **Decision**: Требуемый уровень образования хранится в версии матрицы (AC-15). Retro intake: `005-org-structure-excel-import#P-1` — reject, `#P-3` — include (AC-45), `#P-4` — include (AC-46); `#P-2` — upstream, без решения. Скоуп не делится; документы этого спринта не пропускаются. Scope отправлен на доработку — правки ожидаются от пользователя.
- **Rationale**: Ответы пользователя на шлюзе scope; на HEAD 1eedbd5 правил P-1, P-3, P-4 в `custom-coding-rules.md` нет (перепроверено), поэтому все три вынесены на решение.
- **Affected docs**: [sprint.md](./sprint.md)

## 2026-10-27 — Scope принят

- **Decision**: Пользователь принял `sprint.md` (AC-1…AC-46) после правок: цель заявки — всегда уровень трека; отзыв до решения; предупреждение о несоответствии образования до отправки; дочерние подразделения отмечаются по умолчанию; предстоящие и открытые волны видны всем, подать заявку может сотрудник любого подразделения. Retro intake записан в `retro-backlog.md`: P-1 rejected, P-3 included (AC-45), P-4 included (AC-46); стоимость включённых критериев — 0 итераций, 0 раундов. Audit: `documents.audit: auto` → true (новое поведение, контракты, миграции).
- **Rationale**: Явный `accept` пользователя на шлюзе scope; аудит обязателен для нерутинного скоупа.
- **Affected docs**: [sprint.md](./sprint.md), [research.md](./research.md), [retro-backlog.md](../../project/retro-backlog.md)
- 2026-10-27 — retro intake: verified HEAD 1eedbd5; 005#P-1, #P-3, #P-4 absent from custom-coding-rules.md (unresolved); 005#P-2 upstream, shown without disposition

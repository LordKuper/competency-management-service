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

## 2026-10-12 — audit contradictions C-1…C-3

- **Decision**: C-1 — версию матрицы можно строить и отправлять по черновому треку своего подразделения, публикация (согласование HR) запрещена, пока трек не стал действующим (ответ пользователя своими словами, не из предложенных вариантов); AC-8 уточнён. C-2 — давность только у событийных видов; AC-2 уточнён. C-3 — «Отправить приглашение повторно» кнопкой на столе; AC-6 уточнён.
- **Rationale**: противоречия внутри принятого скоупа без канонической стороны — hard-решение пользователя; стоимость AC-2, AC-6, AC-8: 0 итераций / 0 раундов исправлений. C-1 меняет канон career-framework AC-36 (трек при создании матрицы) — правка канона на design-promote.
- **Affected docs**: sprint.md, audit.md, docs/product/requirements/career-framework.html (design-promote)

## 2026-10-12 — audit ambiguities и приём audit

- **Decision**: пользователь принял все рекомендации A-4, A-6…A-9, A-11…A-13, A-15; оркестратор решил A-5, A-10, A-14 по источникам; C-4 (AC-20 — без автоматического повтора после 412) и C-5 — по канону. Gate audit пройден адаптивно (оркестратор): противоречия и неоднозначности закрыты, audit.md полон.
- **Rationale**: authority/preference-неоднозначности — у пользователя; источникорешаемые — по коду и канону; адаптивный приём audit по `checkpoints.md`.
- **Affected docs**: audit.md, state.json

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

## 2026-10-27 — Audit: цель и комиссия по матрице подразделения волны

- **Decision**: Противоречие AC-9/AC-27 с правилом матриц (трек может быть вышестоящего подразделения) решено пользователем: цель — уровень трека в опубликованной матрице подразделения волны, рассматривает комиссия этого подразделения по его матрице. AC-9 и AC-27 выведены, добавлены AC-47 и AC-48 (стоимость 0 итераций, 0 раундов).
- **Rationale**: Комиссия и HR остаются в пределах волны; канонической стороны у противоречия не было.
- **Affected docs**: [sprint.md](./sprint.md), [audit.md](./audit.md)

## 2026-10-27 — Audit: компетенция без наблюдений считается непройденной

- **Decision**: Компетенция, по которой все оценивающие отметили «Не наблюдал», в проверке после круговой оценки — доля 0 и обязательные артефакты (AC-49, стоимость 0 итераций, 0 раундов).
- **Rationale**: Решение пользователя по неоднозначности, которую источники не закрывают.
- **Affected docs**: [sprint.md](./sprint.md)

## 2026-10-27 — Audit принят (adaptive)

- **Decision**: `audit.md` принят оркестратором в adaptive-режиме; список уровней образования взят из ч. 4–5 ст. 10 закона № 273-ФЗ; новая подсистема `promotion` предлагается к реестру в design-promote.
- **Rationale**: Противоречие и неоднозначность решены пользователем, шлюзов подсистем и удаления нет, stubs по скоупу нет.
- **Affected docs**: [audit.md](./audit.md)

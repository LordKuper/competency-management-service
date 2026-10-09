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

## 2026-10-09 — scope amendment AC-16

- **Decision**: AC-16 изменён по указанию пользователя: если ответственный HR подразделения матрицы — сам автор, шаг 2 (HR) считается согласованным сразу; прежняя формулировка «он сам автор — отправить нельзя» снята.
- **Rationale**: Прямое указание пользователя во время audit (hard gate «new or changed scope»); plan ещё не создан, Task не затрагивается; стоимость AC-16: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-16, audit.md

## 2026-10-09 — audit contradictions and ambiguities

- **Decision**: Пользователь принял все рекомендации аудита: C-1…C-3 (уточнены AC-3 и AC-9 — неизменённая должность не перепроверяется; черновые записи версии видны всем, кто видит версию; HR видит черновики только своих подразделений) и A-1, A-2, A-4, A-5, A-6b, A-7…A-11, A-13…A-15, A-17 (перечень — `audit.md` «Решения по противоречиям и неоднозначностям»).
- **Rationale**: Противоречия внутри `sprint.md` без канонической стороны и неоднозначности, решаемые только полномочием или предпочтением; стоимость затронутых AC-2, AC-3, AC-9: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-3, AC-9; audit.md

## 2026-10-09 — audit accepted

- **Decision**: `audit.md` принят; подсистема `career-framework` предложена (утверждение — hard gate; решение запросить на plan, риск R-10); документы не пропускаются.
- **Rationale**: Все противоречия и неоднозначности решены пользователем; разделы `t_audit.md` заполнены; source-resolvable неоднозначности (A-3, A-6a, A-12, A-16, A-18, A-19) закрыты по источникам, BA не требуется.
- **Affected docs**: audit.md, state.json

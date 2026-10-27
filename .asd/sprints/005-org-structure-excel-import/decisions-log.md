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

## 2026-10-27 — design-promote (lite): постоянные документы приведены к принятой реализации

- **Decision**: Требования: org-structure (74 AC: удаление пустого подразделения, совмещения и юрлица, равные права HR, импорт xlsx — AC-47…AC-74), desk (AC-220 карточка «Импорт оргструктуры», доступ HR к сценариям оргструктуры), career-framework AC-159, user-management AC-12, audit AC-3. Архитектура: org-structure.md переписан (назначения, юрлица, `IUnitReferences`, `Import/` с пределами и отпечатком, `TextCollation`), desk.md, career-framework.md (`UnitReferences`), audit.md, subsystems.md (строка и mermaid), stack.html (коллация `ru-x-icu`, ClosedXML подключён, revision 19). Новых подсистем, переименований и удалений документов нет; токены ДС не менялись — гейт дизайн-системы без дельты.
- **Rationale**: Lite: продвижение из принятой реализации после impl-review (DoD на 50090b3); решения пользователя спринта (стратегия импорта, предел 30 уровней, права HR) свёрнуты в постоянные документы.
- **Affected docs**: [org-structure.html](../../../docs/product/requirements/org-structure.html), [desk.html](../../../docs/product/requirements/desk.html), [career-framework.html](../../../docs/product/requirements/career-framework.html), [user-management.html](../../../docs/product/requirements/user-management.html), [audit.html](../../../docs/product/requirements/audit.html), [org-structure.md](../../../docs/architecture/org-structure.md), [desk.md](../../../docs/architecture/desk.md), [career-framework.md](../../../docs/architecture/career-framework.md), [audit.md](../../../docs/architecture/audit.md), [subsystems.md](../../../docs/architecture/subsystems.md), [stack.html](../../../docs/architecture/stack.html)
- 2026-10-27 — cleanup checkpoint 56b81d6f8b1673f4a89ada9318ffc05eef2cf05e
- 2026-10-27 — cleanup: removed none; reverted none; verified n/a: no removals

## 2026-10-27 — Ретроспектива спринта записана

- **Decision**: `retrospective.html` записан по ветке пустого журнала трений (0 записей, 0 действий) с 4 системными предложениями: P-1 пределы разбора загружаемых файлов, P-3 надписи только для зрителя, P-4 охрана маршрутов при расширении ролей (все — `custom-coding-rules.md`), P-2 вопросы scope о жизненном цикле массово созданных данных (upstream ASD).
- **Rationale**: Свидетельства — ход спринта: критическая находка внешнего ревью, четыре поправки скоупа и два запроса изменений в приёмке impl, находка о переходе HR на закрытый экран. Ничего не применено; строки попадут в приём ретро следующего scope.
- **Affected docs**: [retrospective.html](./retrospective.html)

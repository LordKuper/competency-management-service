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
- 2026-10-13 — route Task 1: standard, dispatch HEAD d54507c
- 2026-10-13 — route Task 2: critical (owner asd-dev-critical — память агента правит только её владелец, artifact-layout.md «Agent memory»), dispatch HEAD d54507c
- 2026-10-13 — route Task 3: critical, dispatch HEAD d54507c
- 2026-10-13 — Task 2 flagged choices приняты оркестратором (в границах AC-22/AC-23 и правил памяти): удалены ссылки на историю (Task/Sprint/MS/AC) в правленых файлах; project_web-cdp-real-backend-smoke-facts.md и project_scratch-process-control.md оставлены с пометкой (на первый ссылаются 6 файлов, во втором — запрет down -v); висячая ссылка [[scratch-host-without-postgres]] допустима правилами памяти
- 2026-10-13 — Task 3 flagged choices приняты оркестратором (в границах ADR-2/org-structure.md): OrgSnapshot — sealed class; отдельная запись PositionStatus; OrgUnitStatus с названием; сброс запомненного снимка при взятии блокировки дерева через порт; WorkingEmployeeCounts/UnitPositionIds; волна 1 завершена (777d50e, f9d2d7e, 746bbe6)
- 2026-10-13 — route Task 4: critical, dispatch HEAD f8a7f56

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
- 2026-10-13 — Task 4 COMPLETED (c938c16, 2b35ff7, 8d4ac3f); блокировка инвариантов связей — CatalogStore.BeginWriteAsync (дерево 5001001 разделяемо → справочники 5003001 эксклюзивно, шаг 2 порядка stack.html, новых блокировок нет). Flagged choices приняты оркестратором: (1) минимальные перечисления PublicationRefusal — Task 5 расширит; (2) смешанные формы 409 у approve/activate — выровнять в Task 5 (передано в payload); (3) отказ по готовности до IfMatch.ApplyTo — по прецеденту проверки «не черновик», чтобы проигравший в гонке снятий получал 409 (отступление от общего порядка «412 раньше последующих проверок» stack.html ограничено проверками состояния, а не содержимого); (4) идемпотентный повтор POST без If-Match; (5) активация проверяет готовность только при переходе Inactive→Active; (6) hasActivePosition, сортировка OrdinalIgnoreCase; (7) коды полноты и readiness — Task 6. Ожидаемое падение тестов (хранитель создаёт трек действующим, полнота без связей) — для impl-test
- 2026-10-13 — route Task 5: critical, dispatch HEAD fbbd94a
- 2026-10-13 — Task 5 COMPLETED (0453509, 2969c5f, 59c4fca); новых блокировок нет. Flagged choices приняты оркестратором: (1) entries предпросмотра без фильтра видимости — черновики, на которые ссылается версия, читают все, кто видит версию (career-framework AC C-2), фильтруются blockers; (2) права ответственного HR строятся как HR, isAdmin только если HR — сам действующий администратор (крайний случай, без роли в UserStatus); (3) у уровня нет предпросмотра (замыкание пустое); (4) 412 переходов версии — PublicationChanged с необязательным publication, 409 переходов — ProblemDetails; (5) PublicationBlockerKind → PublicationEntryKind (3.1.0 не выпущен); (6) «Принять правки» при отсутствии HR и пустом каскаде публикует, как прежде; (7) порядок проверок записи 404→403→409→409→412(If-Match)→412(тег). Ожидаемое падение тестов (412 без тега, NameTaken, создание черновиком) — для impl-test
- 2026-10-13 — route Task 6: critical, dispatch HEAD de3bc98
- 2026-10-13 — Task 6 COMPLETED (a4ce4dd…0e84dbd). Flagged choices приняты оркестратором: (1) builderStep и readiness необязательные в схеме; (2) /…/{id}/matrices — неархивные читаемые версии матриц, использующих запись; (3) неиспользуемые: компетенция через варианты, категория через snapshot, шкала буквально по AC; (4) подключение: «Заблокирована» важнее «Приглашена», «пустой перечень» — ни у подразделения, ни у вышестоящих нет должностей (источник org-structure Task 8 считает так же — передано в payload), positions {own, inherited}, reason — код; (5) «Моё место»: действующие уровни действующих треков, HR — только действующая учётная запись, requirements = null без опубликованной матрицы; (6) состояние приглашения через IsRegistered, срок ссылки — внутри user-management; (7) ProcessRoles.NearestAssigned/AssigneesAsync для повторного использования; (8)-(9) — для impl-test
- 2026-10-13 — route Task 7: critical, dispatch HEAD 0e84dbd
- 2026-10-13 — route Task 8: critical, dispatch HEAD 0e84dbd

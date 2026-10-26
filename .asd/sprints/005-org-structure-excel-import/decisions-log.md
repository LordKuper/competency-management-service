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
- 2026-10-26 — route Task 1: standard, dispatch HEAD 8c5b1a2

## 2026-10-26 — Task 1: отмеченные выборы dev приняты

- **Decision**: Приняты все `flagged` Task 1: `legalEntityIds` и `assignments` в `EmployeeRequest` обязательны (полная замена в `PUT`); ответ отдаёт `legalEntities: [{id, name}]`, `assignments` (только совмещения) и `unitAssignments` (список назначений в отфильтрованном подразделении); возврат на работу — 409, если совмещение в неактивном подразделении; связи юрлиц — аудируемые сущности; `WorkingAssigneeCounts` в `OrgSnapshot`. Ошибки типов SPA (`ChangePositionForm.tsx`, фикстуры `EmployeeCards.test.tsx`, `orgTree.test.ts`) уходят в Task 3; тестовые вызовы API без новых полей — в impl-test.
- **Rationale**: Обязательные поля исключают случайное стирание совмещений; список назначений в подразделении нужен, когда у сотрудника две должности в одном подразделении; правило возврата на работу держит инвариант деактивации. План эти детали не задавал.
- **Affected docs**: [plan.md](./plan.md)
- 2026-10-26 — route Task 2: standard, dispatch HEAD 762df3e
- 2026-10-26 — route Task 3: standard, dispatch HEAD 762df3e

## 2026-10-26 — Волна 2: отмеченные выборы Task 2 и Task 3 приняты

- **Decision**: Task 2: порт `IEmployeeAccounts.LeavesNoActiveAdministratorAsync` для проверки набора увольнений; контракт `preview` → `{errors, plan}` с 13 группами-списками и `missingUnits`/`missingEmployees`, `apply` — multipart с `decisions` строкой JSON, незаданные решения = «Деактивировать»/«Уволить», 409 `code: planChanged` отличается от 409 по правилу; тёзки в системе проверяются глобально (буквально AC-14); перечень подразделения, где заканчиваются строки файла, заменяется должностями этих строк; пустая ячейка юрлица очищает юрлица назначения; применение наборное, увольнения через `EmployeeImpact` поштучно (потолок около 9 000 увольнений за 25 с); предел тела — `RequestSizeLimit` на группе. Task 3: `canEdit` вместо `isAdmin`, `ChangePositionForm` меняет должность названного назначения, шаг учётной записи для HR — плашка «создаёт администратор», пути ошибок `assignments[i].*`, блокировка `UnitFormModal`, `MoveUnitModal`, `AddPositionForm` на время сохранения.
- **Rationale**: Каждое решение либо следует буквальному AC, либо закрывает инвариант (ноль администраторов при двух увольнениях, совмещение вне перечня), либо нужно для AC-24; замер: preview 1,9–2,9 с, apply 6,0–8,6 с на 10 000 строк.
- **Affected docs**: [plan.md](./plan.md)
- 2026-10-26 — route Task 4: standard, dispatch HEAD b8c9273

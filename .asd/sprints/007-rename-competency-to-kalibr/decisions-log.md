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

## 2026-10-30 — plan amended: test literals to Test-only Task 3

- **Decision**: Строковые литералы тестов с именем продукта (`DeploymentTests.cs`, `ApiContract.cs`, `ApiHost.cs`, `PlatformTests.cs`, `OrgImportParserTests.cs`) вынесены из Task 1 в Test-only Task 3 (`asd-tester`, волна 2); Task 1 меняет в тестах только то, что нужно для компиляции (namespace, `using`, `cref`, csproj). Найден дополнительный литерал `PlatformTests.cs` `FailureLogCategory = "Competency.Platform.ProblemExceptionHandler"`.
- **Rationale**: impl запрещает dev'у править тесты, кроме Test-only Task (`sprint-lifecycle.md` «Plan file format»); AC и решения пользователя не меняются, поэтому принятие плана повторено адаптивно.
- **Affected docs**: [plan.md](plan.md)
- 2026-10-30 — route Task 1: standard, dispatch HEAD 21713c10; risk change: session reset, artifact: literal swap across build graph via build, has-pending-model-changes, check:api, diff pattern check
- 2026-10-30 — route Task 2: standard, dispatch HEAD 21713c10; risk artifact: deploy literal swap via rg product-name patterns in deploy/
- 2026-10-30 — route Task 3: standard, dispatch HEAD b176f509

## 2026-10-30 — impl assessment approved

- **Decision**: Impl принят адаптивно: Task 1 (`198631db` git mv, `07d83cca` содержимое), Task 2 (`f62e2c5f`), Task 3 (`ce7a2ea8`) и правки оркестратора `commands.yaml`, `custom-coding-rules.md`, `custom-common-rules.md` с правилом AC-16 (`b176f509`). Флаг тестера «`dotnet test --solution Kalibr.slnx` находит 0 тестов» проверен: `dotnet test --solution Kalibr.slnx --no-build --list-tests` находит 952 теста, код 0 — дефекта нет, причина в способе запуска самопроверки.
- **Rationale**: Все подзадачи отмечены; `dotnet build Kalibr.slnx` (Debug и Release) — 0 предупреждений и 0 ошибок, web `build`, `lint`, `check:api` чисты, `has-pending-model-changes` чист; диф раунда не выходит за пути задач и строки оркестратора плана. Сброс сессий и смена `info.title` разрешены AC-5 и AC-8; заглушек спринт не добавил.
- **Affected docs**: [plan.md](plan.md)

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

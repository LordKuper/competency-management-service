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
- 2026-10-30 — route impl-test entry 1: standard, dispatch HEAD 5d32467a
- 2026-10-30 — memory-fix: retired agents asd-dev-critical (14), asd-tester-critical (1), asd-reviewer-correctness (1), asd-reviewer-testing (1) — product-name files deleted with their MEMORY.md lines, b2f62ebb
- 2026-10-30 — memory-fix: asd-dev leftover-term hits (perf-measure-temp-db, hosted-service-ctor-options) fixed by owner, 746be450
- 2026-10-30 — memory-fix: asd-tester leftover-term hits (backend-tests-scratch-build) fixed by owner, AC-14 and AC-19 recorded (threshold-is-contract, suite-timing-operation), 1718aa70
- 2026-10-30 — impl-test: impacted set green (full suite by safety valve: .NET 952/952, web 850/850 @1718aa70), 0/0 tests

## 2026-10-30 — impl-review: runtime git calls run in a Linux container

- **Decision**: Выбор пользователя — вариант A (рекомендация advisor). Шаги `review-waves` и `emit-manifest` выполняет тот же неизменённый `.asd/runtime.js` в контейнере `node:24.21.0-trixie`: репозиторий примонтирован, `safe.directory` задан через env. Остальные шаги идут на Windows как обычно.
- **Rationale**: На Windows один вызов git со всеми путями диффа (~60 000 символов) превышает лимит CreateProcess в 32 767 символов (F-1). Править инфраструктуру нельзя. Диффы строятся по диапазону коммитов, поэтому результат от ОС не зависит. Advisor: A не требует hard gate. B стопорит готовый спринт. C — waiver ревью, и до лимита он всё равно не ужимает.
- **Affected docs**: [friction-log.md](friction-log.md)
- 2026-10-30 — combined interrupted attempt 1 in wave-1/iter-01 (stall: host watchdog, no progress 600s)
- 2026-10-30 — external interrupted attempt 1 in wave-1/iter-01 (session ended before return)
- 2026-10-30 — memory-fix: asd-advisor memory-check hits (sprint id in project_windows-arg-limit-review-waves, MEMORY.md line) fixed by owner's MEMORY-FIX block, applied by orchestrator, 0fe76f5b
- 2026-10-30 — external wave-1/iter-01 persisted APPROVE without its `Interrupted attempts: 1 (session ended before return)` line (persist-review refuses a second write); the attempt record stands in this log
- 2026-10-30 — impl-review wave-1/iter-01: combined APPROVE, external APPROVE — reviewer DoD met (single wave), latched combined, external; terminal full suite next
- 2026-10-30 — route impl-review wave-1/iter-01 suite: standard, dispatch HEAD a2f3855b
- 2026-10-30 — impl-review terminal full suite green (ec1fad8f; HEAD a2f3855b: .NET 952/952, web 850/850, lint и build exit 0) → DoD met; impl-review green handoff (gate adaptive, оркестратор): открытых находок и дефектов нет; NEXT design-promote
- 2026-10-30 — design-promote, decomposition (gate adaptive, оркестратор): новых подсистем и изменений границ нет; меняются только имена модулей и путей (Competency.* → Kalibr.*) в подписях диаграммы subsystems.md и ключевых путях <id>.md
- 2026-10-30 — design-promote, design-system gate: docs/ux/DESIGN.md, design-system.html и accessibility.html на месте; токены не менялись — в DESIGN.md только `name` и заголовок (AC-10); design-md-delta.yaml не нужен, design-system.html имени продукта не содержит и не перегенерируется; designmd-lint: 0 ошибок, 4 предупреждения — те же, что на main
- 2026-10-30 — design-promote (lite): постоянные документы приведены к принятой реализации (DoD impl-review на ec1fad8f), AC-10 — 22 файла docs/**, имя продукта в путях, проектах, решении, образе, dev-стеке, cookie, ключах DataProtection и пользователе PostgreSQL: subsystems.md, audit.md, career-framework.md, desk.md, org-structure.md, promotion.md, user-management.md, stack.html (ревизия 21; прежние записи журнала ревизий переписаны на новые имена проектов), 13 tech-reference, DESIGN.md. PRD (requirements/*.html) имени продукта не содержит; ADR нет; c4 (mermaid в subsystems.md) — подписи модулей. Доменный термин оставлен: desk.md `kind` `competency`, сущности в Постановка.md, Competency* типы
- 2026-10-30 — AC-13 check: missed product name `POSTGRES_USER=competency` (form with `=` not in the plan.md patterns) in retired asd-dev-critical/project_typed-problem-extension-facts.md:12 — deleted with its MEMORY.md line per the user's AC-18 decision (retired agents' product-name files are deleted)
- 2026-10-30 — AC-13 check (`rg -i competency`, без node_modules, bin, obj, dist, TestResults, .vs, .git) на 02a7ba91: остались только доменный термин (типы, таблицы, маршруты, тексты UI, сиды `tests/perf/seed-10k.sql`, описания openapi, проза docs и DESIGN.md) и места «Out of scope»: архив `.asd/sprints/archived/**`; папка и id этого спринта (`.asd/sprints/007-rename-competency-to-kalibr/**`, `.asd/project/retro-backlog.md` строки 26–28, `docs/architecture/stack.html` ревизия 21); id прошлого спринта `003-positions-career-competency` в `.asd/project/custom-coding-rules.md:31,35`; `deploy/dev/README.md:32` (старые том `competency-dev-pgdata` и контейнеры `competency-dev`, AC-7); `deploy/README.md:94` (старая установка, AC-6). Инфраструктура ASD вхождений не содержит
- 2026-10-30 — cleanup: removed none; reverted none; verified n/a: no removals
- 2026-10-30 — retro: retrospective.html written; 1 friction entry (F-1), 2 actions (A-1 asd, A-2 consumer covered by asd-advisor memory), 2 systemic proposals (P-1, P-2, asd); analysed branch
- 2026-10-30 — retro upstream: published A-1 https://github.com/LordKuper/agentic-software-development/issues/74, P-1 https://github.com/LordKuper/agentic-software-development/issues/75, P-2 https://github.com/LordKuper/agentic-software-development/issues/76; skipped A-2 (covered by)

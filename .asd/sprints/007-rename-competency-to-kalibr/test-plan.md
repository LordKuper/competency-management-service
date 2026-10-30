---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/quality.md (verdict)
---

# Test plan — sprint 007-rename-competency-to-kalibr

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 |  | full change surface |

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `Kalibr.slnx`, `src/Kalibr.*`, `tests/Kalibr.Tests` (перенос папок и `.csproj`, `ProjectReference`, `InternalsVisibleTo`, lock-файлы) | решение или проект не находится, сборка графа ломается | static (сборка) + существующие `DeploymentTests` (список проектов из дерева, поиск корня по `Kalibr.slnx`) | none | сборка и 952 теста прошли; `DeploymentTests` и `ApiContract` уже ищут `Kalibr.slnx` и ждут `Kalibr.CareerFramework`, новый тест дублировал бы их |
| Запуск `Kalibr.Api.dll` | хост не стартует под новым именем сборки | существующий `ApiHost` (интеграционные тесты на реальной PostgreSQL) | none | каждый API-тест стартует `Kalibr.Api.dll`; 952 из 952 зелёные — отдельный тест не нужен |
| Категория журнала `Kalibr.Platform.*` | тест ожидает старую категорию, пропускает сбой | существующий `PlatformTests` (`FailureLogCategory`) | none | литерал обновлён Task 3, тест зелёный; категория выводится из namespace, второй проверки не требуется |
| Строковые имена типов EF (`CareerFrameworkEntityConfiguration`, `UserManagementEntityConfiguration`, миграции, snapshot) | расхождение модели и snapshot, непредусмотренная миграция | static: `dotnet ef migrations has-pending-model-changes` | none | команда на этом дереве: exit 0, «No changes have been made to the model since the last migration»; интеграционные тесты применяют миграции к реальной БД и зелёные |
| Контракт `openapi/openapi.json` (`info.title` `Kalibr.Api \| v1`) | контракт расходится с сгенерированным | static: `check:api` (gate impl) + существующие контрактные тесты через `ApiContract` | none | заголовок генерируется при сборке и проверяется `check:api`; тест на литерал `info.title` повторял бы генератор |
| `DataProtectionApplicationName = "kalibr"` и cookie `.kalibr.auth` | однократный сброс сессий и ссылок; рассинхрон имени между конфигом и кодом | существующие `SessionTests`/`AccountLinkTests` (вход, сессия, ссылки сброса и приглашения работают end to end) | none | значения — конфигурация без ветвления; тест, сверяющий литерал, подтверждал бы реализацию, не поведение (§17). Сброс сессий — ожидаемое следствие, зафиксированное в `deploy/README.md` |
| Dockerfile, `deploy/**`, `web/package*.json`, `package*.json` | опечатка в литералах манифестов, сборке образа, имени пакета | static: `rg` по шаблонам имени продукта (Task 2), сборка web | none | тестов на манифесты в проекте нет и новый вид инфраструктуры не вводится (Complication Approval); поведение кода не меняется; существующий `DeploymentTests` проверяет Dockerfile/проекты |
| Литералы тестов (Task 3) | тесты ждут старые имена | существующие тесты Task 3 (`ce7a2ea8`) | keep | повторно не писались; пре-стратегический прогон их покрыл |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | удалений нет: ни один тест не trivial, не дубликат и не flaky на этой поверхности | — |

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| — (новых тестов нет: ни один риск не проходит критерий гипотетического риска `code-style.md` §17) | n/a |

## Suite run

- Command: `dotnet test --solution Kalibr.slnx && npm --prefix web test`
- Scope: full (предохранитель: поверхность затрагивает решение, все `.csproj`, lock-файлы и общий модуль `Kalibr.Platform`)
- Result: pass — .NET: итог 952, сбой 0, успешно 952, пропущено 0 (exit 0, 4m 44s); web: Test Files 83 passed (83), Tests 850 passed (850) (exit 0)
- Lint / build: pass — `npm --prefix web run lint` exit 0 (biome 363 файлов, typecheck чист); `dotnet build Kalibr.slnx --tl:off` exit 0 (предупреждений 0, ошибок 0); `npm --prefix web run build` exit 0
- HEAD: 1718aa70 (финальный прогон шага 8 после memory-fix; предыдущий прогон шага 3 на 5d32467a дал те же 952 и 850, exit 0)

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|

## Memory leftover-term check (entry 1)

Шаблоны имени продукта из `plan.md` «Overview», весь репозиторий кроме `node_modules`, `bin`, `obj`, `dist`, `TestResults`, `.vs`, `.git`, архива спринтов; `.claude/agent-memory/**` включительно.

- (a) Память живых агентов — правят владельцы (memory-fix, шаг 7a):
  - `asd-dev`: `.claude/agent-memory/asd-dev/project_perf-measure-temp-db.md:13` (`Competency.Api.dll`, `Competency.Api/bin/Release/net…`), `:15` (`Competency.Api.dll`), `:17` (`Competency.Api`); `.claude/agent-memory/asd-dev/feedback_hosted-service-ctor-options.md:8` (`Competency.Api`).
  - `asd-tester`: `.claude/agent-memory/asd-tester/project_backend-tests-scratch-build.md:3` (`Competency.Tests`), `:8` (`Competency.Api`, `Competency.Tests`), `:10` (`Competency.slnx`), `:14` (`Competency.Tests/Infrastructure`), `:18` (`Competency.slnx`).
  - Прочие живые каталоги (`asd-advisor`, `asd-architect`, `asd-ba`, `asd-external-review`, `asd-pm`, `asd-reviewer-combined`, `asd-reviewer-documentation`, `asd-reviewer-efficiency`, `asd-ux`): имён продукта нет (`asd-architect/feedback_adr-authoring-review-traps.md:20` — доменное слово).
- (b) Каталоги удалённых агентов (файлы с именем продукта):
  - `asd-dev-critical` (14): `project_ef-left-join-count-and-visibility-facts.md`, `project_contract-reset-and-squash-facts.md`, `project_career-reminder-mail-facts.md`, `project_docker-spike-recipe.md`, `project_desk-merge-contract-facts.md`, `project_migration-scratch-verification-recipe.md`, `project_position-catalogue-facts.md`, `project_packaging-offline-verification.md`, `project_role-set-hr-assignment-facts.md`, `project_password-reset-link-facts.md`, `project_scratch-process-control.md`, `project_vs-debug-build-lock-and-release-config.md`, `project_vs-f5-launch-facts.md`, `project_web-cdp-real-backend-smoke-facts.md`.
  - `asd-tester-critical` (1): `project_competency-test-harness.md`.
  - `asd-reviewer-correctness` (1): `feedback_design-review-derived-actor-paths.md`.
  - `asd-reviewer-testing` (1): `feedback_dotnet-api-test-review.md`.
  - Каталоги без определения в `.claude/agents/` (`asd-architect`, `asd-ba`, `asd-pm`, `asd-ux`, `asd-reviewer-documentation`) имён продукта не содержат.
- (c) `docs/**` — design-promote: `docs/architecture/{org-structure,career-framework,user-management,subsystems,audit,desk,promotion}.md` и `docs/architecture/stack.html`; `docs/architecture/tech-reference/{visual-studio-multiproject-launch-17.11,mailkit-4.18.1,postgres-image-18.6-trixie,mailpit-1.31.4,docker-multi-stage-dockerfile-1,testcontainers-postgresql-4.15.0,sql-postgresql-18.6,nsubstitute-6.2.0,npgsql-entityframeworkcore-postgresql-10.0.3,microsoft-visualstudio-javascript-sdk-1.0.5984942,efcore-10.0.12,dotnet-sdk-image-10.0.401,dotnet-aspnet-image-10.0.12}.md`; `docs/ux/DESIGN.md`.
- (d) Намеренно оставлено: `deploy/dev/README.md:32` (старые том `competency-dev-pgdata` и контейнеры `competency-dev`), `deploy/README.md:94` (старая установка `competency`); артефакты этого спринта `.asd/sprints/007-rename-competency-to-kalibr/**` (audit, sprint, plan, research, decisions-log — описывают переименование); `.asd/project/retro-backlog.md` (id спринта); id прошлых спринтов в `.asd/project/custom-*-rules.md` (`003-positions-career-competency#…`); доменный термин «компетенция» (`openapi/openapi.json`, `src/**`, `web/src/**`, `tests/perf/seed-10k.sql`, таблицы и колонки БД).
- (e) Дефекты: не найдено (вне `docs/**` и памяти живых и удалённых агентов вхождений имени продукта нет).

## Manual verification (optional)

Не требуется: все AC проверяются сборкой, статическими проверками и автоматическими тестами.

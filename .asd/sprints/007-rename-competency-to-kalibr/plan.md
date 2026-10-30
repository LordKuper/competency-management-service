---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

## Overview
Сквозное переименование имени продукта Competency → Kalibr (подход A, `research.md`). Волна 1 — две задачи dev: Task 1 — решение, проекты, production-код, компиляция тестов, lock-файлы, контракт, образ и web; Task 2 — манифесты Kubernetes, dev-стек и руководства `deploy/`. Волна 2 — Task 3 (`asd-tester`, Test-only): строковые литералы тестов. Правила проекта, `commands.yaml`, память агентов и `docs/**` правит оркестратор в точках, названных ниже. Доменный термин «компетенция» не меняется (`sprint.md` «Out of scope»).

Значения, которые обе задачи используют одинаково: имя БД и пользователя PostgreSQL `kalibr`, путь ключей DataProtection `/var/lib/kalibr/keys`, образ `kalibr-app`, Secret `kalibr-secret`, проект dev compose `kalibr-dev`, том `kalibr-dev-pgdata`, профиль запуска `Kalibr.Api`.

Шаблоны имени продукта (охват замены и проверок; все прочие вхождения `competency` — доменные): `Competency.(Api|Audit|CareerFramework|Desk|OrgStructure|Platform|Promotion|UserManagement|Tests)` и их строчные формы в `packages.lock.json`; `Competency.slnx`, `Competency.slnLaunch`; `competency-(app|secret|web|dev|dev-pgdata|images|tests|xxe|management-service)`; `/var/lib/competency/`; `.competency.auth`; `part-of: competency`; `Database=competency`, `Username=competency`; `POSTGRES_USER`/`POSTGRES_DB: competency`; `-U competency -d competency`; `namespace competency`, `-n competency`; `competency.corp.example`; `competency-dev-db-1`; проза «Competency Management Service», «Competency service», «`POSTGRES_USER` (`competency`)». Места, где старое имя остаётся намеренно, перечислены в `sprint.md` «Out of scope», плюс упоминания старых тома и контейнеров в `deploy/dev/README.md` (AC-7) и старых имён установки в `deploy/README.md` (AC-6).

Внешние API: Task 1 — `dotnet restore --force-evaluate`, `dotnet build` с генерацией OpenAPI при сборке (`microsoft-extensions-apidescription-server-10.0.12.md`), `dotnet ef migrations has-pending-model-changes` (`dotnet-ef-10.0.12.md`), `openapi-typescript` (`openapi-typescript-7.13.0.md`), npm (`npm-11.19.0.md`); Task 2 — только переименование значений в существующих манифестах Kubernetes (`stack.html`), compose и образе postgres (`visual-studio-multiproject-launch-17.11.md`, `postgres-image-18.6-trixie.md`). Новых API нет, tech reference достаточно.

## Definition of Done
Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.
После design-promote оркестратор выполняет `rg -i competency` с исключениями AC-13 и сверяет каждое оставшееся вхождение с доменным термином или списком намеренных мест; итоговый список намеренных мест входит в описание PR.

### Task 1: Переименовать решение, проекты, код, контракт, образ и web
AC: AC-1, AC-2, AC-3, AC-4, AC-5 (код), AC-7 (`launchSettings.json`), AC-8, AC-12.
Material risk: change: session reset — новые ApplicationName DataProtection и имя cookie однократно делают недействительными все сессии и выданные ссылки
Material risk: artifact: literal swap across build graph — проверяется сборкой, `has-pending-model-changes`, `check:api` и проверкой удалённых строк diff по шаблонам имени продукта
- [x] Перед переносом остановить по PID только процессы, мешающие ему, из списка `custom-common-rules.md` «Blocking project processes» (запущенный `Competency.Api`, Visual Studio с `Competency.slnx`, его сборочные процессы, `testhost`, Vite из `web/`).
- [x] Отдельным коммитом, без правок содержимого: `git mv` `Competency.slnx` → `Kalibr.slnx`, `Competency.slnLaunch` → `Kalibr.slnLaunch`, каждой папки `src/Competency.<Module>` → `src/Kalibr.<Module>` (Api, Audit, CareerFramework, Desk, OrgStructure, Platform, Promotion, UserManagement) и её `Competency.<Module>.csproj` → `Kalibr.<Module>.csproj`, `tests/Competency.Tests` → `tests/Kalibr.Tests` и `Competency.Tests.csproj` → `Kalibr.Tests.csproj`.
- [x] Удалить неотслеживаемые остатки сборки (`bin/`, `obj/`) в старых папках `src/Competency.*`, `tests/Competency.Tests`, чтобы старых папок не осталось.
- [x] Заменить `Competency.<Module>` (9 имён) на `Kalibr.<Module>` в production-коде и сборке: `src/**/*.cs` (namespace, `using`, `cref`, строковые имена типов EF в `CareerFrameworkEntityConfiguration.cs`, `UserManagementEntityConfiguration.cs`, namespace и `modelBuilder.Entity("…")` в файлах миграций и `AppDbContextModelSnapshot.cs`), все `*.csproj` (`ProjectReference`, `InternalsVisibleTo`), `Kalibr.slnx`, `Kalibr.slnLaunch` (пути и `DebugTarget`), `Properties/launchSettings.json` (имя профиля). В `tests/**/*.cs` — только то, что нужно для компиляции: `namespace`, `using`, `cref` (`custom-coding-rules.md` «Contract surface changes»); строковые литералы тестов — Task 3. ID и имена классов миграций не меняются, новая миграция не создаётся.
- [x] Литералы production: `PlatformModule.cs` `DataProtectionApplicationName = "kalibr"`; `appsettings.json` cookie `.kalibr.auth`; `launchSettings.json` `Database=kalibr;Username=kalibr`.
- [x] `Dockerfile`: `COPY`, `restore`, `publish` для `src/Kalibr.*`, `DataProtection__KeysPath=/var/lib/kalibr/keys`, `ENTRYPOINT ["dotnet", "Kalibr.Api.dll"]`.
- [x] Перегенерировать `packages.lock.json` всех 9 проектов `dotnet restore Kalibr.slnx --force-evaluate` (порядок записей меняется, текстовой замены мало).
- [x] `web/package.json` и `web/package-lock.json`: имя `kalibr-web`; корневой `package.json`: добавить `"name": "kalibr"`, корневой `package-lock.json` — то же имя.
- [x] `dotnet build Kalibr.slnx --tl:off -c Release` без предупреждений и ошибок перегенерирует `openapi/openapi.json` (`info.title` `Kalibr.Api | v1`); затем `npm --prefix web run gen:api`, `check:api`, `lint`, `build` чисты, `dotnet ef migrations has-pending-model-changes --project src/Kalibr.Platform --startup-project src/Kalibr.Api` чист.
- [x] Проверка доменной границы: в удалённых строках `git diff` задачи, кроме строковых литералов тестов из Task 3, каждое вхождение `competency` (без учёта регистра) относится к шаблонам имени продукта из «Overview»; в `openapi/openapi.json` изменилась только строка `info.title`.

### Task 2: Переименовать манифесты Kubernetes, dev-стек и руководства deploy
AC: AC-5 (README), AC-6, AC-7.
Material risk: artifact: deploy literal swap — проверяется `rg` по шаблонам имени продукта в `deploy/`
- [x] `deploy/k8s/app.yaml`: образ `kalibr-app`, все `secretKeyRef.name` `kalibr-secret`, `app.kubernetes.io/part-of: kalibr`, `mountPath: /var/lib/kalibr/keys`.
- [x] `deploy/k8s/configmap.yaml`: `part-of: kalibr`, `DataProtection__KeysPath: /var/lib/kalibr/keys`, `POSTGRES_USER`/`POSTGRES_DB: kalibr`; `deploy/k8s/db.yaml`: `part-of: kalibr`, Secret `kalibr-secret`.
- [x] `deploy/secret.template.yaml`: `name: kalibr-secret`, `part-of: kalibr`, строка подключения `Database=kalibr;Username=kalibr`.
- [x] `deploy/dev/docker-compose.yml`: `name: kalibr-dev`, `POSTGRES_USER`/`POSTGRES_DB: kalibr`, healthcheck `-U kalibr -d kalibr`, том `kalibr-dev-pgdata`; `deploy/dev/docker-compose.dcproj`: `DockerComposeProjectName` `kalibr-dev`.
- [x] `deploy/README.md`: `kalibr-app`, `kalibr-images.tar`, namespace `kalibr` во всех командах, `kalibr-secret`/`kalibr-secret.yaml`, пример `https://kalibr.corp.example`; одна строка: после обновления все пользователи один раз выходят из сессии, неиспользованные ссылки сброса пароля и приглашения недействительны; одна строка: установка со старыми именами (`competency`) не переносится, Kalibr ставится заново.
- [x] `deploy/dev/README.md`: пути `src/Kalibr.Api`, `Kalibr.slnx`, `Kalibr.slnLaunch`, профиль `Kalibr.Api`, проект `kalibr-dev`, том `kalibr-dev-pgdata`; сказано, что новый стек начинает с пустой БД, контейнеры проекта `competency-dev` нужно остановить до первого запуска (порты 15432, 11025, 18025), а том `competency-dev-pgdata` и эти контейнеры пользователь удаляет сам.

### Task 3: Обновить строковые литералы тестов под новые имена
AC: AC-1, AC-2, AC-12 (тесты находят `Kalibr.slnx`, запускают `Kalibr.Api.dll`, ждут категорию журнала `Kalibr.Platform.*`).
Material risk: none
Test-only: tests/Kalibr.Tests/DeploymentTests.cs, tests/Kalibr.Tests/Infrastructure/ApiContract.cs, tests/Kalibr.Tests/Infrastructure/ApiHost.cs, tests/Kalibr.Tests/PlatformTests.cs, tests/Kalibr.Tests/OrgImportParserTests.cs
- [x] `DeploymentTests.cs`: `"Kalibr.CareerFramework"` в ожидании списка проектов, файл решения `Kalibr.slnx` при поиске корня.
- [x] `Infrastructure/ApiContract.cs`: `SolutionFile = "Kalibr.slnx"`; `Infrastructure/ApiHost.cs`: `Kalibr.Api.dll`, временная папка `kalibr-tests-<guid>`.
- [x] `PlatformTests.cs`: `FailureLogCategory = "Kalibr.Platform.ProblemExceptionHandler"`; `OrgImportParserTests.cs`: временный файл `kalibr-xxe-<guid>.txt`.
- [x] `rg` по шаблонам имени продукта в `tests/**/*.cs` пуст.

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | Task 1, Task 2 |
| 2 | Task 3 |

- Task 1 и Task 2 не пересекаются по путям: Task 2 — только `deploy/**`; общие значения заданы в «Overview».
- Task 3 зависит от Task 1: файлы тестов перенесены в `tests/Kalibr.Tests` и компилируются с новыми namespace.

Orchestrator: в impl после волны 1, до шлюза build + lint, правит `.asd/project/commands.yaml` (`Kalibr.slnx`, `src/Kalibr.Api`, `src/Kalibr.Platform`), `custom-coding-rules.md` (`Kalibr.slnx`, `Kalibr.Platform`), `custom-common-rules.md` («Blocking project processes»: `Kalibr.Api`, `Kalibr.Api.exe`, `Kalibr.Api.dll`, `Kalibr.slnx`) и добавляет в `custom-common-rules.md` раздел с правилом AC-16 (AC-16, AC-17).

Orchestrator: на первом входе в impl-test — проверка оставшихся вхождений термина (`artifact-layout.md` «Agent memory») по шаблонам из «Overview» во всём репозитории, `.claude/agent-memory/**` включительно; попадания в памяти `asd-dev` и `asd-tester` — memory-fix владельцу, диспатч `asd-tester` несёт и строки AC-14, AC-19; в каталогах удалённых агентов (`asd-dev-critical`, `asd-tester-critical`, `asd-reviewer-correctness`, `asd-reviewer-testing`) оркестратор удаляет файлы с именами продукта и их строки в `MEMORY.md` (AC-18, AC-14, AC-19).

Orchestrator: в design-promote обновляет `docs/**` — `stack.html`, `subsystems.md`, `<id>.md` подсистем, tech reference, `DESIGN.md` (`name`, заголовок) — новыми именами (AC-10), затем выполняет проверку AC-13 из «Definition of Done».

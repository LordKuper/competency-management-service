---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

An absent optional section below means an empty finding set for that section, never an unperformed check (`.asd/workflows/asd-phase-audit.md` "Rules"). Omit any optional section entirely when it has no findings — never emit a placeholder row.

Метод: затронуто больше 200 отслеживаемых файлов (646 файлов `src/`, `tests/` с `Competency` в пути), поэтому сначала `rg` по шаблонам, затем чтение только найденных мест. Счёт на HEAD 3e5594d2 без `node_modules`, `bin`, `obj`, `dist`, `TestResults`, `.vs`, `.git`, `.asd/sprints`: 7230 вхождений `competency` без учёта регистра, из них 3510 — имя продукта по шаблонам ниже, остальное — доменный термин.

## Scope reference
[sprint.md](./sprint.md)

## Touched areas
- `Competency.slnx`, `Competency.slnLaunch`: пути проектов `src/Competency.*`, `tests/Competency.Tests`; `DebugTarget: Competency.Api` в обоих профилях (AC-1).
- `src/Competency.{Api,Audit,CareerFramework,Desk,OrgStructure,Platform,Promotion,UserManagement}/`, `tests/Competency.Tests/`: имена папок и `.csproj`, `ProjectReference`, `InternalsVisibleTo Include="Competency.Tests"`, `namespace`/`using Competency.<Module>`, `<see cref="Competency.Platform.IDeskSource"/>` (AC-1, AC-2).
- Строковые имена типов EF: `CareerFrameworkEntityConfiguration.cs` (`OrgUnitEntityName`, `PositionEntityName`, `EmployeeEntityName`, `UserEntityName` = `"Competency.OrgStructure.*"`, `"Competency.UserManagement.AppUser"`), `UserManagementEntityConfiguration.cs` (`EmployeeEntityName`) (AC-2).
- `src/Competency.Platform/Migrations/*.Designer.cs`, `AppDbContextModelSnapshot.cs`, `*.cs` миграций: `namespace Competency.Platform.Migrations`, `modelBuilder.Entity("Competency.<Module>.<Type>", …)`, `using Competency.Platform` (AC-2).
- `src/Competency.Platform/PlatformModule.cs`: `DataProtectionApplicationName = "competency-management-service"` — и в `SetApplicationName`, и в пути ключей по умолчанию `%LOCALAPPDATA%\competency-management-service\keys` (AC-5).
- `src/Competency.Api/appsettings.json`: `Authentication:Cookie:Name` `.competency.auth` (AC-5).
- `src/Competency.Api/Properties/launchSettings.json`: профиль `Competency.Api`, `Database=competency;Username=competency` (AC-7).
- `tests/Competency.Tests/`: `DeploymentTests.cs` (поиск корня по `Competency.slnx`, ожидание `"Competency.CareerFramework"` в списке проектов), `Infrastructure/ApiContract.cs` (`SolutionFile = "Competency.slnx"`), `Infrastructure/ApiHost.cs` (`"Competency.Api.dll"`, временная папка `competency-tests-<guid>`), `OrgImportParserTests.cs` (временный файл `competency-xxe-<guid>.txt`), `IdentityRolesMigrationTests.cs` (`using Competency.Api`) (AC-1, AC-2).
- `packages.lock.json` всех 9 проектов: записи проектов `competency.<module>` и зависимости `"Competency.<Module>": "[1.0.0, )"` (AC-3).
- `web/package.json`, `web/package-lock.json`: `competency-web`; корневой `package.json` без `name` — корневой `package-lock.json` берёт `competency-management-service` из имени папки (AC-3).
- `Dockerfile`: 8 строк `COPY src/Competency.*`, `RUN dotnet restore`/`publish src/Competency.Api/Competency.Api.csproj`, `DataProtection__KeysPath=/var/lib/competency/keys`, `ENTRYPOINT ["dotnet", "Competency.Api.dll"]` (AC-4).
- `deploy/k8s/app.yaml`, `configmap.yaml`, `db.yaml`, `deploy/secret.template.yaml`: `competency-app`, `competency-secret` (×6 ссылок), `app.kubernetes.io/part-of: competency` (метаданные и шаблоны подов; селекторы его не используют), `POSTGRES_USER`/`POSTGRES_DB: competency`, `mountPath`/`DataProtection__KeysPath` `/var/lib/competency/keys`, `Database=competency;Username=competency` (AC-6).
- `deploy/README.md`: `-t competency-app`, `competency-images.tar`, `kubectl create namespace competency`, `-n competency` (×9), `competency-secret.yaml`, `https://competency.corp.example` (AC-6).
- `deploy/dev/docker-compose.yml`, `docker-compose.dcproj`, `deploy/dev/README.md`: `name: competency-dev`, том `competency-dev-pgdata`, `POSTGRES_USER`/`POSTGRES_DB: competency`, healthcheck `-U competency -d competency`, `DockerComposeProjectName`, пути `src/Competency.Api`, `Competency.slnx`, `Competency.slnLaunch` (AC-7).
- `openapi/openapi.json`: `info.title` `"Competency.Api | v1"` — производное имени сборки, генерируется при сборке Api (`OpenApiDocumentsDirectory`); остальные 162 вхождения — доменные схемы и значения enum. `web/src/api/schema.d.ts`: только доменные вхождения (AC-8).
- `.asd/project/commands.yaml` (5 строк), `custom-common-rules.md` (1), `custom-coding-rules.md` (2 имени продукта; 2 ссылки на id спринта `003-positions-career-competency` остаются) (AC-17).
- `docs/**` (AC-10) и `.claude/agent-memory/**` (22 файла, AC-11) — перечень в «Existing docs found».
- Память `asd-tester` для AC-14, AC-15: `.claude/agent-memory/asd-tester/` (индекс `MEMORY.md`); `custom-common-rules.md` для AC-16.

## Existing docs found
- [stack.html](../../../docs/architecture/stack.html): 15 вхождений имени продукта — проекты, пути, `Competency.slnLaunch`, ключи DataProtection, cookie.
- [subsystems.md](../../../docs/architecture/subsystems.md): «модуль Competency.<Module>» в Mermaid-диаграмме и строка о `Competency.Platform`/`Competency.Api`; id подсистем (`org-structure`, `user-management`, `audit`, `career-framework`, `desk`, `promotion`, `platform-core`) имени продукта не содержат.
- [audit.md](../../../docs/architecture/audit.md) (7), [career-framework.md](../../../docs/architecture/career-framework.md) (18 из 30, остальное доменное), [desk.md](../../../docs/architecture/desk.md) (4), [org-structure.md](../../../docs/architecture/org-structure.md) (19), [promotion.md](../../../docs/architecture/promotion.md) (3), [user-management.md](../../../docs/architecture/user-management.md) (15): ключевые пути `src/Competency.<Module>/…` и namespace'ы.
- `docs/architecture/tech-reference/`: [docker-multi-stage-dockerfile-1.md](../../../docs/architecture/tech-reference/docker-multi-stage-dockerfile-1.md), [dotnet-aspnet-image-10.0.12.md](../../../docs/architecture/tech-reference/dotnet-aspnet-image-10.0.12.md), [dotnet-sdk-image-10.0.401.md](../../../docs/architecture/tech-reference/dotnet-sdk-image-10.0.401.md), [efcore-10.0.12.md](../../../docs/architecture/tech-reference/efcore-10.0.12.md), [mailkit-4.18.1.md](../../../docs/architecture/tech-reference/mailkit-4.18.1.md), [mailpit-1.31.4.md](../../../docs/architecture/tech-reference/mailpit-1.31.4.md), [microsoft-visualstudio-javascript-sdk-1.0.5984942.md](../../../docs/architecture/tech-reference/microsoft-visualstudio-javascript-sdk-1.0.5984942.md), [npgsql-entityframeworkcore-postgresql-10.0.3.md](../../../docs/architecture/tech-reference/npgsql-entityframeworkcore-postgresql-10.0.3.md), [nsubstitute-6.2.0.md](../../../docs/architecture/tech-reference/nsubstitute-6.2.0.md), [postgres-image-18.6-trixie.md](../../../docs/architecture/tech-reference/postgres-image-18.6-trixie.md) (в т. ч. «`POSTGRES_USER` (`competency`)»), [sql-postgresql-18.6.md](../../../docs/architecture/tech-reference/sql-postgresql-18.6.md), [testcontainers-postgresql-4.15.0.md](../../../docs/architecture/tech-reference/testcontainers-postgresql-4.15.0.md), [visual-studio-multiproject-launch-17.11.md](../../../docs/architecture/tech-reference/visual-studio-multiproject-launch-17.11.md): пути, имена сборок, контейнеров, тома, ключей и cookie в описаниях проверок.
- [DESIGN.md](../../../docs/ux/DESIGN.md): `name: Competency Management Service` и заголовок «Competency Management Service - Design System» — имя продукта (метаданные, не токен); остальные 21 вхождение — доменные («competency-matrix», «critical-competency», уровни оценки).
- [Постановка.md](../../../docs/Постановка.md), [career-framework.html](../../../docs/product/requirements/career-framework.html): только доменный термин — не меняются.
- `docs/product/concept.html`, `docs/ux/design-system.html`, `docs/ux/accessibility.html`, прочие `docs/product/requirements/*.html`: вхождений нет.
- Документы вне раскладки ASD: [deploy/README.md](../../../deploy/README.md), [deploy/dev/README.md](../../../deploy/dev/README.md) — затронуты AC-5..AC-7; это руководства оператора и разработчика, остаются на месте.
- Память агентов `.claude/agent-memory/**`: имена продукта в 22 файлах `asd-dev`, `asd-dev-critical`, `asd-tester`, `asd-tester-critical`, `asd-reviewer-testing`; проза «Competency service» в `asd-tester-critical/project_competency-test-harness.md` (description), «competency service» в `asd-reviewer-correctness/feedback_design-review-derived-actor-paths.md` — не ловятся шаблоном, правятся вручную.

## Contradictions (optional)
- AC-9 (`sprint.md`: `commands.yaml` через `/asd-init` sprint-mediated) vs `.claude/skills/asd-init/SKILL.md` «Sprint-mediated» (меняет только пары `config.yaml`): критерий невыполним, канонической стороны нет; winner=unsettled → user: AC-9 снят, AC-17 — прямая правка `commands.yaml` в impl (решение 2026-10-30, `decisions-log.md`).

## Existing implementation found
- `web/index.html`: `<title>Калибр</title>`, иконка `/brand/logo-64.png`; адреса `admin@kalibr.local`, `noreply@kalibr.local` в dev-настройках — пользовательский бренд уже «Калибр», в интерфейсе менять нечего.
- `AuditSaveChangesInterceptor` пишет в `audit.entity_type` короткое имя типа (`ClrType.Name`), `__EFMigrationsHistory` хранит только `MigrationId` и `ProductVersion`: в данных БД имён namespace нет, смена namespace не требует ни миграции, ни переноса данных.
- Селекторы `Service`/`Deployment`/`StatefulSet` используют только `app.kubernetes.io/name`: смена `part-of` не задевает неизменяемые селекторы. PVC (`data-db-0`, `app-keys`) имени продукта не содержат.
- `UserSecretsId` `Competency.Api.csproj` — GUID без имени продукта: dev-секреты SMTP сохраняются.
- `appsettings.json` `Logging` и `.gitignore` имени продукта не содержат; встроенных ресурсов (`EmbeddedResource`) и чтения манифест-ресурсов по имени нет; CI (`.github/`) нет.
- Инфраструктура ASD (`.asd/rules/`, `templates/`, `workflows/`, `agents/`, `skills/`, сгенерированные `.claude/{agents,skills,hooks}`, `.codex/`, `.agents/skills/`, `AGENTS.md`, `CLAUDE.md`) имени продукта не содержит.
- Память `asd-tester-critical/project_competency-test-harness.md` уже требует закрывать `suite` сразу после прогона (AC-15 сужен до открытия).

## Gaps
- Шаблон имени продукта для замены (не слово competency): `Competency.(Api|Audit|CareerFramework|Desk|OrgStructure|Platform|Promotion|UserManagement|Tests)` и их строчные формы в lock-файлах, `Competency.slnx`/`.slnLaunch`, `competency-(app|secret|web|dev|dev-pgdata|images|tests|xxe|management-service)`, `/var/lib/competency/`, `.competency.auth`, `part-of: competency`, `Database=competency`, `Username=competency`, `POSTGRES_USER/DB: competency`, `-U competency -d competency`, `namespace competency`, `-n competency`, `competency.corp.example`, `competency-dev-db-1`, проза «Competency Management Service», «Competency service», «`POSTGRES_USER` (`competency`)».
- Порядок записей `packages.lock.json` отсортирован по имени: строчные `competency.*` → `kalibr.*` меняют позицию, поэтому lock-файлы перегенерируются `dotnet restore --force-evaluate`, а не только текстовой заменой.
- `openapi/openapi.json` перегенерируется сборкой Api (`info.title` → `Kalibr.Api | v1`), `schema.d.ts` — `npm --prefix web run gen:api`; `check:api` проверяет совпадение.
- Корневой `package.json` не имеет `name` — его надо добавить (`kalibr`) и обновить корневой `package-lock.json`.
- После `git mv` в старых папках `src/Competency.*`, `tests/Competency.Tests` остаются неотслеживаемые `bin/`, `obj/` (выход сборки, не файлы проекта) — удаляются, иначе остаются пустые старые папки; `DeploymentTests.Ac8` их не считает (нет `.csproj`), но они вводят в заблуждение.
- `deploy/dev/README.md` должен предупредить: старые контейнеры проекта `competency-dev` держат порты 15432, 11025, 18025 — их нужно остановить до первого запуска стека `kalibr-dev`.
- Migration gaps: dev-БД: том `competency-dev-pgdata` (пользователь/БД `competency`) → новый пустой том `kalibr-dev-pgdata`, без переноса (решение пользователя); ключи DataProtection dev: `%LOCALAPPDATA%\competency-management-service\keys` → `%LOCALAPPDATA%\kalibr\keys`, старая папка остаётся на машине пользователя; Kubernetes: namespace/Secret/БД `competency` → `kalibr`, существующая установка ставится заново.
- Ретро-строки: в памяти `asd-tester` нет правила о пороге из `docs/architecture/` как контракте (AC-14) и об открытии `suite` перед каждым прогоном (AC-15); в `custom-common-rules.md` нет правила о макетах на scope (AC-16).

## Risks
- Шаблон задевает доменный термин (например, `competency.id` в TS, `competency_id`, `CompetencyBlock`): impact=сломанная предметная модель, контракт или схема БД; mitigation=замена только по перечисленным шаблонам имени продукта, сверка до и после: количество доменных вхождений (7230 − 3510 = 3720 на HEAD 3e5594d2) не меняется, `has-pending-model-changes` чист, `check:api` показывает только `info.title`.
- Блокировки файлов при `git mv` папок на Windows (открытый Visual Studio с `Competency.slnx`, запущенный `Competency.Api`, `testhost`, `MSBuild`, `VBCSCompiler`): impact=частично перемещённое дерево; mitigation=перед переносом остановить только процессы, разрешённые `custom-common-rules.md` «Blocking project processes», по PID; после merge пользователь открывает `Kalibr.slnx`.
- Смена ApplicationName и cookie: impact=однократный выход всех пользователей, недействительны неиспользованные ссылки сброса пароля и приглашения; mitigation=принято пользователем (до 1.0), строка в `deploy/README.md`.
- Dev-стек: impact=пустая dev-БД, конфликт портов со старыми контейнерами; mitigation=предупреждение в `deploy/dev/README.md`; агенты не удаляют том и не выполняют `down -v` (`custom-common-rules.md` «Docker safety»).
- Большой механический diff (~650 файлов с переносом): impact=трудно ревьюировать; mitigation=переносы отдельным коммитом (`git mv` без правок содержимого) до коммита замен, чтобы git видел переименования.

## Subsystems map (optional, decomposition enabled)
- org-structure, user-management, audit, career-framework, desk, promotion, platform-core: в каждой меняются только папка проекта, namespace и ключевые пути `<id>.md`; границы, id и связи подсистем не меняются, новых подсистем нет.

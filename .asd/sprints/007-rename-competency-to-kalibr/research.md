---
responsibility:
  owns: staged-design research for sprint scope (functional requirements, best-practice findings with sources, conceptual approaches and the chosen one)
  excludes: acceptance criteria, technical design, plan, code
  delegates_to: sprint.md (AC-N), design/ docs or plan.md (technical detail), decisions-log.md (decisions)
---

# Research

<!-- Stages, conceptual boundary, no-web-research Findings line: .asd/rules/sprint-lifecycle.md "Staged solution design" — not restated here. -->

## Requirements
Raw scope and stage (a) answers; stage (c) refines the answers.
- Raw scope: Переименовать продукт: везде, где используется старое рабочее название Competency (`Competency`, `competency`), заменить его на Kalibr (`Kalibr` / `kalibr`): решение `Competency.slnx`, `Competency.slnLaunch`; проекты `src/Competency.*` и `tests/Competency.Tests` (папки, `.csproj`, namespace'ы, `packages.lock.json`); `Dockerfile`; `deploy/k8s/*.yaml`, `deploy/secret.template.yaml`, `deploy/dev/docker-compose.yml`, `deploy/README.md`, `deploy/dev/README.md`; `openapi/`, `web/`, `docs/`. Готово, когда `dotnet build` решения под новым именем, `dotnet test`, сборка и тесты `web` зелёные, а `rg -i competency` находит только осознанно оставленные места.
- Problem: старое рабочее название Competency осталось в именах решения, проектов, сборок, namespace'ов, артефактов развёртывания и документации, хотя продукт уже называется «Калибр» (заголовок SPA в `web/index.html`). Слово competency при этом — ещё и доменный термин «компетенция».
- Actors: разработчик (Visual Studio, `dotnet`/`npm` CLI, dev-стек docker compose); оператор развёртывания в Kubernetes (`deploy/`); агенты ASD, читающие `.asd/project/*`, `docs/` и свою память; конечные пользователи — косвенно (однократный выход из сессии).
- Functional requirements:
  - Граница (ответ пользователя): меняется только имя продукта — префикс `Competency.*` проектов, сборок, namespace'ов и решения, а также инфраструктурные и пакетные имена. Доменный термин «компетенция» остаётся: сущности и типы `Competency`, `CompetencyBlock`, `CompetencyScale` и т. п., таблицы и колонки БД (`competencies`, `competency_matrices`, `competency_id`), маршруты API (`/api/v1/career/competencies`), значения enum контракта (`"Competency"`), имена миграций EF.
  - Имена с сохранённым состоянием (ответ пользователя): переименовать все в kalibr — имя БД и пользователя PostgreSQL, том dev-БД, проект dev compose, namespace Kubernetes, Secret, путь ключей DataProtection, ApplicationName DataProtection, имя cookie аутентификации, образ приложения, labels `app.kubernetes.io/part-of`.
  - Dev-БД после переименования начинается с нуля (ответ пользователя): инструкции переноса данных нет, старый том `competency-dev-pgdata` не трогается (удаляет пользователь сам).
  - Имя репозитория (стадия (c)): `name` корневого `package.json` → `kalibr`; репозиторий на GitHub и локальную папку `competency-management-service` пользователь переименовывает сам после merge, вне спринта.
  - Память агентов (стадия (c)): имена продукта в `.claude/agent-memory/**` обновляются точечными правками.
- Constraints:
  - До 1.0 обратная совместимость не поддерживается (`.asd/project/custom-common-rules.md` «No backward compatibility before 1.0»); контракт API остаётся 1.0.0.
  - Инфраструктура воркфлоу (`.asd/rules/`, `.asd/templates/`, сгенерированные `.claude/{agents,skills,hooks}`, `.codex/`, `.agents/skills/`) и архив прошлых спринтов (`.asd/sprints/archived/**`) не меняются.
  - Агенты не выполняют разрушающих Docker-команд против dev-стека пользователя (`custom-common-rules.md` «Docker safety»).
  - Изменение `.asd/project/commands.yaml` — настройка ASD, меняется только через `/asd-init` (sprint-mediated режим, объявленный в плане).
- Success criteria: `dotnet build Kalibr.slnx` и `dotnet test` зелёные; `npm --prefix web run build`, `lint`, `test`, `check:api` зелёные; `has-pending-model-changes` чист; `rg -i competency` (без `node_modules`, `bin`, `obj`, `dist`, `TestResults`, `.vs`, архива спринтов) находит только доменный термин и перечисленные осознанно оставленные места.

## Findings
- ApplicationName DataProtection — дискриминатор изоляции защищённых данных; Microsoft требует держать его одинаковым между развёртываниями приложения. Смена имени делает нечитаемыми ранее выданные cookie, antiforgery- и прочие защищённые payload'ы: однократный выход всех пользователей и недействительные неиспользованные ссылки, подписанные DataProtection. — source: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
- Переименование проекта .NET охватывает папку, `.csproj`, файл решения, корневой namespace и ссылки других проектов; строковые литералы и комментарии обновляются отдельно. — source: https://blog.jetbrains.com/dotnet/2018/11/21/renaming-projects-easy-way-new-refactoring-rider-2018-3/
- Файлы миграций EF Core можно переносить и менять их namespace вручную; новая миграция берёт namespace последней миграции контекста, snapshot — namespace существующего класса snapshot, поэтому namespace миграций и `*ModelSnapshot.cs` меняются вместе, а после переименования проверяется отсутствие ожидающих изменений модели. — source: https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing
- Официальный образ postgres применяет `POSTGRES_USER`/`POSTGRES_DB` только при инициализации пустого каталога данных; существующий том пропускает инициализацию («Database directory appears to contain a database; Skipping initialization»). Новое имя БД/пользователя на старом томе не появится — нужен новый (пустой) том. — source: https://github.com/docker-library/postgres/issues/454, https://station.railway.com/questions/postgre-sql-custom-docker-image-with-volu-aa38e53b

## Approaches
- **A. Одно сквозное переименование**: за один спринт переименовать папки и файлы (`git mv`), namespace'ы, строковые ссылки на имена сборок/типов, решение, инфраструктуру и документацию по точным шаблонам имени продукта (не по слову competency), затем перегенерировать lock-файлы, `openapi.json` и `schema.d.ts` и проверить сборку, тесты и `rg`; pros: согласованное состояние в одном PR, история через `git mv`, единая точка однократного сброса сессий и dev-БД; cons: большой механический diff, ошибка шаблона задевает доменный термин — нужна сверка `rg` по доменным именам до и после.
- **B. Поэтапно**: сначала код (проекты, namespace'ы, решение), инфраструктура и документация — следующими спринтами; pros: меньше diff за раз; cons: промежуточное рассогласование (Dockerfile и README ссылаются на старые пути), удвоенные накладные расходы спринтов, два однократных сброса вместо одного.
- **C. Только файлы и сборки**: переименовать решение, `.csproj` и имена сборок, оставив namespace'ы `Competency.*` и инфраструктурные имена; pros: минимальный diff; cons: имя сборки расходится с namespace, `rg` по-прежнему находит сотни вхождений имени продукта, цель спринта не достигается.
- Recommended: A — единственный вариант, который даёт критерий успеха (`rg` находит только доменный термин) одним согласованным изменением; риск доменных совпадений снимается точными шаблонами и сверкой счётчиков.
- Chosen: A — выбор пользователя 2026-10-30, рекомендованный вариант

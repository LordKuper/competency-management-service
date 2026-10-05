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
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

## 2026-10-05 — Workflow выбран: lite

- **Decision**: Спринт 001 идёт по workflow `lite` (scope → audit → plan → impl ⇄ impl-test → impl-review → design-promote → retro → pr), зафиксирован в `state.json.workflow`.
- **Rationale**: Явный выбор пользователя на жёстком gate `workflow choice`; рекомендация была `standard`.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/state.json`

- 2026-10-05 — retro intake skipped: no archived sprint with retrospective, candidates `[]`
- 2026-10-05 — audit: frozen `true` (greenfield scope, behaviour and contract impact)

## 2026-10-05 — Scope принят: каркас + user-management + org-structure

- **Decision**: Спринт включает каркас проекта, подсистему `user-management` и подсистему `org-structure` (AC-1..14). User→Employee 1:1 необязательно; две роли — глобальный админ (без привязки) и пользователь; штатная единица хранит должность текстом (Career Framework вне спринта).
- **Rationale**: Пользователи привязываются к оргштатке, поэтому подсистемы поставляются вместе (решение пользователя на scope gate); предложение выделить каркас в отдельный спринт отклонено.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`

## 2026-10-05 — Audit: ответы по C-1…C-4, Q-1, Q-2 и поправка scope

- **Decision**: Схема БД применяется приложением при старте. Ставок, штатных единиц, назначений и типов подразделений нет; у сотрудника поля «подразделение» и «должность» (текст); иерархия подразделений — строгое дерево. Роль «пользователь» видит дерево и ФИО, e-mail, подразделение, должность. События входа журналируются, журнал выделен в подсистему `audit` (AC-15). Это поправка scope: изменены AC-5, 8, 9, 10, 11, 12, 13, 14, добавлен AC-15.
- **Rationale**: Явные ответы пользователя на вопросы audit; упрощение модели оргштатки. Допущения вне ответов: статусы сотрудника «работает / не работает» блокируют вход; UI покрывает все мутации подразделений и сотрудников; журнал читает администратор через API и экран.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/audit.md`

- 2026-10-05 — subsystem boundary: `audit` added to sprint scope by user ("Журналирование"); registry write deferred to design-promote

## 2026-10-05 — Audit gate принят

- **Decision**: `audit.md` и поправленный `sprint.md` (AC-1…AC-15) приняты вместе с допущениями: статус сотрудника «работает / не работает», UI всех мутаций, журнал читает администратор через API и экран, `audit` — третья подсистема.
- **Rationale**: Явное согласие пользователя на audit gate; переход к plan (workflow `lite`, фаза design отсутствует).
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/audit.md`, `.asd/sprints/001-project-init-org-structure/sprint.md`

- 2026-10-05 — settings: `project.diagram_tool` none → mermaid via /asd-init (user request); sprint `documents.c4` stays frozen `false`, diagram from next sprint

## 2026-10-05 — plan.md принят

- **Decision**: `plan.md` (Task 1–10, 7 волн) принят. Архитектурные решения (дерево — adjacency list + рекурсивные запросы под advisory-lock; Identity-core + cookie, роль — колонка; Guid v4; Version + ETag/If-Match; CSRF через SameSite=Strict + Origin; журнал с триггером неизменяемости; plain YAML манифесты) приняты на plan gate; `global.json` на SDK 10.0.400.
- **Rationale**: В workflow `lite` нет design, поэтому решения принимаются на plan gate. По просьбе пользователя ручной шаг (Docker Desktop) отложен до Task 10 — Task 1–9 выполняются без Docker и PostgreSQL.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/plan.md`

- 2026-10-05 — stubs: no open stubs in scope
- 2026-10-05 — route Task 1: critical, dispatch HEAD c8a5aad
- 2026-10-05 — route Task 2, Task 3: critical, dispatch HEAD 9425a2c

## 2026-10-05 — Волны 1–2: flagged choices devs и commands.yaml

- **Decision**: Заполнен `.asd/project/commands.yaml` (build = dotnet + npm build, lint = `npm --prefix web run lint`, test = `dotnet test --solution` + `npm test`, run, custom gen-api/check-api/ef-*). Flagged choices Task 1–3 приняты как не противоречащие plan: openapi в `openapi/openapi.json`, миграции в `src/Competency.Platform/Migrations`, `IEntityConfigurationContributor`, guard только на `GetDocument.Insider`, `[Audited]` одним атрибутом (класс — opt-in, свойство — allow-list), `Version` через `EntityStampingInterceptor`, `IfMatch` как handler-параметр, CSRF без сравнения схемы Origin, `withIfMatch` не реализован (ETag возвращает `unwrap`), `standaloneRoutes` для экрана входа, `skipLibCheck: true`.
- **Rationale**: Каждый выбор в пределах plan и persistent docs, без новых зависимостей. Открыто до impl assessment: лицензия MPL-2.0 у `lightningcss` (жёсткая build-time зависимость Vite 8.3.2, не входит в допустимый список stack.html) — решение за пользователем; бэкенд-lint отсутствует (TreatWarningsAsErrors), `npm test` падает до появления тестов (не входит в impl gate).
- **Affected docs**: `.asd/project/commands.yaml`, `docs/architecture/tech-reference/openapi-typescript-7.13.0.md`
- 2026-10-05 — route Task 4: critical, dispatch HEAD 3f92a44

## 2026-10-05 — Волна 3: flagged choices Task 4 (audit)

- **Decision**: Приняты: таблица `audit_events` (snake_case), действия `<EntityType>.Created|Updated|Deleted`, `IAuditWriter` в собственном scope/контексте (события входа переживают откат вызывающего), allow-list только атрибутами, лимиты страниц 50/200 в коде. Для Task 5 добавлены: `JsonNumberHandling.Strict` в Platform (чтобы int в клиенте был `number`) и `InternalsVisibleTo Competency.Tests` в Platform, Audit, OrgStructure (Task 6 — для UserManagement и Api); Task 6 ограничивает длину пользовательского `AuditEntry.Actor`.
- **Rationale**: Решения в пределах plan и AC-5/AC-15; `InternalsVisibleTo` нужен impl-test как реальному вызывающему. Поведение триггера, jsonb и применение миграции на реальном PostgreSQL не проверены (нет Docker) — Task 10 и impl-test прогоняют их первыми.
- **Affected docs**: `src/Competency.Audit/`, `src/Competency.Platform/Migrations/`
- 2026-10-05 — route Task 5: critical, dispatch HEAD 451f39a

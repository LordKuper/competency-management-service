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

## 2026-10-26 — Task 4: отмеченные выборы приняты

- **Decision**: Мастер — в новой папке `web/src/features/org-import/`, маршрут `/desk/org-import` из `desk/routes.ts`; без отдельного диалога перед «Применить»; блок AC-18 по дочерним — только по `missingUnits.parentId`, как на сервере; случай «два администратора уволены вместе» ловит сервер (409 с причиной); `preview`/`apply` через `useMutation`; признак `planChanged` читается из ответа openapi-fetch.
- **Rationale**: Решения на шаге «Отсутствующие» и есть подтверждение; подразделение с дочерним из файла не бывает отсутствующим; серверная проверка набора администраторов уже есть.
- **Affected docs**: [plan.md](./plan.md)

## 2026-10-27 — Поправка скоупа: AC-28 «Перемещение сотрудника», Task 5, волна 4

- **Decision**: На гейте оценки реализации пользователь запросил перемещение сотрудника из подразделения в подразделение вместе с должностью и выбрал: должность вне перечня нового подразделения добавляется в его перечень в той же операции; вход — пункт «Переместить» в меню карточки; перемещается любое назначение, показанное карточкой (основное или совмещение). Добавлены AC-28 (`sprint.md`) и Task 5 в новой волне 4 (`plan.md`). Стоимость критерия: итераций ревью 0, раундов исправлений 0.
- **Rationale**: Сейчас перевод в форме сбрасывает должность, если её нет в перечне нового подразделения; отдельная атомарная операция переносит назначение без ручной правки перечня.
- **Affected docs**: [sprint.md](./sprint.md), [plan.md](./plan.md)
- 2026-10-27 — route Task 5: standard, dispatch HEAD 5a05a5a

## 2026-10-27 — Task 5: отмеченные выборы приняты

- **Decision**: Отказы перемещения — 400 с полем (`toOrgUnitId`/`positionId`), 404 только для неизвестного сотрудника, 412 по `If-Match`; ответ — `EmployeeResponse` без признака добавления в перечень (окно предупреждает заранее); `EmployeeAssignment.OrgUnitId` изменяемый, строка совмещения сохраняет id и связи юрлиц; «Переместить» скрыт у уволенного; при нескольких назначениях карточки окно предлагает выбор; подтверждение снятия с руководства вынесено в `useConfirmUnitLosesHead`.
- **Rationale**: Совпадает с формой ошибок `PUT` сотрудника; одно место для подтверждения снятия с руководства.
- **Affected docs**: [plan.md](./plan.md)

## 2026-10-27 — Поправка скоупа: руководители при импорте (AC-29…AC-31), Task 6 волна 5, Task 7 волна 6

- **Decision**: По запросу пользователя импорт распознаёт кандидатов в руководители по первому слову или сочетанию названия должности (список в AC-30), отдельный шаг мастера «Руководители» показывает результат с выбором; существующего руководителя импорт сам не меняет, но предупреждает о расхождении; при нескольких кандидатах никто не выбран, применение возможно без выбора; руководителем может быть сотрудник с любым назначением в подразделении (выбор пользователя). Изменены AC-1, AC-3, AC-19, AC-28, снята строка «Назначение руководителей импортом» из Out of scope; добавлены AC-29…AC-31. Стоимость критериев: итераций ревью 0, раундов исправлений 0.
- **Rationale**: Ответы пользователя на три вопроса: «Первое слово должности», «Можно, без руководителя», «Разрешить по совмещению».
- **Affected docs**: [sprint.md](./sprint.md), [plan.md](./plan.md)
- 2026-10-27 — route Task 6: standard, dispatch HEAD f5b17aa

## 2026-10-27 — Task 6: отмеченные выборы приняты

- **Decision**: Руководители сохраняются вторым `SaveChanges` в той же транзакции применения (новое подразделение и новый сотрудник-руководитель ссылаются друг на друга), отступление от «`SaveChanges` один раз» в Overview плана; «всё или ничего» сохранено. Решения по руководителям — отдельное поле multipart `heads` (`[{unit, employee|null}]`), `plan.heads` = `{units, employees}` с ключами сотрудников (id или ключ ФИО). Потребители `career-framework` и стол не потребовали правок логики: проверки «руководитель работает в подразделении по основному назначению» там нет. Опции JSON `apply` отклоняют решения без обязательных полей (400).
- **Rationale**: Циклическая ссылка не вставляется одним сохранением; транзакция и блокировка дерева те же. Замер AC-24 с руководителями: preview 3,3–3,6 с, apply 8,1–9,8 с.
- **Affected docs**: [plan.md](./plan.md)
- 2026-10-27 — route Task 7: standard, dispatch HEAD 0bbeee4

## 2026-10-27 — Task 7: отмеченные выборы приняты

- **Decision**: «Применить» перенесено на шаг «Руководители», «Отсутствующие» получило «Далее» (недоступно при блоках AC-18); фильтр «Требуют внимания» статичный (строка не исчезает после решения); диалог снятия с руководства переименован («Снять с руководства?», «Продолжить») и перечисляет несколько подразделений. Значения по умолчанию шага считаются на клиенте из решений «Отсутствующие», `suggested` сервера не используется.
- **Rationale**: Последний шаг перед «Итогом» применяет импорт; подтверждение теперь покрывает и снятие совмещения.
- **Affected docs**: [plan.md](./plan.md)

## 2026-10-27 — Поправка скоупа: русская коллация (AC-32), Task 8 волна 7

- **Decision**: По запросу пользователя («оставь как есть, но локаль надо поменять на русскую») сортировка по названиям и ФИО на сервере переходит на ICU-коллацию `ru-u-ka-shifted` на уровне колонок (миграция, без пересоздания базы); клиентский `Intl.Collator("ru")` получает `ignorePunctuation`. Меняет решение `stack.html` «Русская collation не нужна» — фиксируется в design-promote. Стоимость критерия: итераций ревью 0, раундов исправлений 0.
- **Rationale**: Проверено на dev-базе: ICU `ru` в образе есть; чистая `ru-x-icu` ставит названия в кавычках вперёд, `ru-u-ka-shifted` сохраняет нынешний порядок, который пользователь просил оставить; смена коллации базы целиком требует её пересоздания.
- **Affected docs**: [sprint.md](./sprint.md), [plan.md](./plan.md), [stack.html](../../../docs/architecture/stack.html)
- 2026-10-27 — route Task 8: standard, dispatch HEAD d8423e0

## 2026-10-27 — AC-32 изменён: стандартная коллация `ru`

- **Decision**: Пользователь выбрал стандартную русскую коллацию ICU (`ru-x-icu`) вместо `ru-u-ka-shifted`: знаки препинания значимы, названия в кавычках встают перед остальными. Клиентский `Intl.Collator("ru")` не меняется. Task 8 переадресован работающему dev до завершения. Отменяет вариант из записи «Поправка скоупа: русская коллация (AC-32)».
- **Rationale**: Ответ пользователя «Нет, используй стандартную ru».
- **Affected docs**: [sprint.md](./sprint.md), [plan.md](./plan.md)

## 2026-10-27 — Task 8: сортировки в памяти тоже по русским правилам

- **Decision**: Сортировки по названиям в памяти сервера (`career-framework`, `desk`) переведены с `OrdinalIgnoreCase` на общий `TextCollation.Comparer` (культура `ru-RU`), совпадающий с `ru-x-icu` на проверочных примерах; сортировки импорта для отпечатка остаются `Ordinal`. Runtime-образ `aspnet:10.0.12-noble`, invariant globalization нигде не включён.
- **Rationale**: AC-32 покрывает все списки, которые упорядочивает сервер; иначе «ё» стояла бы после «я».
- **Affected docs**: [plan.md](./plan.md)

## 2026-10-27 — Поправка скоупа: удаление подразделения (AC-33), Task 9 волна 8

- **Decision**: По запросу пользователя добавлено удаление подразделения: только пустое (нет дочерних, сотрудников с любым назначением включая уволенных, данных карьерной модели), в любом статусе, администратором и HR; перечень должностей и закрепление HR удаляются вместе с ним. Удаление через импорт по-прежнему вне скоупа. Стоимость критерия: итераций ревью 0, раундов исправлений 0.
- **Rationale**: Ответы пользователя: «Только пустое», «Нет, любое», «Администратор и HR».
- **Affected docs**: [sprint.md](./sprint.md), [plan.md](./plan.md)
- 2026-10-27 — route Task 9: standard, dispatch HEAD 18890d1

## 2026-10-27 — Task 9: отмеченные выборы приняты, область записи расширена

- **Decision**: Принят порт `IUnitReferences` в `Competency.Platform` (проверка препятствий и снятие закрепления HR реализует `career-framework`); `OrgSnapshot` отбрасывает счётчики и перечни подразделений, которых нет в снимке; 409 несёт `obstacles` (строки), карьерные препятствия — счётчиками без названий. Правка `web/src/api/ApiError.ts` (поле `obstacles`, вне выданной области записи) принята как нужная для показа списка. Документы «подразделения не удаляются» обновляются в design-promote.
- **Rationale**: Модуль оргструктуры не читает таблицы карьерной модели; снимок без блокировки больше не может опираться на «подразделения не удаляются». Проверено: dev-база не тронута, временные базы удалены.
- **Affected docs**: [plan.md](./plan.md), [org-structure.md](../../../docs/architecture/org-structure.md)
- 2026-10-27 — route change-request-1: mechanical, dispatch HEAD 2fe9d43
- 2026-10-27 — impl assessment: change requested — убрать из подтверждения удаления подразделения предложение об условиях удаления и журнале
- 2026-10-27 — impl assessment: change requested — ревизия надписей интерфейса: убрать информацию, нерелевантную пользователю, которому она показывается
- 2026-10-27 — route change-request-2: standard, dispatch HEAD 4ed7796
- 2026-10-27 — impl assessment change-request-2 done: ревизия надписей SPA, 4 коммита (org-structure, org-import, career, desk); сомнительные тексты оставлены

## 2026-10-27 — impl assessment approved

- **Decision**: Пользователь одобрил реализацию (approve): Task 1–9 (AC-1…AC-33), правки по запросам — сокращено подтверждение удаления подразделения и проведена ревизия надписей SPA. Сборка, линт, контракт API и миграции чистые; заглушек нет.
- **Rationale**: Все задачи плана отмечены, отмеченные выборы dev разрешены и записаны выше.
- **Affected docs**: [plan.md](./plan.md), [sprint.md](./sprint.md)
- 2026-10-27 — route impl-test entry 1: standard, dispatch HEAD 78c2550
- 2026-10-27 — memory-fix: удалены устаревшие `.claude/agent-memory/asd-dev-critical/project_pg-behaviour-facts.md` и `project_org-head-rule-facts.md` (агента asd-dev-critical больше нет — владельца нет; факты противоречили collation `ru-x-icu` и правилу руководителя спринта 005)
- 2026-10-27 — impl-test: defects D-1 → impl test-fix (digest 286fcfe5bdcfe6a3a1f63398c87d9f0fa8fc9e96f63674f64dcee6a6536c3776)
- 2026-10-27 — route D-1: standard, dispatch HEAD 2cf1f3c
- 2026-10-27 — D-1 flag принят: `PositionSelect` больше не очищает значение, заданное при первом рендере и отсутствующее в перечне подразделения; очистка — только после смены подразделения. Сохранённую должность и так держит `current`, допустимость проверяет сервер.
- 2026-10-27 — impl test-fix: defects D-1 resolved
- 2026-10-27 — route impl-test entry 2: standard, dispatch HEAD f87c37d
- 2026-10-27 — impl-test: impacted set green (web 74/74, lint ok), 0/0 tests (entry 2; entry 1: +156 dotnet, +75 web)

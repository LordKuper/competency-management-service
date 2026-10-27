---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

An absent optional section below means an empty finding set for that section, never an unperformed check (`.asd/workflows/asd-phase-audit.md` "Rules"). Omit any optional section entirely when it has no findings — never emit a placeholder row.

The main orchestrator writes all applicable sections. Product/domain ambiguity a source can resolve is resolved from it; ambiguity only authority or preference can settle goes to the user.

Проверено на HEAD `8d66ae3`; 726 файлов в `src`, `web/src`, `docs`, `tests` — чтение по плану «сначала grep, затем найденные места».

## Scope reference
[sprint.md](./sprint.md)

## Touched areas
- `src/Competency.OrgStructure/`: сотрудник получает основное назначение (нынешние `OrgUnitId`/`PositionId`) и совмещения с юрлицами (AC-1…AC-7); права HR на изменение (AC-8); импорт: разбор, сверка, предпросмотр, применение (AC-9…AC-24); правила и находки стола, считающие сотрудников подразделения и держателей должности (AC-4, AC-5).
- `src/Competency.Platform/`: политика авторизации (`AuthorizationPolicies.GlobalAdminOrHr` уже есть), миграции (новые таблицы), `IAuditWriter.Stage` для события импорта (AC-21).
- `src/Competency.Audit/`: вид события «Импорт оргструктуры» и его отображение в журнале (AC-21).
- `src/Competency.CareerFramework/`: потребители `EmployeeStatus.OrgUnitId`/`PositionId` и снимка (`MyPlaceEndpoints`, `ProcessRoles`, `CareerDeskScan`, `MatrixRoute`) — по AC-1 читают только основное назначение; точка решения о границе AC-8 (`OrgUnitHrEndpoints`, `OrgUnitSetupEndpoints` — сейчас `GlobalAdmin`).
- `src/Competency.Api/`: приём `multipart/form-data`, пределы тела запроса (Kestrel) для загрузки файла (AC-10).
- `web/src/features/org-structure/`: карточки совмещений и юрлиц, форма сотрудника с совмещениями (AC-3, AC-4, AC-7); замена `isAdmin` на право изменения администратора или HR (AC-8).
- `web/src/features/desk/`: карточка сценария «Импорт оргструктуры» и экран мастера `/desk/…` (AC-9, AC-22); карточки сценариев оргструктуры для HR (AC-8).
- `web/src/app/`: общий механизм несохранённых правок `useLeaveGuard` (AC-22).
- `tests/Competency.Tests/`, `tests/perf/seed-10k.sql`: тесты API и замер AC-24 на существующем сиде.
- `.asd/project/custom-common-rules.md`, `.asd/project/custom-coding-rules.md`: правила AC-25…AC-27.
- `docs/`: требования и описания подсистем `org-structure`, `desk`, `audit`, `stack.html` — обновляются в design-promote.

## Existing docs found
- [Требования — Оргструктура](../../../docs/product/requirements/org-structure.html): AC-10 «сотрудник имеет … должность, подразделение»; AC-22 «у HR доступ только на чтение»; «Перечень … HR его видит, но не меняет (403)»; AC-29 меню карточек только администратору; вне рамок — «Импорт и экспорт структуры, массовые операции, интеграция с HRIS». Спринт меняет все пять мест.
- [org-structure.md](../../../docs/architecture/org-structure.md): порт `IEmployeeDirectory`, `EmployeeStatus(OrgUnitId, PositionId)`, снимок `OrgSnapshot` со счётчиком работающих по паре «подразделение — должность», единая блокировка дерева `pg_advisory_xact_lock(5001001)`, каскад увольнения, `Rejections.PositionProblemAsync`, видимость `OrgAccess.SeesAll`, источник стола только администратору.
- [desk.md](../../../docs/architecture/desk.md) и [Требования — Рабочий стол](../../../docs/product/requirements/desk.html): блок «Что можно сделать» — карточки сценариев, которые SPA строит по ролям (`DeskScenarios.tsx`), маршруты `/desk/…`, общий механизм несохранённых правок для мастеров; бюджет стола на `tests/perf/seed-10k.sql`.
- [stack.html](../../../docs/architecture/stack.html): ClosedXML 0.105.1 выбран для экспорта XLSX, «выбраны, но не подключены — подключать только вместе с первой задачей, которой они нужны»; «Контракт из кода»; «Целостность на уровне данных» (журнал в той же транзакции, блокировки); «Несохранённые правки в SPA».
- [closedxml-0.105.1.md](../../../docs/architecture/tech-reference/closedxml-0.105.1.md): описан только экспорт; «модель книги целиком в памяти»; риск знаний MEDIUM.
- [audit.md](../../../docs/architecture/audit.md), [Требования — Журнал действий](../../../docs/product/requirements/audit.html): журнал изменений через `[Audited]` в той же транзакции и события через `IAuditWriter`.
- [Требования — Карьерная модель](../../../docs/product/requirements/career-framework.html): закрепление ответственного HR за подразделением и «Подключить подразделение» — права глобального администратора.
- [Требования — Управление пользователями](../../../docs/product/requirements/user-management.html): AC-12 роль HR; запрет последнего активного администратора (AC-22).
- [docs/ux/org-structure.html](../../../docs/ux/org-structure.html), [docs/ux/desk.html](../../../docs/ux/desk.html): UX-спецификации карточек оргструктуры и стола; `documents.ux_spec` спринта — `false`.
- [DESIGN.md](../../../docs/ux/DESIGN.md): таблица компонентов, по которой dev сверяет экраны (`custom-coding-rules.md`).

## Contradictions
- `docs/product/requirements/org-structure.html` (AC-10 одно подразделение и одна должность; AC-22 и перечни — HR только читает; AC-29; вне рамок — импорт структуры) vs принятый скоуп `sprint.md` AC-1, AC-8, AC-9: winner=user — скоуп принят пользователем 2026-10-26 (`decisions-log.001.md` «Скоуп принят»), канонический документ обновляется в design-promote.

## Existing implementation found
- `src/Competency.OrgStructure/Employee.cs`: одно `OrgUnitId` и одно `PositionId` — станут основным назначением без переименования для потребителей `EmployeeStatus`.
- `src/Competency.OrgStructure/OrgSnapshot.cs`: `WorkingEmployeeCounts` по паре «подразделение — должность» — один источник для находок стола и предпосылки `WorkingEmployee`.
- `src/Competency.CareerFramework/MyPlaceEndpoints.cs`, `ProcessRoles.cs`: читают `employee.OrgUnitId`/`PositionId` и руководителя через порт — по AC-1 остаются на основном назначении без правок логики.
- `src/Competency.OrgStructure/EmployeeEndpoints.cs`, `OrgUnitEndpoints.cs`, `OrgUnitPositionEndpoints.cs`: мутации под `AuthorizationPolicies.GlobalAdmin`; политика `GlobalAdminOrHr` уже зарегистрирована (`PlatformModule.cs`) и используется справочником должностей.
- `src/Competency.OrgStructure/OrgUnitPositionEndpoints.cs` `POST …/quick-add`: поиск должности без учёта регистра, возврат неактивной в работу, создание — та же операция, что импорт делает для должностей (AC-12, AC-16).
- `src/Competency.OrgStructure/EmployeeImpact.cs`: каскад увольнения одним кодом для подтверждения и выполнения — применим к решениям «Уволить» (AC-18, AC-19).
- `src/Competency.UserManagement/ActiveAdministrators.cs`: правило последнего активного администратора — для предпроверки AC-18.
- `src/Competency.Platform/IAuditWriter.cs` `Stage`: событие в той же транзакции, что изменения, — для события импорта (AC-21).
- `web/src/features/desk/DeskScenarios.tsx`: карточки сценариев по ролям сессии (`isAdmin`, `isHr`, `isAlwaysShown`) — место входа в мастер (AC-9).
- `web/src/app/` `useLeaveGuard`: общий механизм ухода с несохранёнными правками для мастеров (AC-22).
- `tests/perf/seed-10k.sql`: сид на 10 000 работающих сотрудников для замера AC-24.

## Gaps
- Модель совмещений: таблица дополнительных назначений (сотрудник, подразделение, должность) с уникальностью пары у сотрудника; юрлица у основного и у каждого совмещения.
- Справочник юрлиц: сущность, уникальность названия без учёта регистра, быстрое добавление из формы.
- Сотрудники подразделения и держатели должности по всем назначениям: счётчики карточек и дерева, деактивация подразделения (409 при работающих), `impact` должности и перечня, находки стола оргструктуры, поиск по должности (AC-4, AC-5); руководитель и потребители `career-framework` — по основному.
- Права HR: политики мутаций оргструктуры, источник стола оргструктуры (`OrgStructureDeskSource` проверяет `IsGlobalAdmin`), `isAdmin` в UI оргструктуры и в карточках сценариев.
- Импорт: endpoint загрузки `multipart/form-data` (сейчас в API ни одного), разбор xlsx, сверка, расчёт плана и его отпечатка, применение одной транзакцией, событие журнала, мастер в SPA.
- External dependency gaps: ClosedXML 0.105.1: впервые подключается и впервые для чтения, а не экспорта; `tech-reference/closedxml-0.105.1.md` описывает только запись — нужен раздел чтения и защита от ZIP- и XML-бомб до загрузки книги в память.
- Migration gaps: таблицы совмещений, юрлиц и связей назначений с юрлицами: новые, существующие сотрудники получают основное назначение из своих текущих полей без переноса данных.
- Документы (design-promote): требования и описания `org-structure`, `desk`, `audit`, раздел ClosedXML и загрузки файла в `stack.html`.

## Risks
- Тёзки: ключ — ФИО; два сотрудника с одинаковым ФИО, заведённые вручную, блокируют каждый следующий импорт (AC-14): impact=импорт недоступен до переименования одного из них; mitigation=ошибка распознавания называет такие ФИО и ссылки на сотрудников.
- Права HR: HR сможет увольнять и удалять сотрудников, к которым привязаны учётные записи администраторов, и этим блокировать их учётки: impact=HR влияет на доступ администраторов; mitigation=запрет последнего активного администратора остаётся (AC-8), журнал пишет каждое действие; иное ограничение — решение пользователя.
- Объём транзакции: применение на 10 000 сотрудников — одна транзакция под эксклюзивной блокировкой дерева и тысячи строк журнала: impact=на время применения стоят все правки оргструктуры и действия под разделяемой блокировкой; mitigation=наборные операции, замер AC-24 на `seed-10k.sql`.
- Память разбора: ClosedXML держит книгу целиком в памяти; без предела распакованного объёма файл-бомба исчерпает память процесса: impact=отказ приложения; mitigation=проверка записей архива до загрузки (AC-10), лимит тела запроса Kestrel для этого endpoint.
- Отпечаток плана (AC-20): любая правка оргструктуры между предпросмотром и применением отклоняет применение: impact=при параллельной работе администраторов импорт приходится пересчитывать; mitigation=мастер пересчитывает предпросмотр сам и сохраняет решения по сущностям, которые остались в списке.
- UX-документы: `documents.ux_spec=false`, а `docs/ux/org-structure.html` и `docs/ux/desk.html` существуют: impact=спецификации карточек и стола устареют после спринта; mitigation=решение пользователя (принято на гейте scope — документы этого спринта не пропускаются, ux_spec выключен конфигом).
- Потребители основного назначения: «Моё место», матрицы и роли согласования не видят совмещений: impact=совместитель не получит матрицу должности совмещения; mitigation=принятое ограничение AC-1.

## Subsystems map
- org-structure: модель назначений, юрлица, права HR, импорт.
- desk: карточка сценария и экран мастера, карточки оргструктуры для HR.
- audit: событие «Импорт оргструктуры».
- career-framework: потребитель основного назначения; закрепление ответственного HR и «Подключить подразделение» остаются за администратором (решение пользователя 2026-10-26, граница AC-8).
- user-management: роль HR и правило последнего администратора — только чтение.

---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

An absent optional section below means an empty finding set for that section, never an unperformed check (`.asd/workflows/asd-phase-audit.md` "Rules"). Omit any optional section entirely when it has no findings — never emit a placeholder row.

Аудит на HEAD `196e758`, workflow `lite`.

## Scope reference
[sprint.md](./sprint.md) (AC-1…AC-46), [research.md](./research.md) (подход B)

## Touched areas
- Новый модуль повышения (`src/Competency.Promotion/` по образцу остальных модулей, кода нет): волны, этапы, заявки, самооценка, круговая оценка, артефакты, комиссии, итоговые оценки, рейтинг, решения, отметки обратной связи, настройки администратора (AC-1…AC-41).
- `src/Competency.CareerFramework/MatrixContent.cs`, `MatrixLevel.cs`, редактор версии матрицы в `web/src/features/career/`: необязательный требуемый уровень образования по уровню версии (AC-15); чтение опубликованной и архивной версии со снимком составов блоков для оценки (AC-6, AC-29).
- `src/Competency.CareerFramework/ProcessRoles.cs`, `OrgUnitHr.cs`: роли «руководитель подразделения» и «ответственный HR» с наследованием — основа прав HR на волну (AC-1), обязательных членов комиссии (AC-25) и получателей писем (AC-36, AC-38).
- `src/Competency.OrgStructure/IEmployeeDirectory.cs`: сотрудники (`EmployeeStatus`: основное подразделение, должность, `IsActive`), подразделения, предки и поддеревья — дерево выбора подразделений волны (AC-1), правило «2 оценивающих из других подразделений» (AC-11).
- `src/Competency.UserManagement/` (порт `IUserDirectory`): действующая учётная запись кандидата, оценивающих и членов комиссии (AC-8, AC-11, AC-24).
- `src/Competency.Platform/IDeskSource.cs`, `src/Competency.Desk/`: новые виды находок рабочего стола (AC-39).
- `src/Competency.Platform/MailSender.cs`, образец очереди `src/Competency.CareerFramework/CareerMailQueue.cs`: письма волны (AC-35, AC-36, AC-38).
- `src/Competency.Audit/` (атрибут `[Audited]`, `IAuditWriter`): события процесса (AC-40).
- `src/Competency.Platform/Migrations/`: новые таблицы модуля и изменение формы документа версии матрицы.
- `web/src/app/router.tsx`, `features.ts`, `AppShell.tsx`, новая папка `web/src/features/promotion/`: экраны волн, заявок, оценки, комиссии, «Мои заявки», настроек и демо-режима (AC-37, AC-42…AC-44).
- `.asd/project/custom-coding-rules.md`: два правила из ретро (AC-45, AC-46).

## Existing docs found
- [concept.html](../../../docs/product/concept.html): весь процесс повышения — «от самооценки и круговой оценки до комиссии, рейтинга, кадрового решения и обратной связи»; принципы конфиденциальности кандидатур, самостоятельного выдвижения, нескольких источников оценки, «готовность ≠ кадровое решение».
- [requirements/career-framework.html](../../../docs/product/requirements/career-framework.html): формула рейтинга (AC-39: балл блока — среднее долей компетенций; коэффициенты блоков по уровням в процентах), порог готовности по уровню (AC-40), критические компетенции как блокирующее правило (AC-41), статусы версий (AC-42), одна опубликованная версия у матрицы (AC-43), роли процесса (AC-1…AC-10). «Вне рамок»: волны, заявки, оценки, комиссии, рейтинги и решения — расчёт рейтинга по реальным оценкам не выполняется.
- [requirements/desk.html](../../../docs/product/requirements/desk.html): «один объект — один пункт», поглощение и порядок причин (таблицы 1, 2); «Вне рамок»: волны и их действия — «стол устроен так, чтобы их действия добавлялись новыми видами»; письма-напоминания через очередь в памяти, потеря при перезапуске принята.
- [requirements/org-structure.html](../../../docs/product/requirements/org-structure.html): сотрудники, основное назначение и совмещения, статус «работает / не работает», руководитель подразделения.
- [requirements/user-management.html](../../../docs/product/requirements/user-management.html): роли учётной записи (глобальный администратор, HR), привязка к сотруднику; «Вне рамок»: уведомления, кроме перечисленных писем.
- [requirements/audit.html](../../../docs/product/requirements/audit.html): журнал читает только глобальный администратор.
- [architecture/career-framework.md](../../../docs/architecture/career-framework.md): содержимое версии — один `jsonb`-документ (`MatrixContent`: уровни с `MinOverallPercent`, компетенции с типом, веса блоков, требования, общие блоки); у опубликованной и архивной версии снимок составов блоков; миграция, меняющая форму документа, переносит и сохранённые документы; `CareerMailQueue` — `Channel` на 1 000 и `BackgroundService`, письма теряются при перезапуске (решение пользователя 2026-10-13).
- [architecture/desk.md](../../../docs/architecture/desk.md), [org-structure.md](../../../docs/architecture/org-structure.md), [user-management.md](../../../docs/architecture/user-management.md), [audit.md](../../../docs/architecture/audit.md), [subsystems.md](../../../docs/architecture/subsystems.md): границы модулей, порты Platform (`IDeskSource`, `IHrAssignments`, `IUnitReferences`), направление зависимостей.
- [stack.html](../../../docs/architecture/stack.html): модульный монолит, фоновые задачи — `BackgroundService` в процессе API, очереди в памяти; «Несохранённые правки в SPA»; контракт из кода; порядок блокировок.
- [ux/DESIGN.md](../../../docs/ux/DESIGN.md), [design-system.html](../../../docs/ux/design-system.html), [accessibility.html](../../../docs/ux/accessibility.html), [ux/app-shell.html](../../../docs/ux/app-shell.html), [ux/desk.html](../../../docs/ux/desk.html), [ux/career-framework.html](../../../docs/ux/career-framework.html): компоненты, оболочка и навигация, экраны стола и редактора матрицы.
- [tech-reference/](../../../docs/architecture/tech-reference/): справки по всем технологиям стека; новой технологии скоуп не требует.

## Contradictions
- sprint.md AC-9 + AC-27 vs [career-framework.md](../../../docs/architecture/career-framework.md) (`POST /matrices` принимает трек своего или вышестоящего подразделения): матрица принадлежит подразделению, а трек может принадлежать вышестоящему. Цель «уровень трека подразделения волны, у которого есть опубликованная матрица» и «комиссия подразделения, которому принадлежит трек цели» расходятся, если подразделение волны использует трек вышестоящего (комиссия оказалась бы у подразделения вне волны и чужого HR). Канонической стороны нет; winner=unsettled → user: подразделение матрицы — цель и комиссия по матрице подразделения волны (AC-47, AC-48 заменяют AC-9, AC-27).

## Existing implementation found
- `src/Competency.CareerFramework/MatrixContent.cs`, `MatrixLevel.cs`, `MatrixBlockWeight.cs`, `MatrixRequirement.cs`: формула, пороги и типы компетенций хранятся и проверяются на полноту; вычисления рейтинга по оценкам нет.
- `src/Competency.CareerFramework/ProcessRoles.cs`: вычисление ролей «руководитель подразделения» и «ответственный HR» с наследованием и проверкой действующей учётной записи — переиспользуется для прав HR на волну и обязательных членов комиссии.
- `src/Competency.CareerFramework/CareerMailQueue.cs`, `src/Competency.Platform/MailSender.cs`: образец очереди писем после фиксации.
- `src/Competency.Platform/IDeskSource.cs`, `src/Competency.CareerFramework/CareerDeskSource.cs`, `CareerDeskScan.cs`: образец источника находок стола.
- `TimeProvider` внедряется в модули (`MatrixWorkflow`, `DeskBoard`, `AuditEventFactory`) — пригоден для смены этапов по датам и тестов времени.

## Gaps
- Модуля повышения нет: волны, этапы, заявки, оценки, комиссии, решения, отметки обратной связи, их права и экраны — всё новое.
- Нет фонового планировщика: смена этапов по датам (AC-5), фиксация формальной проверки (AC-17), итога круговой оценки (AC-21), «Решение не принято» (AC-32), письма начала и окончания этапов (AC-35, AC-36, AC-38) требуют периодической задачи в процессе API, догоняющей пропущенные переходы после перезапуска.
- Нет хранилища настроек администратора: интервал между повышениями и длительности этапов по умолчанию (AC-41) — новая таблица и экран.
- Нет даты приёма или последнего повышения у сотрудника и уровня образования: дата вносится ответственным HR в заявку (AC-13), образование — кандидатом (AC-10). Список уровней образования — по ч. 4–5 ст. 10 закона № 273-ФЗ (https://legalacts.ru/doc/273_FZ-ob-obrazovanii/glava-2/statja-10/): среднее общее; среднее профессиональное; высшее — бакалавриат; высшее — специалитет, магистратура (один уровень); высшее — подготовка кадров высшей квалификации. Сравнение «не ниже» — по этому порядку.
- Нет расчёта рейтинга и готовности по оценкам (career-framework «Вне рамок»): нужен общий расчёт по версии матрицы, применяемый к средним баллам круговой оценки (AC-21) и к итоговым баллам председателя (AC-29).
- Нет демо-режима: синтетический сценарий по ролям на тех же экранах без запросов на запись (AC-42…AC-44).
- Migration gaps: документ версии матрицы `content` (jsonb) получает необязательный требуемый уровень образования у уровня — сохранённые документы без поля читаются как «без требования», переноса данных не нужно (`custom-common-rules.md` «No backward compatibility before 1.0»).
- External dependency gaps: нет.

## Risks
- Объём: 46 AC, новый модуль и демо-режим в одном спринте `lite` без черновиков дизайна: impact=долгий impl и до 3 волн ревью, правки требований во время приёмки; mitigation=план волнами по этапам процесса, демо — последней волной поверх готовых экранов.
- Часовой пояс дат этапов: «дата завершения» этапа без пояса неоднозначна для фоновой смены этапов: impact=этап закрывается на часы раньше или позже ожидаемого; mitigation=в плане зафиксировать: этап завершается в конце даты по часовому поясу организации из параметров развёртывания.
- Письма через очередь в памяти теряются при перезапуске (принятый риск desk и career-framework): impact=кандидат или HR не получит письмо этапа; mitigation=источник правды — стол и экраны; фоновая задача отправляет письма этапа один раз по зафиксированному переходу.
- Компетенция без оценок («Не наблюдал» у всех оценивающих): средний балл не определён для AC-21 и сводки комиссии: impact=неоднозначность обязательности артефактов; mitigation=решение пользователя: считается непройденной, доля 0, артефакты обязательны (AC-49).
- Конфиденциальность (AC-20, AC-30, AC-37): много ролей с разной видимостью одной заявки: impact=утечка рейтинга, авторов оценок или списка заявок; mitigation=правило AC-46 — план перечисляет маршруты каждой роли и проверку их охраны; тесты прав на каждый endpoint.
- Демо-режим «на тех же экранах» без записи: impact=расхождение демо с реальными экранами или случайные запросы на запись; mitigation=демо подменяет источник данных в SPA, а не копирует экраны; тест, что демо не делает ни одного запроса, кроме чтения сессии.
- Изменение оргструктуры во время волны (увольнение кандидата, оценивающего или члена комиссии, смена руководителя или HR): impact=заявка без оценивающих, комиссия без обязательного члена; mitigation=роли и членство вычисляются в момент действия (как роли процесса); правила для уволенных — в плане.

## Subsystems map
- career-framework: формула, пороги, критические компетенции, роли процесса, новое поле образования в версии матрицы.
- org-structure: сотрудники и подразделения через `IEmployeeDirectory`.
- user-management: действующие учётные записи через `IUserDirectory`.
- desk: новые виды находок через `IDeskSource`.
- audit: события процесса.
- Новая подсистема повышения (`promotion`) в реестре отсутствует; добавляется в `design-promote` с явным согласием пользователя (`artifact-layout.md` «Subsystem registry»).

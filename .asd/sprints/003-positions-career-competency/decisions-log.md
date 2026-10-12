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
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-09 — route Task 1: standard, dispatch HEAD a07cf4a
- 2026-10-09 — route Task 2: critical, dispatch HEAD a07cf4a; risk change: migration, change: public contract

## 2026-10-09 — Task 2 flagged choices

- **Decision**: Приняты: пункт «Добавить должность «…»» — обычная опция списка, а не `popupRender` (доступность с клавиатуры, accessibility.html); счётчики перед деактивацией и исключением — только работающие сотрудники; производные тексты ошибок по образцу «Не удалось …», столбец «Действия», aria-label из утверждённого заголовка, «Должность не найдена»; форма API (204 на повторное добавление, `GET /positions/{id}`, quick-add 200 без требования активности подразделения). Пункт меню «Должности» сейчас только администратору — Task 6 открывает просмотр перечня всем вместе с пунктом «HR». Не принят п. 5: полнотекстовый запрос, смешивающий слово ФИО и слово должности, перестал находить сотрудника — нарушение AC-5 «как прежде»; возврат Task 2 на исправление.
- **Rationale**: Оценка оркестратора по AC-2, AC-5, AC-20 и accessibility.html; производные тексты не меняют утверждённых формулировок.
- **Affected docs**: plan.md Task 2, Task 6
- 2026-10-09 — route Task 2: critical, dispatch HEAD f259005 (re-dispatch, priorTier critical)

## 2026-10-09 — plan revision: Test-only Task 10

- **Decision**: В plan добавлена Test-only Task 10 (волна 2, `asd-tester`): фикстуры `Employee` в `EmployeeCards.test.tsx` и `orgTree.test.ts` под контракт 3.0.0; критерии и прочие Task не меняются.
- **Rationale**: Смена входа `/api/v1/employees` (Task 2, AC-6) ломает typecheck web-тестов, а он входит в `lint`/`build` gate impl; dev не правит тесты. Адаптивное решение оркестратора: объём — две фикстуры, проверка — `npm --prefix web run typecheck`.
- **Affected docs**: plan.md
- 2026-10-09 — Task 2 follow-up accepted (cc0e98d): смешанный запрос ФИО+должность снова находит сотрудника; полнотекстовый путь без GIN-индекса (конкатенация векторов) принят — перф-бюджет org-structure AC-24 (P95 < 2 с на 10 000) проверяется в impl-test
- 2026-10-09 — route Task 3: critical, dispatch HEAD f51aca9; risk change: lock ordering
- 2026-10-09 — route Task 10: standard, dispatch HEAD f51aca9; risk none via npm --prefix web run typecheck
- 2026-10-09 — Task 3 flagged choices accepted: `FindAncestryAsync` включает само подразделение первым (шаг 1 маршрута пропускает его), защита от цикла `CYCLE … USING path`, `FindHeadedUnitsAsync` без фильтра активности (фильтрует вызывающий), «действующая учётная запись» — один раз в `UserStatus.IsActive`; Task 10 COMPLETED (eca4c33), web typecheck и lint чисты
- 2026-10-09 — route Task 4: critical, dispatch HEAD 273d5ee; risk change: security
- 2026-10-09 — Task 4 flagged choices accepted: `IEmployeeDirectory.FindSubtreeAsync` (поддерево для наследования HR без прямого чтения `org_units`); неактивные подразделения вне ролей процесса; GET HR возвращает `assigned` и `inherited` с признаком `isActive` учётной записи (видят все — выносится на impl assessment); PUT/DELETE HR без If-Match, 204; метод «ответственный HR подразделения» — в Task 7; производный текст «Пользователь не найден»
- 2026-10-09 — route Task 5: critical, dispatch HEAD eee40ef; risk change: security, change: migration
- 2026-10-09 — route Task 6: standard, dispatch HEAD eee40ef; risk none via npm --prefix web run lint
- 2026-10-09 — Task 6 flagged choices accepted: запрос HR на каждую видимую карточку (кэш 60 с; пакетный endpoint — при необходимости по итогам ревью), импорт `career/careerApi.ts` из `org-structure`, порядок меню 26; производный текст «Не удалось загрузить HR подразделения»
- 2026-10-09 — Task 5 flagged choices accepted: общий базовый тип записи справочника (плановая «общая модель», шесть типов); руководитель видит все черновики своего подразделения; записи администратора и HR создаются действующими; области уникальности (направления, категории, шкалы — весь справочник; треки — в направлении; уровни — в треке; компетенции — в направлении, общие — одной группой); направление трека, трек уровня и подразделение-владелец неизменяемы; оклад — отдельные endpoints `…/levels/{id}/salary`; «владелец» компетенции = автор; `ListQueries` перенесён в Platform. На impl assessment выносятся: регистр названий 12 категорий и производные серверные тексты (правило AC-20). Заглушка AC-13 (409 для шкалы опубликованной версии) — закрывает Task 7.
- 2026-10-09 — route Task 7: critical, dispatch HEAD 3d51315; risk change: workflow state machine, change: lock ordering
- 2026-10-09 — route Task 8: standard, dispatch HEAD 3d51315; risk none via npm --prefix web run lint
- 2026-10-09 — Task 8 flagged choices accepted: пометка о шкале опубликованной матрицы — по 409 без ошибки поля; категории ведутся в форме компетенции; общий cast пути OpenAPI для однотипных endpoints; «Деактивировать» и для черновиков. Производные тексты UI — на impl assessment. Строка MEMORY.md не добавлена: файл `project_career-catalogue-ui.md` агентом не создан.
- 2026-10-09 — Task 7 flagged choices accepted: пороги — проценты 0–100 на уровне и на паре «уровень — блок»; «критическая без минимального уровня» = требование типа Critical без `requiredScore`; неизменность шкалы — проверка баллов требований по текущей шкале; шаг вышестоящего без согласующего в момент действия передаёт ход HR с решением Skipped (A-6b, без нового действия); согласование с невозвращёнными правками — 409; «автор» = текущий руководитель подразделения матрицы, правило «HR = автор» — по отправителю последнего варианта; чтение неопубликованных версий и истории — администратор, руководитель и HR подразделения, автор, участники решений, текущий согласующий; endpoints версий под `/api/v1/career/matrix-versions/{id}`; публикация активирует черновые уровни, компетенции и шкалу версии (черновая категория остаётся черновой); стоимость правила C-2 на каждом запросе справочника — на ревью. Заглушка AC-13 закрыта (строка stubs.md удалена). Производные серверные тексты — на impl assessment.
- 2026-10-09 — route Task 9: standard, dispatch HEAD 1179ab2; risk none via npm --prefix web run lint
- 2026-10-09 — Task 9 COMPLETED; impl completion gate: dotnet build Release 0/0, web lint, build и check:api чисты; пути диффа волн 1–6 — только назначенные Task (Task 10 — две фикстуры тестов); plan.md — все подзадачи отмечены; stubs.md спринта пуст

## 2026-10-11 — impl assessment: user answers on open items

- **Decision**: Производные тексты интерфейса и серверные сообщения Task 2, 4, 5, 7, 8, 9 приняты как есть; названия 12 категорий — с заглавной буквы; признак `isActive` учётной записи HR в `GET /api/v1/career/org-units/{id}/hr` показывается только глобальному администратору (запрос изменения); цвета уровней матрицы остаются в `matrix.css`. Пользователь пришлёт ещё замечания — оценка impl не завершена.
- **Rationale**: Ответы пользователя на гейте impl assessment.
- **Affected docs**: src/Competency.CareerFramework (GET HR), web/src/features/org-structure (окно и строка HR)

## 2026-10-11 — impl assessment: navigation change request

- **Decision**: Меню шапки: первый пункт «Оргструктура», затем «Карьерная модель», остальные пункты («Пользователи», «Должности», «Журнал») — в пункте «Администрирование» (виден только тем, кому доступен хотя бы один вложенный пункт). Пересматривает раздел «Меню» утверждённых «Текстов интерфейса» plan; новый текст «Администрирование».
- **Rationale**: Замечание пользователя на гейте impl assessment; критерии AC не меняются (AC-19 — экраны и меню по ролям).
- **Affected docs**: plan.md «Тексты интерфейса» → «Меню»; на design-promote — docs/ux/app-shell.html
- 2026-10-11 — route impl assessment change 1: critical, dispatch HEAD 4a13cec; risk change: security
- 2026-10-11 — route impl assessment change 2: standard, dispatch HEAD 4a13cec; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment changes 1 (66793b9) и 2 (209555d) выполнены; приняты: при заблокированном собственном HR не-администратор получает и `inherited: null` (A-5 — ответственного HR нет), `isActive` в конце записи ответа; gate: Release 0/0, web lint, build, check:api чисты; пути — только назначенные
- 2026-10-11 — impl assessment: пользователь сообщил, что «Карьерная модель» и «Администрирование» скрыты под «…» (сжатие горизонтального меню antd: `Menu` с `flex: none` в шапке); route impl assessment change 3: standard, dispatch HEAD a65e3fd; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment change 3 выполнен (de6aacf): `Menu` в шапке — `flex: 1, minWidth: 0` (rc-overflow меряет `clientWidth` своего `<ul>`); факт добавлен в antd-6.6.5.md; web lint чист; в браузере не проверено — проверяет пользователь

## 2026-10-11 — scope amendment AC-9/AC-10 (in progress): directions and tracks ownership

- **Decision**: Пользователь: направление не привязано к подразделению — ведут глобальный администратор и любой HR, руководители подразделений создают черновые направления, действующими их делает администратор или любой HR; трек обязательно принадлежит подразделению, матрица подразделения строится только по треку своего или вышестоящего подразделения. Правило для уровней, категорий, компетенций и шкал — уточняется (вопрос пользователя «что такое уровень?»).
- **Rationale**: Указание пользователя на impl assessment (hard gate «new or changed scope»); стоимость AC-9, AC-10, AC-14: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-9, AC-10, AC-14 (после завершения уточнения)

## 2026-10-11 — scope amendment catalog ownership accepted

- **Decision**: Поправка скоупа принята пользователем: AC-9 (направления, категории, компетенции, шкалы — без подразделения, ведут администратор и любой HR, черновики — руководители; трек — подразделение обязательно), AC-11 (уровни — часть трека, правятся в форме трека), AC-14 (матрица — только по треку своего или вышестоящего подразделения), новые AC-22 (шкала «По умолчанию», подстановка в новую компетенцию) и AC-23 (вкладка «Категории»); допущения о видимости черновиков без подразделения, их использовании в матрицах подразделений их автора и уникальности — приняты. Plan: Task 11 (backend, волна 7), Task 12 (экраны, волна 8); новые тексты — «Категории», «Добавить категорию», «Категорий пока нет», «По умолчанию», «Сделать по умолчанию».
- **Rationale**: Замечания пользователя на impl assessment; ревью ещё не было — стоимость затронутых AC: 0 итераций ревью, 0 раундов исправлений; поправка до точки деления impl-review, `floor_base` не нужен.
- **Affected docs**: sprint.md AC-9, AC-11, AC-14, AC-22, AC-23; plan.md Task 11, Task 12, Overview, «Тексты интерфейса», Dependencies
- 2026-10-11 — route Task 11: critical, dispatch HEAD bc14c17; risk change: security, change: migration
- 2026-10-11 — Task 11 flagged choices accepted, кроме п. 7: «любой HR» — ответственный HR хотя бы одного активного подразделения; уникальность — среди нечерновых и черновиков того же автора для всех типов; черновик в матрице — только если его автор сейчас руководит подразделением матрицы; уровни через прежние endpoints с правами по треку; `scaleId: null` в PUT компетенции — шкала по умолчанию; трекам без подразделения миграция ставит корневое (только dev-данные); фильтр треков для AC-14 — на клиенте. П. 7 не принят: деактивация шкалы по умолчанию оставляет AC-22 («ровно у одной шкалы») невыполнимым — возврат Task 11.
- 2026-10-11 — route Task 11: critical, dispatch HEAD 6ec1457 (re-dispatch, priorTier critical)
- 2026-10-11 — Task 11 follow-up accepted (9b7ca26): деактивация шкалы по умолчанию — 409; новый серверный текст «Шкалу по умолчанию нельзя деактивировать — сначала сделайте по умолчанию другую шкалу»
- 2026-10-11 — route Task 12: standard, dispatch HEAD 9b7ca26; risk none via npm --prefix web run lint
- 2026-10-11 — Task 12 flagged choices accepted, кроме уровней: фильтр компетенций редактора матрицы — не неактивные (правило черновика проверяет сервер); создание черновиков доступно руководителю, HR и администратору; «Сделать по умолчанию» — только для действующих шкал; производный текст «Не удалось загрузить категории»; столбец «Владелец» убран у записей без подразделения. Не принято: уровни остались отдельной таблицей с отдельной формой — AC-11 (поправка 2026-10-11, решение пользователя) требует создания и правки уровней в форме трека; возврат Task 12.
- 2026-10-11 — route Task 12: standard, dispatch HEAD b1fe73e (re-dispatch, priorTier standard)
- 2026-10-11 — Task 12 follow-up accepted (b48b960): уровни создаются и правятся в окне трека (сохранённые уровни правятся сразу во вложенном окне, новые — вместе с треком одной кнопкой «Сохранить», при частичной ошибке окно остаётся открытым с несохранёнными уровнями; удалить можно только несохранённый уровень); производный текст «Уровни». Gate: Release 0/0, web lint, build, check:api чисты; пути — только назначенные Task 11, 12; plan.md — все подзадачи отмечены

## 2026-10-11 — scope amendment filters and categories accepted

- **Decision**: По замечаниям пользователя на impl assessment: AC-23 — удаление категории без связанных компетенций (включая историю версий), физическое, с событием журнала; новый AC-24 — серверные фильтры столбцов в таблицах (перечень — sprint.md AC-24), вынесенные за таблицу фильтры-дубли убираются (уточнение пользователя), поиск по названию остаётся, полные названия в фильтре подразделений, порядок вкладок по зависимостям «Шкалы», «Категории», «Направления и треки», «Компетенции», «Матрицы», «На согласовании». Plan: Task 13 (backend, волна 9), Task 14 (экраны, волна 10).
- **Rationale**: Прочтение замечаний принято пользователем с уточнением о дублирующих фильтрах; стоимость AC-19, AC-23: 0 итераций ревью, 0 раундов исправлений; физическое удаление категории — ещё одно уточнение принципа `stack.html` на design-promote.
- **Affected docs**: sprint.md AC-23, AC-24; plan.md Task 13, Task 14, «Тексты интерфейса», Dependencies
- 2026-10-11 — route Task 13: critical, dispatch HEAD f377000; risk change: public contract
- 2026-10-11 — Task 13 accepted (8574f17): `DELETE /career/categories/{id}` (администратор и любой HR, 409 при ссылках компетенций и истории версий), восемь фильтров списков (точное совпадение подразделения, без дочерних); фильтры `/approvals` и `/drafts` — в памяти после полного списка (ponytail-комментарий, списки короткие); контракт аддитивный, web typecheck чист
- 2026-10-11 — route Task 14: standard, dispatch HEAD 8574f17; risk none via npm --prefix web run lint
- 2026-10-11 — Task 14 accepted (cf3e47f): фильтры столбцов (одиночный выбор, поиск в длинных списках, выпадающий список 360 px), внешние фильтры-дубли убраны, порядок вкладок по AC-24, «Удалить» у категорий; у шкал — отдельный столбец «По умолчанию»; должности по умолчанию — фильтр «Активна»; подразделения в фильтре — плоский список в порядке дерева (одинаковые названия неразличимы — на impl assessment); общий `columnFilters.ts` пока в career. Gate: Release 0/0, web lint, build, check:api чисты; пути — только назначенные Task 13, 14; plan.md — все подзадачи отмечены

## 2026-10-11 — scope amendment delete records accepted

- **Decision**: По замечаниям пользователя на impl assessment: AC-24 — фильтр по подразделению деревом с поиском; новый AC-25 — физическое удаление направлений, треков (с уровнями), уровней, компетенций (с историей), шкал и матриц без опубликованных версий, только без ссылок, с событием журнала и текстами отказов из plan «Тексты интерфейса»; опубликованные и архивные версии не удаляются. Plan: Task 15 (backend, волна 11), Task 16 (экраны, волна 12).
- **Rationale**: Предложение принято пользователем; стоимость AC-24: 0 итераций ревью, 0 раундов исправлений; принцип `stack.html` «физически удаляется только сотрудник» уточняется на design-promote для записей карьерной модели без ссылок.
- **Affected docs**: sprint.md AC-24, AC-25; plan.md Task 15, Task 16, «Тексты интерфейса», Dependencies
- 2026-10-11 — route Task 15: critical, dispatch HEAD 39ffcd8; risk change: persisted data
- 2026-10-11 — Task 15 flagged choices accepted: If-Match удаления матрицы — по версии в работе; удаление трека без поиска по jsonb (ссылки исключает FK матрицы на трек); у категорий — прежнее правило AC-23. П. 3 (гонка «удаление уровня или компетенции» ⇄ правка содержимого черновика, висячий id в `content`) закрывается: правка содержимого версии берёт дерево разделяемо, затем `pg_advisory_lock_shared`-эквивалент `pg_advisory_xact_lock_shared(5003001)` — решение plan «Порядок блокировок» («правки Draft — If-Match без advisory-блокировок») пересмотрено оркестратором; порядок «дерево → 5003001 → строка матрицы» сохраняется. Возврат Task 15.
- 2026-10-11 — route Task 15: critical, dispatch HEAD 034b8ec (re-dispatch, priorTier critical)
- 2026-10-11 — Task 15 follow-up accepted (07304c0): правка содержимого версии — дерево разделяемо → 5003001 разделяемо → If-Match, проверка ссылок под блокировкой
- 2026-10-11 — memory-check: `.claude/agent-memory/asd-dev-critical/project_career-record-deletion-facts.md` содержит идентификаторы спринта и Task (строки 3, 8, 13) — не закоммичено, возврат владельцу (memory-fix)
- 2026-10-11 — route Task 16: standard, dispatch HEAD 07304c0; risk none via npm --prefix web run lint
- 2026-10-11 — memory-fix выполнен (8fe32da): идентификаторы спринта и Task убраны из `project_career-record-deletion-facts.md`, memory-check чист
- 2026-10-11 — Task 16 accepted: удаление в UI через общий `useConfirmDeletion` и кнопку в `EntryActions` (по `canEdit`; у категорий кнопка видна и руководителю на черновике, сервер по AC-23 отвечает 403 — на ревью), удаление матрицы — If-Match версии в работе, фильтр подразделений — `TreeSelect` с поиском; производный aria-label «Подразделение». Gate: Release 0/0, web lint, build, check:api чисты; пути — только назначенные Task 15, 16 и память dev; plan.md — все подзадачи отмечены

## 2026-10-11 — scope amendment deletion blockers accepted

- **Decision**: AC-25 дополнен: отказ в удалении перечисляет до 5 видимых пользователю мешающих записей (вид и название) и ниже «и N других связанных записей» с согласованием числа; невидимые — только в числе; черновик категории может удалить и его автор (единое правило AC-25). Plan: Task 17 (backend, волна 13), Task 18 (экран, волна 14).
- **Rationale**: Замечание пользователя на impl assessment и ответ на вопрос о правах удаления черновика категории (рекомендованный вариант); стоимость AC-23, AC-25: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-25; plan.md Task 17, Task 18, «Тексты интерфейса», Dependencies
- 2026-10-11 — route Task 17: critical, dispatch HEAD 4748273; risk change: security
- 2026-10-11 — Task 17 accepted (dee47e1, память 0e07d49): 409 `DeletionRefusal` с `blockers` (до 5, виды Track, Level, Competency, CompetencyVersion, Matrix; `context` — трек уровня, подразделение матрицы) и `moreCount`; удаление категорий — общее правило `CanDelete`; у шкалы по умолчанию — пустой список; у матрицы — без списка; матрицы названы всегда (читаются всеми). Dev поднимал временную PostgreSQL вопреки правилу проекта — F-4.
- 2026-10-11 — route Task 18: standard, dispatch HEAD 0e07d49; risk none via npm --prefix web run lint
- 2026-10-11 — Task 18 accepted (b77c6be): перечень мешающих записей в окне ошибки (`<ul>`, правило числа — функция `moreBlockersText`), отказ без перечня — прежнее уведомление; правка `web/src/api/ApiError.ts` вне путей Task 18 принята оркестратором и внесена в Task 18 (тело ответа иначе недоступно); формат контекста для прочих видов «… (context)». Gate: Release 0/0, web lint, build, check:api чисты; пути — назначенные Task 17, 18 и память dev; plan.md — все подзадачи отмечены

## 2026-10-11 — impl assessment: dialog titles, scale point value, track dialog spacing

- **Decision**: Заголовки окон карьерной модели: создание — «Новая …», правка — «Изменение …» без названия записи (уточнение пользователя); значение балла шкалы — многострочное поле во всю ширину с автоувеличением; отступ между блоком «Уровни» и кнопками окна трека (и подобные места). Тексты внесены в plan «Тексты интерфейса».
- **Rationale**: Замечания пользователя на impl assessment; критерии AC не меняются (AC-19, AC-13, AC-11 — экраны).
- **Affected docs**: plan.md «Тексты интерфейса»
- 2026-10-11 — route impl assessment change 4: standard, dispatch HEAD be018a7; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment: пользователь — надпись кнопок «Править» во всём приложении заменяется на «Изменить»; plan «Тексты интерфейса» дополнен; объём impl assessment change 4 расширен на `web/src` (кроме тестов)
- 2026-10-11 — impl assessment change 4 выполнен (0e3727c, 1761cc9): заголовки окон, многострочное «Значение» балла (до 500 символов, производный текст «Не длиннее 500 символов»), отступ в окне трека, «Изменить» вместо «Править» во всём приложении; web lint и build чисты
- 2026-10-11 — impl assessment: пользователь — при двух баллах с одинаковым значением шкала не сохраняется без текста ошибки; показать «Баллы не должны повторяться» (уже принятый серверный текст) и прочие ошибки баллов в окне шкалы
- 2026-10-11 — route impl assessment change 5: standard, dispatch HEAD 1fc245b; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment change 5 выполнен (6f1a917): у списка баллов не было места для ошибки поля `points` — добавлены проверка повторов на клиенте и `Form.ErrorList`; в других окнах карьерной модели такой проблемы нет; web lint и build чисты

## 2026-10-11 — scope amendment no financial data accepted

- **Decision**: По указанию пользователя диапазон оклада убирается из карьерных уровней (AC-11, AC-18); финансовая информация в системе не хранится — правило проекта в `custom-common-rules.md` (долговременный дом решения) и пункт Out of scope. Plan: Task 19 (backend, миграция и экраны, волна 15). Риск R-8 (утечка оклада) снимается.
- **Rationale**: Прямое указание пользователя на impl assessment (hard gate «new or changed scope»); стоимость AC-11, AC-18: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-11, AC-18, Out of scope; plan.md Task 19, «Тексты интерфейса», Dependencies; .asd/project/custom-common-rules.md
- 2026-10-11 — route Task 19: critical, dispatch HEAD 9595a64; risk change: migration
- 2026-10-11 — Task 19 accepted (68c8a37, память 5446179): колонки и endpoints оклада удалены (миграция RemoveCareerLevelSalary, Down — пустые колонки), поля «Оклад от/до» убраны, правило «No financial data» в custom-common-rules.md; блок «Уровни» в окне нового трека виден сразу; старые события журнала с окладом остаются (журнал append-only, ветка не выпущена). Gate: Release 0/0, web lint, build, check:api чисты; plan.md — все подзадачи отмечены

## 2026-10-11 — scope amendment simplified levels accepted

- **Decision**: AC-11: уровень — название, статус и порядок в треке (от младшего к старшему); убраны код, признак управленческого уровня, период действия, описание роли, ссылки на предыдущий и следующие уровни; порядок — перетаскиванием в окне трека с подсказкой «Уровни идут от младшего к старшему. Перетащите строку, чтобы изменить порядок»; AC-25: проверки удаления трека и уровня без ссылок между уровнями. Новая зависимость `@dnd-kit` (выбор пользователя вместо встроенного перетаскивания с кнопками; рекомендован встроенный вариант). Plan: Task 20 (волна 16); справка `@dnd-kit` — до диспатча; `stack.html` — на design-promote.
- **Rationale**: Указания пользователя на impl assessment (hard gates «new or changed scope» и «stack direction»); стоимость AC-11, AC-25: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-11, AC-25; plan.md Task 20, «Тексты интерфейса», Dependencies; docs/architecture/tech-reference/ (новая справка); на design-promote — stack.html
- 2026-10-11 — справка `docs/architecture/tech-reference/dnd-kit-6.3.1.md` (asd-architect): @dnd-kit/core 6.3.1, sortable 10.0.0, utilities 3.2.2 (MIT, классическая линия вместо 0.x `@dnd-kit/react` из-за открытой ошибки StrictMode под React 19); тексты диктора — на утверждение пользователю
- 2026-10-11 — тексты перетаскивания уровней утверждены пользователем (plan «Тексты интерфейса» → «Перетаскивание уровней»)
- 2026-10-11 — route Task 20: critical, dispatch HEAD 46d0eec; risk change: migration, change: public contract
- 2026-10-11 — Task 20 accepted (61d1a41): уровень — название, статус, `position` (уникально в треке, полный индекс), миграция SimplifyCareerLevels (порядок по глубине старой цепочки «предыдущий», затем по названию), `PUT /career/tracks/{id}/level-order` (неполный список дополняется текущим порядком; версия трека повышается), `DeletionBlockerKind.Level` удалён, переименование уровня — общий `CatalogNameRequest`; Escape в окне справочника отменяет перетаскивание без закрытия окна; правка `web/vite.config.ts` (порог бандла 1600 kB) вне путей Task 20 принята и внесена в Task 20. Gate: Release 0/0, web lint, build, check:api чисты; plan.md — все подзадачи отмечены
- 2026-10-11 — impl assessment: пользователь — при открытии любого модального окна фокус в первом поле, а без полей — на кнопке действия по умолчанию (во всём приложении); на design-promote — `docs/ux/accessibility.html`
- 2026-10-11 — route impl assessment change 6: standard, dispatch HEAD 7241d74; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment change 6 выполнен (3e9904c): общая обёртка `web/src/app/AppModal.tsx` для всех `<Modal>` — фокус в первое поле при открытии (с ожиданием появления полей после загрузки), без полей — основная кнопка; подтверждения — фокус на OK (умолчание antd 6.6.5); возврат фокуса — умолчание rc-dialog; вопрос «фокус на «Отмена» в подтверждениях удаления» — пользователю. web lint и build чисты
- 2026-10-11 — impl assessment: пользователь — в подтверждениях необратимых действий (удалить, уволить, заблокировать и подобных) фокус при открытии на «Отмена»
- 2026-10-11 — route impl assessment change 7: standard, dispatch HEAD 530a0d0; risk none via npm --prefix web run lint
- 2026-10-11 — impl assessment change 7 (8e73d70): `destructiveConfirm` — фокус «Отмена» в подтверждениях с красной кнопкой; увольнение осталось на OK (кнопка не красная) — не принято: пользователь согласовал «Отмена» для «удалить, уволить, заблокировать»; возврат на правку (увольнение — фокус «Отмена», стиль кнопки прежний)
- 2026-10-11 — impl assessment change 7 follow-up (20c7d63): увольнение — фокус «Отмена» без изменения стиля кнопки, возврат на работу — OK; web lint и build чисты

## 2026-10-11 — impl assessment approved

- **Decision**: Пользователь одобрил результат impl: Task 1–20 (с поправками скоупа 2026-10-11: AC-9, AC-11, AC-14, AC-18, AC-22…AC-25) и правки impl assessment 1–7; переход к impl-test.
- **Rationale**: Gate завершения impl пройден (Release-сборка 0/0, web lint, build, check:api чисты; пути — только назначенные; открытых заглушек спринта нет); ничего не проверялось против PostgreSQL и в браузере — это работа impl-test.
- **Affected docs**: state.json
- 2026-10-11 — route impl-test entry 1: critical, dispatch HEAD 6f01bb8; risk change: migration, change: security, change: lock ordering
- 2026-10-11 — stall: asd-tester-critical impl-test entry 1 (лимит API, без сигнала завершения)
- 2026-10-11 — reconstruction: landed 80f374f, 1192ebf, 2e03ce1 (impl-test entry 1 — запись не считается выполненной); re-dispatched impl-test entry 1 (11 неотслеживаемых web-тестов, test-plan.md нет)
- 2026-10-11 — route impl-test entry 1: critical, dispatch HEAD 2e03ce1 (re-dispatch, priorTier critical); risk change: migration, change: security, change: lock ordering
- 2026-10-12 — impl-test: defects D-1, D-2, D-3, D-4, D-5, D-6 → impl test-fix (digest ea49c0f8610c1c2dc5382032de1a7e28315e8d72f08c1445fba17d94d0cd5f12); D-5, D-6 — память asd-dev-critical (memory-fix владельцу); impacted = полный набор (предохранитель); backend 469 (465 зелёных, 4 красных), web 303 зелёных; перф AC-24: p95 23 мс на 10 000 сотрудниках (разовый замер, постоянного теста нет)
- 2026-10-12 — route test-fix D-1,D-2,D-3,D-4: critical, dispatch HEAD c7c7d3f; risk change: migration (D-2 длина колонки), change: lock ordering (D-1 порядок уровней под уникальным индексом)
- 2026-10-12 — test-fix D-1…D-4 accepted (3ed0b25, 23c909a, a2ec2a5, 7c5912a): перестановка уровней — отдельное сохранение на каждый перемещённый уровень; `matrix_version_decisions.kind` — varchar(32), миграция WidenMatrixDecisionKind; удаление матрицы — проверка If-Match до удаления зависимых строк; проверка уникальности учитывает изменённые в запросе записи (утверждение трека и публикация матрицы)
- 2026-10-12 — memory-fix D-5, D-6 (04864f3): устаревшие факты о `next_level_ids` и поле `position` убраны из памяти asd-dev-critical; статусы D-5, D-6 — fixed (оркестратор)
- 2026-10-12 — impl test-fix: defects D-1, D-2, D-3, D-4, D-5, D-6 resolved; gate: Release 0/0, web lint и build чисты
- 2026-10-12 — route impl-test entry 2: critical, dispatch HEAD 45a0e1f; risk change: migration, change: lock ordering
- 2026-10-12 — smoke check: все 4 строки Manual verification (AC-19 матрица, AC-11 перетаскивание уровней, AC-24 фильтр-дерево, AC-19 фокус окон) — pass, пользователь
- 2026-10-12 — impl-test: impacted set green (полный набор по предохранителю: backend 473/473, web 303/303), 4/0 tests (entry 2); entry 1 — 517 добавлено, 0 удалено

## 2026-10-12 — impl-review division into 3 waves

- **Decision**: Объём impl-review — 303 файла, 54 667 строк, 2 273 854 байт (`main...HEAD`, пороги 3000 / 34 / 180 000) → 3 волны: 1 — backend оргструктуры, платформы, пользователей и хоста, миграции справочника должностей и HR, их тесты, развёртывание, контракт и память агентов (75 файлов); 2 — модуль `Competency.CareerFramework`, его миграции и тесты (128); 3 — web (100).
- **Rationale**: `review-waves` принял деление; темы — по подсистемам, чтобы ревьюер видел связный объём.
- **Affected docs**: reviews/impl/waves.json, state.json
- 2026-10-12 — impl-review wave-1/iter-01: combined CONCERNS (F1 critical — мёртвая константа `BeforeCareer` в тестовой инфраструктуре; F2 low — литерал `btrim` в памяти asd-dev-critical; F3 low — повторы doc-комментариев правила должности), external APPROVE (latched); → impl review-fix (wave-1/iter-01); memory-check отклонил память asd-reviewer-combined (идентификаторы спринта, строки 13, 17) — memory-fix владельцу в том же раунде
- 2026-10-12 — route review-fix wave-1/iter-01: standard, dispatch HEAD 7095e27; risk none via npm --prefix web run lint и dotnet build (F3 — doc-комментарии, F1 — тест, F2 и память ревьюера — memory-fix)
- 2026-10-12 — impl fix for wave-1/iter-01: findings resolved — F3 (779cf49), F1 (c52a603, tester), F2 (427f62b, memory-fix asd-dev-critical); память asd-reviewer-combined исправлена владельцем (memory-check чист), закоммичена оркестратором; gate: Release 0/0, web lint чист
- 2026-10-12 — route impl-test entry 3: standard, dispatch HEAD 9747cb5; risk none via dotnet test (дельта — doc-комментарии, удаление неиспользуемой константы теста, память)
- 2026-10-12 — impl-test: impacted set green (полный набор по предохранителю: backend 473/473, web 303/303), 0/0 tests (entry 3)
- 2026-10-12 — impl-review wave-1/iter-02: combined APPROVE (F1–F3 закрыты), external APPROVE (latched) → волна 1 закрыта; переход к волне 2

## 2026-10-12 — F6 answer: competency owner not shown

- **Decision**: На вопрос ревьюера (wave-2/iter-01 combined F6) пользователь ответил «не показывать»: поле «владелец (пользователь)» убрано из AC-12, автор остаётся в истории версий; F6 закрыт без изменения кода (override).
- **Rationale**: Решение пользователя (hard gate «new or changed scope»); стоимость AC-12: ≥1 итерация ревью (нижняя граница), 0 раундов исправлений.
- **Affected docs**: sprint.md AC-12; reviews/impl/wave-2/iter-01/combined.md
- 2026-10-12 — impl-review wave-2/iter-01: combined CONCERNS (F1 critical тест, F2 medium, F3–F5, F7 low; F6 закрыт решением пользователя), external CONCERNS (1 critical, 2–3 high, 4–6 medium) → impl review-fix (wave-2/iter-01)
- 2026-10-12 — route review-fix wave-2/iter-01: critical, dispatch HEAD 85d43aa; risk change: workflow state machine (external 1 — возврат HR с комментарием), dev chain: external 1–6, combined F2–F5, F7; tester chain: combined F1
- 2026-10-12 — review-fix wave-2/iter-01 dev chain: external 1–6, combined F2–F5, F7 resolved (59f6c9e … 4bc90d8); принят общий фикс If-Match в `IfMatch.ApplyTo` (Platform): устаревший ETag → 412 и при неизменном содержимом, во всех модулях, раньше проверок 409/400 после `ApplyTo`; F5 — `activate` трека согласует его черновые уровни; новые серверные тексты «Укажите допустимый тип требования», «Укажите допустимый тип». Решение external 3 отменяет диспозицию оркестратора 2026-10-09 «Task 7 flagged choices accepted… черновая категория остаётся черновой» (decisions-log.003.md): теперь публикация делает действующими и черновые категории используемых компетенций.
- 2026-10-12 — impl fix for wave-2/iter-01: findings resolved — external 1–6, combined F2–F5, F7 (dev), combined F1 (680d92c, tester); F6 — решение пользователя; gate: Release 0/0, web lint чист
- 2026-10-12 — route impl-test entry 4: critical, dispatch HEAD 79dc3ab; risk change: workflow state machine (возврат HR, полнота при согласовании, If-Match в Platform)
- 2026-10-12 — impl-test: impacted set green (полный набор по предохранителю: backend 487/487, web 303/303), 14/0 tests (entry 4)
- 2026-10-12 — stall: asd-reviewer-combined и asd-external-review wave-2/iter-02 (лимит API, отчётов нет); re-dispatched оба с теми же манифестами
- 2026-10-12 — impl-review wave-3/iter-01: combined CONCERNS (F1–F7), external CONCERNS (F1–F9)
- 2026-10-12 — user decision (combined F5): строки списков матриц и черновых записей получают `version` (аддитивно: `MatrixResponse.inWorkVersion`, `CatalogDraftResponse`); удаление матрицы и согласование черновика отправляют тег, который видел пользователь
- 2026-10-12 — user decision (external F5): ограничение в 200 записей снимается — таблицы справочников карьерной модели с серверным постраничным выводом, выпадающие списки — с серверным поиском по вводу
- 2026-10-12 — route impl review-fix: review_fixes_pending = wave-3/iter-01 (combined F1–F7, external F1–F9)
- 2026-10-12 — route review-fix wave-3/iter-01: critical, dispatch HEAD 539175a; risk change: public contract (version в строках списков, серверные страницы и поиск справочников), dev chain: combined F1–F7, external F1–F9
- 2026-10-12 — review-fix wave-3/iter-01 dev chain: combined F1 (кроме типа `MatrixDiff` — используется после F2/F3), F2–F7, external F1–F9 resolved (1008d69 … fa7f598, память 048a5ec); контракт аддитивно: `inWork`, `withGeneral`, `forOrgUnitId` у списков, `version` у строк матриц и черновиков, `canEdit`/`canDelete` у записей справочников; порядок блокировок не менялся
- 2026-10-12 — user decision: flagged choices review-fix wave-3/iter-01 приняты (блокировка переходов при несохранённых правках, список правок согласующего, окно варианта, версия компетенции, причины неактивности, страницы по 20, ширина 11 × controlHeight); тексты внесены в plan «Тексты интерфейса»
- 2026-10-12 — impl completion gate: `npm run lint` красный — typecheck тестов, сломанных намеренными изменениями (F-6); tester-цепочка на восстановление тестов
- 2026-10-12 — route review-fix wave-3/iter-01 tester: standard, dispatch HEAD 048a5ec; risk none via npm run lint, npm test

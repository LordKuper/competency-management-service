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

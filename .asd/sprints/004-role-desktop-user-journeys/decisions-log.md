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
- 2026-10-13 — route Task 1: standard, dispatch HEAD d54507c
- 2026-10-13 — route Task 2: critical (owner asd-dev-critical — память агента правит только её владелец, artifact-layout.md «Agent memory»), dispatch HEAD d54507c
- 2026-10-13 — route Task 3: critical, dispatch HEAD d54507c
- 2026-10-13 — Task 2 flagged choices приняты оркестратором (в границах AC-22/AC-23 и правил памяти): удалены ссылки на историю (Task/Sprint/MS/AC) в правленых файлах; project_web-cdp-real-backend-smoke-facts.md и project_scratch-process-control.md оставлены с пометкой (на первый ссылаются 6 файлов, во втором — запрет down -v); висячая ссылка [[scratch-host-without-postgres]] допустима правилами памяти
- 2026-10-13 — Task 3 flagged choices приняты оркестратором (в границах ADR-2/org-structure.md): OrgSnapshot — sealed class; отдельная запись PositionStatus; OrgUnitStatus с названием; сброс запомненного снимка при взятии блокировки дерева через порт; WorkingEmployeeCounts/UnitPositionIds; волна 1 завершена (777d50e, f9d2d7e, 746bbe6)
- 2026-10-13 — route Task 4: critical, dispatch HEAD f8a7f56
- 2026-10-13 — Task 4 COMPLETED (c938c16, 2b35ff7, 8d4ac3f); блокировка инвариантов связей — CatalogStore.BeginWriteAsync (дерево 5001001 разделяемо → справочники 5003001 эксклюзивно, шаг 2 порядка stack.html, новых блокировок нет). Flagged choices приняты оркестратором: (1) минимальные перечисления PublicationRefusal — Task 5 расширит; (2) смешанные формы 409 у approve/activate — выровнять в Task 5 (передано в payload); (3) отказ по готовности до IfMatch.ApplyTo — по прецеденту проверки «не черновик», чтобы проигравший в гонке снятий получал 409 (отступление от общего порядка «412 раньше последующих проверок» stack.html ограничено проверками состояния, а не содержимого); (4) идемпотентный повтор POST без If-Match; (5) активация проверяет готовность только при переходе Inactive→Active; (6) hasActivePosition, сортировка OrdinalIgnoreCase; (7) коды полноты и readiness — Task 6. Ожидаемое падение тестов (хранитель создаёт трек действующим, полнота без связей) — для impl-test
- 2026-10-13 — route Task 5: critical, dispatch HEAD fbbd94a
- 2026-10-13 — Task 5 COMPLETED (0453509, 2969c5f, 59c4fca); новых блокировок нет. Flagged choices приняты оркестратором: (1) entries предпросмотра без фильтра видимости — черновики, на которые ссылается версия, читают все, кто видит версию (career-framework AC C-2), фильтруются blockers; (2) права ответственного HR строятся как HR, isAdmin только если HR — сам действующий администратор (крайний случай, без роли в UserStatus); (3) у уровня нет предпросмотра (замыкание пустое); (4) 412 переходов версии — PublicationChanged с необязательным publication, 409 переходов — ProblemDetails; (5) PublicationBlockerKind → PublicationEntryKind (3.1.0 не выпущен); (6) «Принять правки» при отсутствии HR и пустом каскаде публикует, как прежде; (7) порядок проверок записи 404→403→409→409→412(If-Match)→412(тег). Ожидаемое падение тестов (412 без тега, NameTaken, создание черновиком) — для impl-test
- 2026-10-13 — route Task 6: critical, dispatch HEAD de3bc98
- 2026-10-13 — Task 6 COMPLETED (a4ce4dd…0e84dbd). Flagged choices приняты оркестратором: (1) builderStep и readiness необязательные в схеме; (2) /…/{id}/matrices — неархивные читаемые версии матриц, использующих запись; (3) неиспользуемые: компетенция через варианты, категория через snapshot, шкала буквально по AC; (4) подключение: «Заблокирована» важнее «Приглашена», «пустой перечень» — ни у подразделения, ни у вышестоящих нет должностей (источник org-structure Task 8 считает так же — передано в payload), positions {own, inherited}, reason — код; (5) «Моё место»: действующие уровни действующих треков, HR — только действующая учётная запись, requirements = null без опубликованной матрицы; (6) состояние приглашения через IsRegistered, срок ссылки — внутри user-management; (7) ProcessRoles.NearestAssigned/AssigneesAsync для повторного использования; (8)-(9) — для impl-test
- 2026-10-13 — route Task 7: critical, dispatch HEAD 0e84dbd
- 2026-10-13 — route Task 8: critical, dispatch HEAD 0e84dbd
- 2026-10-13 — Task 7 COMPLETED (e6ac477, c2e1e6e); письма только после CommitAsync, новых блокировок нет. Flagged choices приняты оркестратором: (1) строка «Это автоматическое письмо…» оставлена (текст UX-каталога); (2) единый текст письма о возврате по макету m-mail — уточнение для «только комментарий» оставлено на ревью; (3) «От кого» — ФИО сотрудника, без e-mail; (4) несколько подразделений автора — первое по названию в зоне HR; (5) роль актора в Mail.SendFailed по образцу PasswordResetQueue
- 2026-10-13 — Task 8 COMPLETED (6fbdfe1, e92a3cd, 96889fe, 125f9c0). Flagged choices приняты оркестратором: (1) строка COPY в Dockerfile и tests/Competency.Tests/packages.lock.json — вынужденные пути сборки нового проекта (gate путей: допущено); (2) OrgSnapshot.UnitDeskObject — единое именование подразделения для слияния (вызыватели — три источника); (3) коды групп DeskGroup и справочные подгруппы по UX; (4) один вид на 5.2/6.1 и на 26.1–26.4/27.1, 28.6 — три члена; (5) DeskDetails расширяется аддитивно в Task 9; (6) 28.5 — только незаблокированные; (7) отменённая ссылка приглашения — без давности (момент отмены не хранится; правило C-2: давность только у событий с датой) — решение оркестратора по источнику; (8) определения 28.7 и 28.4; (9) limit по умолчанию 10, неизвестная группа — 400; (10) desk.md не менялся. Индексные строки MEMORY.md Task 7, Task 8 добавлены оркестратором
- 2026-10-13 — route Task 9: critical, dispatch HEAD 66c39ad
- 2026-10-13 — Task 9 COMPLETED (075cc8d, 67637e7, d39ff81, 42f2e83); оценка ≤ 21 SQL-команды на запрос стола (лимит 25), замер — в impl-test. Flagged choices приняты оркестратором: (1) HrUnitsAsync и SuperiorApproverAsync читают снимок/наборно — изменение путей действий в пользу бюджета; (2) новые поля DeskDetails; (3) объект находок о версиях — матрица, цель — версия; (4) давность черновика — от последнего решения или создания; (5) справочное 5.3 без условия действующего трека (по AC-5.3); (6) правило «трек есть» продублировано в MyPlaceEndpoints (SQL) и источнике стола — отмечено для impl-review (SSoT); (7) свои черновики 26.8 — любому автору; (8) неиспользуемые — роль HR; (9) поглощение находок чернового трека правилами стола; (10) сид использует хэш пароля администратора стенда
- 2026-10-13 — route Task 10: critical, dispatch HEAD 42f2e83
- 2026-10-13 — route Task 11: standard, dispatch HEAD 42f2e83
- 2026-10-13 — Task 11 COMPLETED (2d7b2be). Flagged choices приняты оркестратором: заголовок отказа предпросмотра собирается на клиенте; тексты причин NoRight/NoLevels/NoActivePosition/NameTaken — авторские (правило утверждения текстов снято 2026-10-13), сверка на impl-review; message.success только при непустом перечне; проп afterEdit у EntryActions; KIND_ACCUSATIVE перенесён; предложение «создана черновиком» не для треков и не при 403/404. Путь вне задачи web/src/api/ApiError.ts (поле publication) допущен gate путей как необходимый для 412 «перечень изменился»
- 2026-10-13 — Task 10 COMPLETED (21c2f6e). Flagged choices приняты оркестратором: (1) обработчик 401 в router.tsx, проверка цели в features/auth/returnTo.ts; (2) MutationCache в api/queryClient.ts (по tech-reference); (3) NavItem.badge; (4) временные ссылки стола без прямого маршрута (новая версия → страница матрицы, трек → /career, подразделение → /org-structure?unit=) — пересмотр в Task 12–13; (5) карточки «Доработать»/«Согласовать» — первый ожидающий пункт; (6)-(7) авторские названия подгрупп и формулировки причин (правило утверждения текстов снято) — сверка на impl-review; (8) раскрытие «К сведению» кнопкой с aria-expanded; (9) без returnTo для / и /desk; (10) deskPaths/BUILDER_STEPS — общая точка для Task 12–13. Предупреждение сборки vite «chunk > 1600 kB» (1 601,5 kB) — gate impl не проходит с предупреждением: устранение ленивой загрузкой маршрутов поручено Task 12
- 2026-10-13 — route Task 12: standard, dispatch HEAD 21c2f6e
- 2026-10-13 — Task 12 COMPLETED (1ce5599, 98d0901); сборка без предупреждений (ленивые маршруты, наибольший чанк 1 093 кБ). Flagged choices приняты оркестратором: «Поправить и утвердить» — существующие окна записей; отказ 409 в очереди — окном usePublishEntry; новая версия без блоков получает блок «Основной блок» (isMandatory=false) на шаге 2 — вынесено на impl assessment; «Нет прав» — ссылка на ответственного HR; выбор трека без trackId — первые 200 с canEdit; useBlocker для несохранённых правок мастера
- 2026-10-13 — route Task 13: standard, dispatch HEAD 98d0901
- 2026-10-13 — Task 13 COMPLETED (3d1b814, 5f25118 — по возврату оркестратора: блокировка ухода при несохранённом вводе на шаге 1 «Принять сотрудника» по правилу Mutating UI flows; сводка «Не хватает» считает только Missing). Flagged choices приняты: фильтр очереди по группам стола; список сотрудников неактивной должности — первые 200; ?unitId= в «Принять сотрудника»
- 2026-10-13 — impl completion gate: dotnet build Release 0/0, web build без предупреждений, lint чисто, check:api чисто, ef pending changes нет; пути диффа d54507c..HEAD — в границах задач (Dockerfile, tests/Competency.Tests/packages.lock.json, web/src/api/ApiError.ts допущены ранее)

## 2026-10-13 — поправка скоупа на приёмке реализации: компактность, возврат на шаг, «Профессиональная область»

- **Decision**: пользователь на impl assessment потребовал компактные экраны сценариев без лишней информации (AC-35), возврат на предыдущий шаг в каждом сценарии — «Назад» без потери введённого в многошаговых, возврат к выбору другого объекта в сценариях с выбором (AC-36), переименование «профессиональное направление» → «профессиональная область» (UI «Область»; API и БД без изменений) (AC-37). Пример пользователя — «Связать должности с треком»: нельзя сменить трек после выбора, экран не помещается, крупная плашка предупреждения, заголовок дублирует хлебные крошки. Task 14 (UX-документы) и Task 15 (требования) — волна 10, Task 16 (SPA и тексты писем) — волна 11.
- **Rationale**: hard `new or changed scope`, указание пользователя; стоимость AC-35…AC-37: 0 итераций / 0 раундов исправлений (новые критерии, impl-review ещё не было); impl assessment повторится после Task 14–16.
- **Affected docs**: sprint.md, plan.md
- 2026-10-13 — route Task 14: standard (owner asd-ux — docs/ux), dispatch HEAD a56dd3c
- 2026-10-13 — route Task 15: standard (owner asd-ba — docs/product), dispatch HEAD a56dd3c
- 2026-10-13 — поправка скоупа (user): AC-38 — линейка шагов в каждом многошаговом мастере (всего шагов, названия, текущий, пройденные и оставшиеся, переход на пройденный шаг); добавлена в Task 14, 15, 16 до их диспатча; стоимость AC-38: 0 итераций / 0 раундов исправлений
- 2026-10-13 — Task 14 COMPLETED (71edf58): раздел #compact-rules (К1–К6, В1–В4, Л1–Л5) и макеты в docs/ux/desk.html, 65 замен в career-framework.html; app-shell, user-management, DESIGN.md без вхождений. Flagged choices приняты оркестратором: страница 10 строк для каталога и доступных должностей, ориентир 1366×768; «Другая версия ▾» из пунктов группы стола; строка Steps в DESIGN.md не менялась
- 2026-10-13 — Task 15 COMPLETED (872628c)
- 2026-10-13 — route Task 16: standard, dispatch HEAD 71edf58
- 2026-10-13 — Task 16 COMPLETED (30d8f60, c3a3dbe, d18a239, 4cb0095, d109a43, 696be64, ca0f578, 57c005d, 2a495f8 — по возврату оркестратора: текст отказа MatrixCatalog «…не из области матрицы», AC-37 покрывает серверные тексты, показываемые в UI). Flagged choices приняты оркестратором: (1) выбор трека/подразделения в пути существующих маршрутов (/desk/track-positions/{trackId}, /desk/connect-unit/{unitId}), а не в query — ссылки стола и писем уже их используют; docs/ux/desk.html В2 и m-link выровнены оркестратором; (2) блокировка выбора записи в очередях — только в очереди оргструктуры (правка на месте), в очередях черновиков и каталога правка — окнами; (3) отказ «Часть записей утвердить нельзя» и 409 DecisionError оставлены с description (четыре части); (4) «Другая версия ▾» из первых 10 пунктов группы стола; (5) необязательные пропсы scenarioActions/onDirtyChange/disabled в EmployeeForm, OrgUnitSelect, UserForm (пути вне задачи допущены — необходимы для компактных форм сценария, режим по умолчанию не изменён). Тесты со старыми текстами («Направление», MatrixCreationTests) — на impl-test
- 2026-10-13 — impl completion gate (после Task 14–16): dotnet build Release 0/0, web build без предупреждений, lint чисто, check:api чисто, ef pending changes нет; пути вне задачи Task 16 (EmployeeForm, OrgUnitSelect, UserForm) допущены
- 2026-10-13 — impl assessment: request changes (user, скриншот «Связать должности с треком»): название варианта со статусом не помещается в поле выбора (тег статуса обрезан); проверить и исправить во всех выборах SPA. Возврат к Task 16 (цикл шага 7), поправка требований не нужна — дефект вёрстки AC-35
- 2026-10-13 — route Task 16 assessment-fix: standard, dispatch HEAD 5aa97b8
- 2026-10-13 — Task 16 assessment-fix COMPLETED (da92410): OptionWithTag (имя с многоточием и title, тег не сжимается) в выборе трека — единственный выбор «имя + тег»; глобально popupMatchSelectWidth=false с ограничением max-width min(90ch, 90vw); EmployeePicker — строки с многоточием; аудит остальных Select — строковые подписи. Flagged choices приняты оркестратором: глобальное правило ширины списка; ограничение в ch/vw (не токен — размер раскладки, не визуальный токен). Проверка: скриншот headless 1366×768, имя ~110 символов. Web lint/build/check:api чисто; пути web/src/app/{OptionWithTag,Providers}.tsx, styles.css, users/EmployeePicker.tsx допущены (общий компонент выбора)
- 2026-10-13 — impl assessment: request changes (user, скриншот «Связать должности с треком»): кнопка «К рабочему столу» в строке контекста не нужна — к столу ведут крошка и меню, место кнопки странное; найти подобное везде. Решение оркестратора: в экранах сценариев в работе нет отдельной кнопки возврата на стол (строка контекста, панель действий, шаг 1 «Принять сотрудника» — «К рабочему столу»/«Отмена»); возврат в другой сценарий, из которого пришли («К построению матрицы», «К подключению подразделения», «К очереди черновиков»), — промежуточная крошка «Рабочий стол / {исходный сценарий} / {сценарий}», не кнопка; кнопка «К рабочему столу» остаётся только в пустых и завершённых состояниях (пустая очередь, нет данных о месте, итог «Принять сотрудника», решение уже принято), где других действий нет. UX-документ — Task 14 assessment-fix 2 (asd-ux), SPA — Task 16 assessment-fix 2 (asd-dev)
- 2026-10-13 — route Task 14 assessment-fix 2: standard (owner asd-ux), dispatch HEAD 0ff78b7
- 2026-10-13 — route Task 16 assessment-fix 2: standard, dispatch HEAD 0ff78b7

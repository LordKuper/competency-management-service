[REVIEW-impl-testing]: APPROVE

# Review — testing

- **Phase**: impl-review
- **Iteration**: wave-1/iter-03

Порог high: учитываются только high/critical. Объём — приращение с HEAD итерации 2: `tests/Competency.Tests/DeskQueryCountTests.cs` (прогрев перед окном `large`), `MatrixAccessTests.cs` (+1 тест), `PublicationCascadeTests.cs` (+1 тест, помощник `Blockers`), `test-plan.md` (запись 4) и новый сегмент `test-plan.entry-03.md` (ротация записи 3 вместе со строкой TST-1). Сборка и прогоны не запускались (у ревьюера нет оболочки, `src/*/bin` заняты пользователем): записи прогонов сверены арифметикой и перепроигрыванием по исходникам.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

## Что проверено

- Решения записи 4 против списка правок ревью-фикса из `decisions-log.md`: external 1 (`CatalogStore.ViewerAsync`) — add; external 2 (`CheckAsync` возвращает прочитанные статусы, `PublicationPlan.Visible`) — add; перенос активации (`Apply`, `CatalogEndpoints.PublishAsync`) — keep с мутациями; перф чтений каталога у HR — none; TST-1 — keep; документы — none. Правки без строки нет. Уровень (HTTP на реальном PostgreSQL) соответствует риску «право чтения черновика»; отдельный тест предиката `Sees<T>` дублировал бы эти проверки.
- `MatrixAccessTests.Ac18_TheDraftTrackAndLevelsOfAVersionInWork_…` (MatrixAccessTests.cs:251-280): по `CatalogViewer.Sees<CareerTrack>` и `Sees<CareerLevel>` (CatalogViewer.cs:85-94) и `CatalogStore.ViewerAsync` (CatalogStore.cs:62-76) руководитель отдела с ролью HR вне `unitIds` читает трек и уровни команды только через `RouteEntryIds`, а `SiblingHr` вне маршрута — нет; положительные и отрицательные читатели есть для одиночного чтения трека, уровня и обоих списков. Мутанты T1A–T1F отображаются на эти ветки; тексты `because …` в записи совпадают дословно с текстом ассертов (строки 260 и 272), порядок первого срабатывания каждого мутанта по телу теста сходится.
- `PublicationCascadeTests.Ac109_Ac110_ABlockerOfALevelOutsideTheVersion_…` (PublicationCascadeTests.cs:242-272): по `CatalogPublication.CheckAsync` (CatalogPublication.cs:161-207) помеха `NameTaken` возникает у обоих одноимённых уровней, потому что оба временно активны; согласующий не читает уровень вне состава, поэтому видит одну помеху и `moreCount = 1`. Оба вызова `Visible` (`Describe` — предпросмотр, `Refuse` — отказ) проверены отдельно; читатели полного перечня (ответственный HR, администратор) страхуют от «скрыто всё». Результат «до правки» (две помехи у согласующего) согласуется с прежним кодом, не возвращавшим статусы.
- Fail-first: команда, exit 2 и 2 красных в копии родителя `0de4ecc` записаны; число мутационных прогонов (`runs: 6` и `runs: 4`) равно числу перечисленных мутантов.
- `keep` про перенос активации: все названные тесты существуют (grep по `tests/`); «130 тестов» сходится (99 `[Fact]` + 4 строки `[InlineData]` + 9 теорий по 3 вида из `Kinds` = 130); сообщение мутанта `M3A` для `CompetencyTests.Ac12_EverySavedChange_…` совпадает с ожидаемой последовательностью `Inactive, Active, Draft, Draft` (CompetencyTests.cs:126) при несделанном корне `Active` — прогон правдоподобен.
- `none` про число SQL-команд у HR: `CatalogStore.ViewerAsync` вызывают только конечные точки каталога и матриц, а `CareerDeskSource` строит `CatalogViewer` сам (CareerDeskSource.cs:51) — утверждение верно, бюджет стола не затронут.
- TST-1 (DeskQueryCountTests.cs:27-29): прогрев перед `large` симметричен прогреву перед `small`. Журнал консоли идёт одной очередью по порядку, а команды EF предыдущего запроса попадают в неё до его ответа, поэтому ожидание строки `- 200` прогревочного запроса догоняет журнал до снимка `before`. Фоновые очереди окно не загрязняют: `PasswordResetQueue` пополняется только из `AuthEndpoints.cs:244`, `CareerMail` вызывают только конечные точки каталога и матриц, а не подготовка (подразделение, сотрудник, руководитель, приглашение). `sleep` нет; мутация N+1 валит обе строки теории, контроль без мутации зелёный.
- Арифметика набора: 678 + 2 новых `[Fact]` = 680; HEAD прогона `be95745` — коммит тестов, после него менялись только артефакты спринта и память.
- Сегмент `test-plan.entry-03.md` — новая ротация (строки записи 3 и строка TST-1), прежние сегменты не тронуты; живой файл сохраняет `Entry log`, `Suite run`, `Defects`, `Manual verification`. Удалённых тестов в записи 4 нет, `Defects` без новых строк.
- Ручная проверка: таблица `Manual verification` пуста с обоснованием «автоматизируется всё проверяемое»; результатов для отчёта нет, необходимость не возникает.
- Свод правил проекта: комментариев внутри тел методов и ссылок на документы в новых строках тестов нет (§7, §8); имена по соглашению `Ac<N>_…`; блокировок многострочных инвариантов правка не добавляет (чтение и порядок статусов в памяти), гонки не требуется.

## Coverage

Манифест `351a75426c9420377145457db1744d3a96693b1fc43f3cd12671cc4992ef1f17` (`testing.manifest.json`), правки без `n/a`.

```json
{"manifest_digest":"351a75426c9420377145457db1744d3a96693b1fc43f3cd12671cc4992ef1f17","findings":[],"files":[{"i":"tests/Competency.Tests/DeskQueryCountTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixAccessTests.cs","s":"checked"},{"i":"tests/Competency.Tests/PublicationCascadeTests.cs","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-01.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-02.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-03.md","s":"checked"}],"rules":[{"i":"Rule-set conformance","s":"pass"},{"i":"Coverage","s":"pass"},{"i":"Edge cases","s":"pass"},{"i":"Manual verification (last resort)","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[]}
```

## Verdict

APPROVE

## Next action

Тестирование закрыто на этой итерации; reviewer защёлкивается как APPROVE. Остальные ревьюеры волны решают свои вердикты; после выполнения состава волны — терминальный полный прогон набора.

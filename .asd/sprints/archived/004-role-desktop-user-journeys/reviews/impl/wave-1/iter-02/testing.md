[REVIEW-impl-testing]: CONCERNS

# Review — testing

- **Phase**: impl-review
- **Iteration**: wave-1/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| TST-1 | medium | tests/Competency.Tests/DeskQueryCountTests.cs:27 | Окно измерения `large` открывается снимком `fixture.Host.Logs.Length` (строка 37) сразу после `AddUnitsAsync(admin, 12)` (строка 27); ничто не дожидается хвоста журнала последнего запроса подготовки. Этот запрос — приглашение (`POST /users`): после коммита он отправляет письмо, затем пишет журнал аудита (`AccountMail.cs:48`) и читает роли (`UserEndpoints.cs:196-197`), то есть его последние команды EF попадают в консоль хоста непосредственно перед ответом. Консольный логгер асинхронный (об этом сказано в `ApiHost.LogsAfterAsync`), а набор идёт десятками параллельных хостов (на нём уже ловили `53300`), поэтому такие строки могут дойти до теста после снимка и войти в окно: `large` = 18 + k, и `large.Should().Be(small)` (строка 31) падает без дефекта кода. Перед `small` тот же риск закрыт прогревочным вызовом (строка 24: он ждёт строку `… - 200` своего запроса, а журнал идёт строго по порядку), перед `large` прогрева нет. flaky-pattern: снимок длины асинхронного журнала без ожидания предыдущего запроса. Зелёные прогоны записи 3 это не опровергают: окно гонки — миллисекунды, а мутации сдвигают счёт на десятки команд. Строка плана: `test-plan.entry-02.md` «Число SQL-команд `GET /desk` и `GET /desk/count`» (TST-6), AC-4. | Перед `var large` (строка 28) добавить отбрасываемый `await CommandsAsync(admin, route);` — как перед `small` (строка 24); либо вынести ожидание в `CommandsAsync`: перед снимком отправлять холостой запрос с собственной пробой и ждать его строку `… - 200`. Пометку `flaky-pattern` в код не вносить (комментарии в теле метода запрещены, `code-style.md` §7): причину записать в строке `DeskQueryCountTests` таблицы «Added tests» живого `test-plan.md`. |

## Coverage

Что сверено (без таблиц; полный учёт — в сжатом реестре ниже):

- Строки записи 3 сверены с перечнем исправлений в `decisions-log.md` («impl fix for wave-1/iter-01»): external 1–5, efficiency 1–4, documentation 1–3. Каждое изменение поведения имеет проверку на реальном хосте API и PostgreSQL либо обоснованное `none`; пять тестов external записаны красными до правки, шесть стражей эффективности честно помечены зелёными до правки и закреплены мутациями. `678 = 667 + 11` сходится: все одиннадцать добавленных тестов — `[Fact]`, не теории.
- `none` по части external 3 про направление проверено по `MatrixEndpoints.ReferenceProblemAsync` и `MatrixCatalog.IsOfDirection`: матрица создаётся только на действующей области, компетенция версии — той же области или без неё, поэтому идентификатор области в `RouteEntryIdsAsync` ничьего права не меняет; редкий случай перевода черновика на чужую черновую область останавливает проверка полноты (`ForeignCompetency`). Решение честное.
- Сегмент `test-plan.entry-02.md`: удаления подтверждены (классы и методы из «Removed tests» в `tests/` отсутствуют, причины — упразднённые категории, тип компетенции в справочнике, колонка `users.role`, перенос данных старых схем до 1.0; все внутри области изменений); замены существуют (`BaseScaleSeedTests`, два случая в `Ac14_Document_IsRefusedWhenItIsNotWellFormed`, `HrRoleTests.Ac62_…`). У каждой строки исправления дефекта (TST-1, 2, 4, 5, миграция ролей, окружение) записаны команда, код выхода и имя упавшего теста либо сообщение runner.
- Недетерминизм: у каждого нового класса своя база (`HostFixture`), тесты класса идут по очереди, поэтому утверждения о «нет ни одной HR-записи», «шкала не тронута» и «суммы видов сходятся» безопасны; гонка `Ac62_…` ждёт блокировку по `pg_stat_activity` без сна; `max_connections=500` подтверждён пиком 108 из 500 при прежнем пределе 100. Единственный дефект — TST-1.
- Блокировки нескольких строк (`custom-coding-rules.md`): новое правило этой дельты — только `Ac62_…` (снятие роли HR и закрепление) — имеет детерминированную гонку в обоих порядках; прочие правки дельты новых инвариантов не вводят.
- Ручная проверка: в «Manual verification» строка «ручной проверки не требуется» — обосновано (всё проверяемое автоматизировано, e2e по правилу проекта нет); результатов ручной проверки к отчёту нет.

```json
{"manifest_digest":"9112f686f8d374e125b2c37db3cf462d992b8e25caeba2ff6d9b525c175c4853","findings":["TST-1"],"files":[{"i":"tests/Competency.Tests/BaseScaleSeedTests.cs","s":"checked"},{"i":"tests/Competency.Tests/CareerLockTests.cs","s":"checked"},{"i":"tests/Competency.Tests/CatalogDraftTests.cs","s":"checked"},{"i":"tests/Competency.Tests/CompetencyBlockTests.cs","s":"checked"},{"i":"tests/Competency.Tests/CompetencyTests.cs","s":"checked"},{"i":"tests/Competency.Tests/DeskCatalogTests.cs","s":"checked"},{"i":"tests/Competency.Tests/DeskPrerequisiteTests.cs","s":"checked"},{"i":"tests/Competency.Tests/DeskQueryCountTests.cs","s":"checked"},{"i":"tests/Competency.Tests/Infrastructure/TestEnvironment.cs","s":"checked"},{"i":"tests/Competency.Tests/LevelPositionTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixAccessTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixCreationTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixWorkflowTests.cs","s":"checked"},{"i":"tests/Competency.Tests/PublicationCascadeTests.cs","s":"checked"},{"i":"tests/Competency.Tests/ScenarioReadTests.cs","s":"checked"},{"i":"tests/Competency.Tests/UnitSetupTests.cs","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-01.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-02.md","s":"checked"}],"rules":[{"i":"Rule-set conformance","s":"finding","f":"TST-1"},{"i":"Coverage","s":"pass"},{"i":"Edge cases","s":"pass"},{"i":"Manual verification (last resort)","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[]}
```

## Verdict
CONCERNS: 1

## Next action
Оркестратор направляет итерацию в review-fix (`asd-tester`): исправить TST-1 в `tests/Competency.Tests/DeskQueryCountTests.cs` и записать причину в строке «Added tests» живого `test-plan.md` (находка среднего уровня в тестовом файле — не «низкая только тестовая», поэтому не test-fix на месте). Остальные строки записи 3 и сегменты менять не нужно.

Замечание о процессе: поиск `TST-9|TST-10` по каталогу спринта вывел две строки из `reviews/impl/wave-1/iter-01/testing.md`; они не использованы как вход, выводы получены из текущего диффа, `test-plan.md` с сегментами, `decisions-log.md` и файлов тестов.

## Escalations
Эскалаций нет.

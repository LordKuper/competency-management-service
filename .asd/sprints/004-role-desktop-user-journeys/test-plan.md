---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 004-role-desktop-user-journeys

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 16478d5 | full change surface |
| 2 | 7fe0c10 | delta since entry 1 |
| 3 | f72d32e | delta since entry 2 |
| 4 | d8c397f | delta since entry 3 |
| 5 | 7b8de3c | delta since entry 4 |
| 6 | ce9240f | delta since entry 5 |
| 7 | 5382584 | delta since entry 6 |

## Risk → check decisions

Дельта записи 7 — `git diff ce9240f...HEAD`: правки dev по замечаниям ревью волны 2, итерации 02 (`2414036`, `a7aaac5`, `50f3cde`, `f4cc316`, `c5a16e7`; шесть файлов `web/`), документы спринта и файлы ревью. Файлов backend, `openapi/` и конфигурации сборки в дельте нет; dev тестов не менял, строк тестировщика ревью-фикса нет. Safety valve применён: правка затрагивает общие формы (`UserForm`, `EmployeeForm`, `PositionSelect`), поэтому набор web запущен целиком; набор backend не запускался: код и тесты backend не менялись с записи 6. Поиск удалённых имён (`setSaved`, `saved !== undefined`, `onOpenChange` в `TrackPositionsPage`) по репозиторию и `.claude/agent-memory`: устаревших утверждений нет; строка памяти тестировщика об `onOpenChange` описывает приём и остаётся верной.

Замечания ревью — дефекты уже написанного кода, поэтому каждое исправленное поведение получает регрессионный тест (`code-style.md` §17), кроме строк `none` и `keep` ниже. Уровень тот же, что в записи 6: компонентный тест с сетью `fakeApi`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `OnboardEmployeePage`: фоновое обновление (возврат фокуса, восстановление сети) не подставляет перечитанную карточку; она берётся только после отказа 412 (`2414036`) | change: workflow state machine — при возврате в шаг «Сотрудник» созданного сотрудника более новая карточка пересоздавала форму, и набранные правки молча терялись; «Сохранить и далее» не видел изменений и шёл дальше | component | add | `OnboardEmployeePage.test.tsx`: набрана правка фамилии, карточка за спиной стала новее, затем возврат фокуса и восстановление сети (две строки `it.each`) → поле хранит набранное, «Сохранить и далее» шлёт `PUT` с набранной фамилией и `If-Match: "1"`. Синхронизация: перечень должностей подразделения перечитан по тому же сигналу (положительный контроль, что сигнал дошёл до кэша), затем одна допечатанная буква: проверка сразу после сигнала проходила бы вхолостую. Ceiling: барьеры кода — `enabled` запроса и условие `isRefused` при подстановке — каждый по отдельности достаточен, мутант с одним убранным равнозначен и зелёный (`O1`, `O7`); красным делает только удаление обоих (`O8`). Тест закрепляет поведение, не выбор барьера |
| `OnboardEmployeePage`: после подстановки перечитанной карточки признак отказа сбрасывается, и следующее фоновое обновление снова её не подставляет (`2414036`) | change: workflow state machine — оставшийся включённым признак возвращал потерю правок после первого 412 | component | add | `OnboardEmployeePage.test.tsx`: отказ 412, перечитанная карточка на экране, дописана буква, карточка за спиной стала новее, возврат фокуса → набранное сохраняется. Ceiling: сброс в `onSaved` (`O5`) и в `onAnother` (`O6`) достижим только после отказа 412 с неудавшимся перечитыванием и успешного повторного сохранения; мутанты выжили, записано как оборона, не как пробел |
| `OnboardEmployeePage`: `isStaleVersion` — перечитывание включает только отказ по версии (`2414036`) | change: workflow state machine — сбой сохранения не 412 при уже изменившейся карточке заменял набранное перечитанной | component | add | `OnboardEmployeePage.test.tsx`: `PUT` отвечает 500 и меняет карточку за спиной → поле хранит набранное, виден алерт «Карточка сотрудника не обновлена». Ветку `isStaleVersion` держит мутант `O3` (перечитывание при любой ошибке): все прежние тесты отказа используют 412 |
| `OnboardEmployeePage`: перечитывание по 412 и следующее сохранение с новой версией (`2414036`: запрос включается признаком отказа, раньше был включён всегда); автоповтора нет | change: workflow state machine | component | keep | прежние `rereads the employee after a refused save…` и `sends the version it reread…` (запись 6) зелёные; мутант `O2` (запрос выключен всегда) краснит оба. Автоповтор в коде отсутствует, дельта его не касалась |
| `UserForm`: `<Form disabled={isSubmitting}>` — e-mail, роли и выбор сотрудника заперты на время отправки (`a7aaac5`) | change: workflow state machine — правка после отправки теряется, приглашение уходит на прежний e-mail | component | add | `UserModal.test.tsx`: создание (`POST /users`) и изменение (`PUT`) удержаны отложенным ответом → три поля выключены; после отказа сервера включены. Потребитель `CreateAccountForm` (поле e-mail шага «Учётная запись») закрыт утверждением в `OnboardEmployeePage.test.tsx` (`holds the steps… and the e-mail…`): мутант `U3` (запирание не для формы с `lockedTo`) краснит только его. Две строки `it.each` различает `U2` (запирание только у новой записи) |
| `EmployeeForm`: `<Form disabled={isSubmitting}>`; `PositionSelect`: `disabled={quickAdd.isPending \|\| undefined}` — явное `false` перекрывало запирание формы (`50f3cde`) | change: workflow state machine — фамилия, имя, отчество, подразделение и должность правились во время сохранения; итог шёл дальше со старыми значениями | component | add | `EmployeeForm.test.tsx`: сохранение удержано отложенным ответом → пять полей выключены (подразделение и должность — списки), после ответа включены; мутант `P1` (прежнее `disabled={quickAdd.isPending}`) оставляет включённой только должность. `PositionSelect.test.tsx`: запирание на время добавления должности — страж (поведение прежнее, строка переписана); мутант `P2` (`disabled={undefined}`) на прежнем файле теста выжил, поэтому тест добавлен. Ceiling: потребитель `ChangePositionForm` (запирание по `useIsFindingBusy`) получает ту же правку `PositionSelect`; отдельного теста нет — корень один и закреплён мутантом `P1`, а запирание самой формы (`disabled={isBusy}`) пришло из прежних записей |
| `BuilderCompetenciesStep`: флажки строк (и «выбрать все») выключены на время сохранения содержимого (`f4cc316`) | change: workflow state machine — отметка компетенции после начала `PUT` отсутствовала в сохранённом запросе, экран показывал отметку, которой на сервере нет | component | add | `BuilderCompetenciesStep.test.tsx`: `PUT /content` удержан → выключены «выбрать все» и обе строки, отметить вторую компетенцию нельзя; после отказа сервера включены, отметка ставится. Проверки тела `PUT` нет: оно собирается при вызове мутации, до любой отметки, и не отличается до правки и после. Попытка отметки идёт с `pointerEventsCheck: Never`: у выключенного флажка `pointer-events: none`, обычный щелчок падает исключением до проверки состояния |
| `TrackPositionsPage`: убран `onOpenChange`, сбрасывавший поиск при закрытии списка (`c5a16e7`) | нет поведения: одиночный `Select` сам вызывает `onSearch("")` при закрытии | component | keep | видимое поведение закреплено прежним тестом «после закрытия списка предлагается весь перечень» (зелёный на HEAD); мутанты `T2` / `T2b` записи 6 сняты вместе с кодом |
| Комментарий к `OnboardEmployeePage`, документы спринта, файлы ревью | нет поведения | — | none | тексты не исполняются; переименование состояния `saved` → `outcome` и новый пропс `onStaleVersion` проверены сборкой и прогоном набора |

## Removed tests

Записью 7 не удалён ни один тест; строк на удаление от ревью-фикса нет.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Fail-first. `node node_modules/vitest/vitest.mjs run <5 файлов> --reporter=verbose` в копии `git archive 10e14f7 web` (родитель первого исправления `2414036`; `node_modules` — junction) с тестами рабочего дерева → exit 1, 9 красных из 27 тестов этих файлов: 8 новых и 1 дополненный прежний (`holds the steps, «Назад», «Пропустить» and the e-mail…`). Зелёный до правки один новый тест — страж (`PositionSelect.test.tsx`: запирание на время добавления должности существовало и раньше). Vitest типы не проверяет, поэтому тесты идут против старого кода и падают на утверждениях.

Мутации — в копии `git archive HEAD web` (`node_modules` — junction) с тестами рабочего дерева; один якорь на правку (число попаданий проверяется), файл восстанавливается в `finally`; рабочее дерево не мутировалось. Команда: `node node_modules/vitest/vitest.mjs run <файлы теста> --reporter=json`; контрольный прогон пяти файлов без мутации — 27 из 27, exit 0. «Убит» = exit 1 и runner назвал упавший тест; в ячейке — первая упавшая проверка из отчёта runner, `runs` — число мутационных прогонов, нацеленных на файл.

| Test | Regression proof |
|---|---|
| `web/src/features/desk/OnboardEmployeePage.test.tsx` | до правки (exit 1, пять красных): обе строки `keeps what was typed on the card of the created employee when $cause…` (возврат во вкладку, восстановление сети) → `Error: expect(element).toHaveValue(Ивановова)`, получено `Сидорова` (набранное заменено перечитанной карточкой); `goes back to leaving the card alone once the reread after a refused save is on screen` → `toHaveValue(Сидоровав)`, получено `Петровв`; `keeps what was typed when the save fails for another reason than a stale card…` → `toHaveValue(Ивановова)`, получено `Сидорова`; дополненный `holds the steps, «Назад», «Пропустить» and the e-mail while the account is being created` → `Error: expect(element).toBeDisabled()`, `Received element is not disabled`. Мутации (каждая exit 1, кроме выживших): `O8` (запрос включён всегда и подстановка без `isRefused`, то есть код до правки) → четыре красных, первый — `toHaveValue(Ивановова)`; `O3` (`onStaleVersion()` при любой ошибке) → `keeps what was typed when the save fails for another reason…`: `toHaveValue(Ивановова)`; `O4` (после подстановки признак отказа не сбрасывается) → `goes back to leaving the card alone…`: `toHaveValue(Сидоровав)`; `O2` (запрос выключен всегда) → три красных, первый — прежний `rereads the employee after a refused save…`: `toHaveValue(Сидоров)`; `U1` (`UserForm` не запирается), `U3` (запирание не для формы с `lockedTo`) → `holds the steps… and the e-mail…`: `toBeDisabled()`. Выжили: `O1` (запрос включён всегда), `O1b` / `O1c` (то же с отключённым обновлением по фокусу / по сети), `O7` (подстановка без `isRefused`) — равнозначны, каждый из двух барьеров достаточен один; `O5`, `O6` (сброс признака в `onSaved`, `onAnother`) — достижимы только после отказа 412 с неудавшимся перечитыванием, записано как оборона; runs: 12 |
| `web/src/features/users/UserModal.test.tsx` | до правки: обе строки `holds the e-mail, the roles and the employee while the form $what the account…` (создаёт, изменяет) → `AssertionError: expected [] to deeply equal [ 'e-mail', 'roles', 'employee' ]`. Мутации (каждая exit 1): `U1` (форма не запирается) → обе строки; `U2` (запирание только у новой записи) → строка изменения; runs: 2 |
| `web/src/features/org-structure/EmployeeForm.test.tsx` | до правки: `holds every field, the unit and the position choices included…` → `AssertionError: expected [] to deeply equal [ 'surname', 'name', …(3) ]`. Мутации (каждая exit 1): `E1` (форма не запирается) → та же строка; `P1` (прежнее `disabled={quickAdd.isPending}`) → `AssertionError: expected [ 'surname', 'name', …(2) ] to deeply equal [ 'surname', 'name', …(3) ]` (включённой остаётся должность); runs: 2 |
| `web/src/features/org-structure/PositionSelect.test.tsx` | страж, зелёный до правки: `holds the choice while the typed name is being added, and frees it once the position is added`. Мутации: `P2` (`disabled={undefined}`, запирание на время добавления убрано) → exit 1, `Error: expect(element).toBeDisabled()`, `Received element is not disabled`; тот же `P2` на прежнем файле теста без этой строки → exit 0 (11 из 11), мутант выжил, поэтому тест добавлен; `P1` этот файл не краснит (краснит `EmployeeForm.test.tsx`); runs: 3 |
| `web/src/features/desk/BuilderCompetenciesStep.test.tsx` | до правки: `turns every checkbox off while the content is saved, so no competency is ticked then, and turns them on when the save is refused` → `AssertionError: expected [] to deeply equal [ 'select all', 'row 1', 'row 2' ]`. Мутация `B1` (`isSaving` убран из `disabled` строки) → exit 1, та же строка; runs: 1 |

## Suite run

Command (запись 7): `npm --prefix web test` — весь набор web. Safety valve применён: правка затрагивает общие формы (`UserForm`, `EmployeeForm`, `PositionSelect`), поэтому impacted-набор вырождается в весь набор web; backend не запускался (код и тесты backend не менялись с записи 6).
Scope: full web (impacted-набор вырождается в полный).
Result: pass — до стратегии на дереве `fb5237c` web 676 тестов в 65 файлах: 676 успешно, 0 упало, exit 0, 33 с; после авторства на дереве `427082c` web 685 тестов в 65 файлах: 685 успешно, 0 упало, exit 0, 37 с. Это 676 прежних и 9 добавленных записью (4 в `OnboardEmployeePage`, 2 в `UserModal`, по одному в `EmployeeForm`, `PositionSelect`, `BuilderCompetenciesStep`); один прежний тест дополнен утверждением (`OnboardEmployeePage`, e-mail). Удалённых тестов нет. Три подряд прогона пяти затронутых файлов — по 27 из 27 (нестабильности нет). Новых кодовых дефектов нет.
Lint / build: pass — `npm --prefix web run lint` (biome ci + tsc) exit 0; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0 без расхождений; backend не менялся (запись 6: `dotnet build Competency.slnx --tl:off` — предупреждений 0, ошибок 0).
HEAD: 427082c (прогон измеряет дерево этого коммита; после него менялись только `test-plan.md`, `test-plan.entry-06.md` и память тестировщика)

Замер стола на сиде `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, 1 000 подразделений, 348 учётных записей, 625 версий матриц; сборка `Debug`, один процесс, 60 замеров после 5 прогревочных; число SQL-команд — команды приложения, без `DISCARD ALL` сброса соединения Npgsql):

| Роль | `/desk/count` p50 / p95 мс | `/desk` p50 / p95 мс | SQL-команд | Элементов действий | Крупнейшая группа |
|---|---|---|---|---|---|
| администратор | 75 / 91 | 57 / 64 | 22 | 813 | 1077 (UnusedCatalogEntries) |
| HR с закреплением на корне | 53 / 61 | 53 / 63 | 22 | 275 | 1077 (UnusedCatalogEntries) |
| руководитель верхнего подразделения | 33 / 42 | 34 / 41 | 19 | 1 | 6 |
| сотрудник | 19 / 27 | 20 / 25 | 11 | 0 | 1 |

Бюджеты соблюдены (≤ 300 мс, ≤ 600 мс, ≤ 25 SQL-команд, наибольшая группа 1077 < 2000). Сырой счёт строк журнала PostgreSQL вдвое больше (44 / 44 / 38 / 22) из-за `DISCARD ALL` после каждой команды; для администратора три раза читаются учётные записи и сотрудники и два раза — справочники (запас до 25 небольшой).

Замер сделан в записи 3 и не повторялся: код стола в дельтах записей 4–7 не менялся (запись 5 меняет `PublicationPlan`, ответы предпросмотров публикации и web, записи 6 и 7 — только web и его тесты), а `CatalogStore.ViewerAsync` из записи 4 стол не вызывает.

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.CareerFramework/CareerDirectionEndpoints.cs | Expected refused.Json!["detail"]!.GetValue<string>() to be the same string, but they differ at index 0: | Competency.Tests.CatalogDeletionTests.Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices | fixed | 2725176 |
| D-2 | 1 | web/src/features/career/usePublishEntry.tsx | AssertionError: expected null not to be null | approving an entry under the list of what is published with it (AC-110…AC-112) > explains a refusal the server gives at the moment of approval, with the way to the track's positions | fixed | 4483101 |

Пояснения (вне строк таблицы): D-1 — константа `InUseMessage` («Направление используют треки, компетенции или матрицы — удалить нельзя») не обновлена под AC-37 (сущность называется «область»), `CareerMail` и остальной текст уже используют «область». D-2 — `RefusalContent` вставляет `<Link>` в содержимое `modal.error`, а модальное окно из `App.useApp()` в `Providers` отрисовывается вне `RouterProvider` (структура приложения такая же, как в тесте), поэтому при отказе «нет действующей должности» с известным треком `Link` падает на `useContext(...) is null` и экран остаётся пустым; проверено: замена `<Link>` на `<a>` в копии делает весь файл зелёным (13 из 13).

## Manual verification

| AC | Steps | Expected observation |
|---|---|---|
| — | автоматизируется всё проверяемое; ручной проверки не требуется | — |

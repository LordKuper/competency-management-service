---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/quality.md (verdict)
---

# Test plan — sprint 006-promotion-waves

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | e16a232e2d9b7d86c6e2672a3a23a2b03c2ce926 | full change surface (Tasks 1–27, AC-1…AC-93; AC-9, AC-27, AC-39 сняты) |

## Risk → check decisions

Область изменений затрагивает общие тестовые помощники, меню, рабочий стол и контракт API, поэтому предохранитель общей инфраструктуры (`sprint-lifecycle.md` «Impacted test set») принуждает полный прогон: оба набора (бэкенд и веб) запущены целиком.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Тип компетенции «Профильная» заменён на «Дополнительная» (`"Profile"` → `"Additional"`) | устаревшие тесты падают на контракте (136 красных на входе) | component/contract | keep (обновлены) | значение — часть контракта; правка данных тестов, поведение не менялось |
| Меню: группа «Справочники» снята, пункты «Карьерные переходы», «Управление карьерными переходами», «Карьерная модель», «Оргструктура» | пользователь не видит или видит чужой раздел | component | keep (обновлён `AppShell.test.tsx`) | меню — наблюдаемое поведение, обязано совпадать с AC |
| Новые маршруты Promotion (41 шт.) | маршрут без охраны или с чужой ролью (AC-46) | contract (`AuthorizationTests`) | keep (расширены списки) | списки выводятся из таблиц маршрутов; роль каждого маршрута проверена: администратор / администратор+HR / любой вошедший (права в обработчике: 404/428 для обычного пользователя) |
| Новые маршруты с `If-Match` (15 шт.) | потерянное обновление | contract (`OptimisticConcurrencyTests`) | keep (расширен список) | общий механизм, один ряд на маршрут |
| Рабочий стол: комиссия без председателя (AC-86), карточка «Объявить карьерную сессию» (AC-89) | элемент не появляется / не исчезает; чужие роли его видят | integration (API на реальной БД) + component | add | `PromotionWaveTests`, `DeskScenarios.test.tsx`; хелпер `DeskReads.ItemOf` теперь игнорирует элементы-комиссии, у которых тот же id, что у подразделения |
| Бюджет запросов рабочего стола (`docs/architecture/desk.md`: не больше 25 SQL-команд независимо от объёма данных) | источник Promotion довёл запрос до 26 команд | integration | keep, бюджет 25 не менялся | красный тест — код-дефект D-1; ослаблять бюджет в тесте нельзя |
| Даты сессии, календарь этапов, шаги планировщика (AC-52, 55, 56, 57) | неверные границы этапов, день по Москве против UTC, пропущенные переходы | unit (чистая логика) | add | `PromotionWaveScheduleTests`: `WaveDates`, `PromotionCalendar`, `DueStep` без часов и БД |
| Расчёт: взвешенный рейтинг, средняя доля, критичные, «Не наблюдал» (AC-21, 29, 49, 93); правила баллов (AC-19); круговая оценка (AC-20); формальная проверка (AC-15, 16, 17, 81); откат на этап (AC-53) | ошибка формулы даёт неверное решение о карьере | unit | add | `PromotionScoringTests`, чистые функции, значения проверены вручную |
| Заявка: следующий уровень (AC-47, 77), ограничения подачи (AC-8, 58, 59, 60, 82), образование (AC-10, 78), видимость (AC-37), предварительный рейтинг (AC-76) | чужая заявка, неверный набор оценивающих, редактирование после подачи | integration | add | `PromotionApplicationTests` на общей сцене (волна переведена на этап 1 планировщиком) |
| Жизненный цикл сессии: этапы 2–6 (AC-17, 19, 20, 21, 29–31, 62, 68, 69, 80) | утечка авторов оценок и рейтинга; неверный статус на этапе; закрытие правок по этапу | integration (один длинный сценарий, этапы сдвигаются датами в БД, планировщик отрабатывает сам) | add | `PromotionCycleTests`; один сценарий вместо дублей, потому что каждая смена этапа ждёт тик планировщика |
| Объявление сессии, настройки (AC-1, 2, 7, 56, 63, 66, 71, 87) | подразделение без председателя, пересечение сессий, чужая зона ответственности, письмо без ссылки | integration | add | `PromotionWaveTests` |
| Письма HTML + текст (AC-87) | ссылка не кликабельна, разметка ломается от спецсимволов | unit + integration | add | `MailHtmlTests` |
| Пароль с кириллицей (AC-88) | валидатор отвергает допустимый пароль | unit + integration | add | `UnicodePasswordTests` (валидатор через `UserManager` с рукописным хранилищем: NSubstitute не умеет проксировать internal `AppUser`) |
| Руководитель выше ведёт матрицы подразделений без руководителя (AC-90) | неверный состав `matrixUnits`, пропуск шага «руководитель», чужие создают матрицы | integration | add | `MatrixHeadlessUnitTests` |
| Наследование матриц вниз по дереву (AC-91) | сотрудник получает не ту матрицу; своя опубликованная не заменяет унаследованную | unit (`MatrixInheritance`) + integration | add | `MatrixInheritanceTests` |
| Веса компетенций и порог при критичных (AC-93) | сумма весов ≠ 100 проходит; порог при всех критичных сохраняется; бывший порог не снимается | integration | add | `MatrixWorkflowTests.Ac93_…`: отказ при передаче и при сохранении |
| Шаг «Формула и пороги»: поровну 34/33/33, свои веса, порог недоступен при критичных (AC-93) | неверное распределение остатка; кнопка не пересчитывает; порог остаётся | unit + component | add | `matrixContent.test.ts` (чистые функции), `MatrixPage.test.tsx` (три сценария редактора) |
| Подсказки мастера по шагам (AC-92) | подсказка чужого шага | component | add | `ScreenTour.test.tsx` — три теста на выбор подсказок по `data-tour-step`; сами тексты пояснений не закреплены (формулировка — не контракт) |
| Модули `stageDates`, `educationRule`, `raterEmail` | границы выходных, порядок образования, регистр адресов | unit | add | чистые функции, ноль зависимостей |
| Экраны Promotion в вебе (формы, таблицы) | регресс отрисовки | component | none | поведение экранов определяется сервером и проверено API-тестами; снимок разметки был бы реализационно связанным тестом; визуальная проверка — на ревью |
| Тексты пояснений AC-92 (содержание строк) | — | — | none | пины на прозу запрещены `code-style.md` §17 |

## Removed tests

Ни один тест не удалён. Тестов вне области изменений, предлагаемых к удалению, нет.

| Test | Reason | In change scope |
|---|---|---|
| — | — | — |

## Added tests

Исправленных дефектов в этом входе нет: код не менялся, тесты покрывают уже реализованное поведение, fail-first не применим.

| Test | Regression proof |
|---|---|
| tests/Competency.Tests/PromotionWaveScheduleTests.cs (13) | n/a — новое покрытие готового кода, не фикс дефекта |
| tests/Competency.Tests/PromotionScoringTests.cs | n/a |
| tests/Competency.Tests/PromotionApplicationTests.cs (9) | n/a |
| tests/Competency.Tests/PromotionCycleTests.cs (1, около 5 мин: четыре смены этапа ждут планировщик) | n/a |
| tests/Competency.Tests/PromotionWaveTests.cs (8) | n/a |
| tests/Competency.Tests/MailHtmlTests.cs | n/a |
| tests/Competency.Tests/UnicodePasswordTests.cs | n/a |
| tests/Competency.Tests/MatrixHeadlessUnitTests.cs (4) | n/a |
| tests/Competency.Tests/MatrixInheritanceTests.cs | n/a |
| tests/Competency.Tests/MatrixWorkflowTests.cs: Ac93_HandOver_… | n/a |
| tests/Competency.Tests/Infrastructure/PromotionScene.cs | вспомогательный код (сцена и хост), не тест |
| web/src/features/career/matrixContent.test.ts (+9) | n/a |
| web/src/features/career/MatrixPage.test.tsx (+3) | n/a |
| web/src/features/desk/DeskScenarios.test.tsx (+2) | n/a |
| web/src/app/ScreenTour.test.tsx (3) | n/a |
| web/src/features/promotion/stageDates.test.ts, educationRule.test.ts, raterEmail.test.ts (11) | n/a |

Обновлены (тестовый дефект, поведение продукта не менялось): `AuthorizationTests`, `OptimisticConcurrencyTests`, `DeskPagingTests`, `DeskQueryCountTests`, `MatrixCreationTests` (сообщение «Коэффициенты, веса и пороги»), `MatrixWorkflowTests.Ac15` (`requiredEducation`), `CompetencyBlockTests`, `MyPlaceTests`, `PositionMatrixTests`, `PublicationCascadeTests`, `ScenarioReadTests`, `Infrastructure/MatrixKit`, `DeskReads`, `MailCatcher` (HTML-часть письма), `Waiting` (необязательный тайм-аут), веб: `AppShell`, `MatrixCreateModal`, `MatrixPage`, `catalogueTables`, `columnFilterWiring`.

## Suite run

- Command: `dotnet <Competency.Tests.dll>` (бэкенд, собран в отдельный каталог артефактов) и `npm --prefix web test`; сборка и lint: `dotnet build Competency.slnx --tl:off`, `npm --prefix web run lint`, `npm --prefix web run build`
- Scope: full
- Result: fail — бэкенд 927 прогонов, из них DeskQueryCountTests красный при бюджете 25 (оба случая: 26 команд, D-1), остальные зелёные; веб 836 пройдено, 0 упало, 0 пропущено (79 файлов). До возврата бюджета в тесте стояло 30, и бэкенд был зелёный.
- Lint / build: pass — `dotnet build` 0 предупреждений и 0 ошибок; `biome ci` и `tsc` чисто; `vite build` успешно
- HEAD: 2fdcc24 — коммит с тестами (бюджет 30; возврат к 25 — в следующем коммите, прогон DeskQueryCountTests отдельно, без веб-набора); бэкенд запущен только после остановки веб-прогона

Заметки о запуске (не дефекты кода):
- Бэкенд-прогон одновременно с веб-прогоном дал 227 падений «The API host was not ready in 60 s»: хосты стартуют параллельно и не успевают при нагрузке процессора. Одиночный прогон зелёный. Бэкенд-набор следует запускать отдельно от веб-набора.
- В веб-наборе под нагрузкой единичные тесты (разные в разных прогонах: `linkScreens`, `DraftsQueuePage`, `authFlow`, `OrgImportPage` и др.) укладываются в 5 с на пределе; по отдельности проходят, повторный полный прогон зелёный. Среди них нет тестов этого входа.

## Defects

Прогон `DeskQueryCountTests` отдельно при бюджете 25: оба случая теории красные, наблюдаемое число команд малых данных — 26 для `/api/v1/desk` и для `/api/v1/desk/count` (сравнение «малые = большие» до него не доходит). SQL-журнал запроса `/api/v1/desk` (26 команд): после команд источника карьеры (№ 1–19) идут № 20 `promotion_commissions`, № 21 `promotion_commission_members`, № 22–25 `org_unit_hr`, `user_roles/users`, `employees`, `user_roles/users` и № 26 `promotion_wave_units/promotion_waves`. Источник Promotion: `CommissionFindingsAsync` (чтение составов комиссий `commissions.ReadAsync` — № 20–21, запрос волн подразделений — № 26, `career.ResponsibleUnitsAsync` — вероятно № 22–25). Достаточно убрать одну команду, например объединить чтение комиссий и волн подразделений в один запрос или вынести повторные проверки ролей.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.Promotion/PromotionDeskSource.cs | Expected small to be between 1 and 25, but found 26. | Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk") и (route: "/api/v1/desk/count") | pending |  |

## Manual verification (optional)

Не требуется: визуальная сторона пояснений AC-92 и экранов Promotion проверяется ревью по макетам; единственного пути без автоматической проверки нет.

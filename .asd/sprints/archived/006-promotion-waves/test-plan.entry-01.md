---
responsibility:
  owns: сегмент повествования записи 1 test-plan.md (решения риск→проверка, удаления, добавленные тесты)
  excludes: Entry log, Suite run, Defects, Manual verification (остаются в живом test-plan.md)
  delegates_to: test-plan.md
---

# Test plan — sprint 006-promotion-waves, запись 1

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

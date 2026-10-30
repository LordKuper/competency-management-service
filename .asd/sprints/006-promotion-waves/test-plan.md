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
| 2 | 0a2ae2834d7d84621b92648f16241b4d21ed30cb | delta since entry 1 (`3659c0ae`, фикс D-1: `CommissionReader.ReadAsync`) |
| 3 | 47b455e4e6716896e4f6652118fc13b1eaeb98aa | delta since entry 2 (`0a2ae283`): review-fix wave-1/iter-01 (F1–F10), Task 28 (AC-94, AC-95), правка веб-поиска подразделений (`useUnitTreeSearch`) |

## Risk → check decisions

Дельта затрагивает общий модуль `Competency.Platform` (`MailSender`, `WebAddress`, миграция, снимок модели), поэтому предохранитель общей инфраструктуры (`sprint-lifecycle.md` «Impacted test set») принуждает полный прогон: оба набора запущены целиком до и после записи. До записи: бэкенд 942 зелёных; веб 841 из 842, один тайм-аут 5 с в `auth/linkScreens.test.tsx` (файл вне дельты; отдельно 18 из 18 зелёных — шум нагрузки параллельных воркеров).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Правки review-fix F1–F10 (задержка заявок по подразделению, `CloseReviewAsync`, `Promotion:TickInterval`, `WebAddress.IsHttp`, токены `ScaleChoice` и `ApplicationView`) | поведение F1/F5/F6 и ветки этапов уже проверены; остаётся убедиться, что вынос `IsHttp` не сменил проверку ссылок | integration, component | keep (новых тестов нет) | строки тестировщика review-fix — в `test-plan.entry-02.md`: `PromotionHeldApplicationTests`, `PromotionWaveScheduleTests`, `PromotionArtifactTests` (ссылка `ftp://` отклонена), `MailHtmlTests` (`ftp://` не становится ссылкой), `PromotionWavesBlock.test.tsx`, `DemoPage.test.tsx`; полный прогон до записи зелёный. Замена литералов на токены в `ScaleChoice`/`ApplicationView` — оформление; тест на значение токена повторял бы тему antd |
| Письмо «у комиссии нет председателя» (AC-94): один раз за сессию и подразделение, закреплённому HR с наследованием, иначе действующим глобальным администраторам, со ссылкой на «Комиссии» | письмо уходит на каждый тик; уходит не тому HR (вышестоящему вместо закреплённого за подразделением) или никому; при отсутствии HR администраторы его не получают | integration (API на реальной БД, планировщик с тиком 1 с, почтовый перехватчик) | add | `PromotionHeldApplicationTests.Ac94_*` (2): HR подразделения получает одно письмо с названием, числом заявок и ссылкой, после дальнейших проходов планировщика оно остаётся одним, HR выше по дереву письма не получает; без HR у подразделения и выше письмо получает администратор, снятый HR — нет. Текст письма не пинится дальше названия, числа и ссылки |
| Письмо новым членам комиссии, когда заявки уже на рассмотрении (AC-95, AC-63); исключённый теряет рассмотрение сразу | новый член не видит заявок или не получает письма; уже сидевший член получает письмо повторно; исключённый сохраняет доступ | integration | add | `PromotionCommissionChangeTests.Ac95_Ac63_*`: до включения 404, после 200 и одно письмо, прежний председатель остаётся при одном письме, после исключения 404 |
| Смена председателя посреди рассмотрения (AC-95): решение и итоговые оценки прежнего председателя сохраняются, дальше вносит новый | решение или итоговые оценки теряются; прежний председатель продолжает решать | integration | add | `PromotionCommissionChangeTests.Ac95_TheChairReplaced…`: решение и рейтинг по итоговым оценкам видны новому председателю и равны прежним, прежний получает 403 на PUT, новый меняет решение |
| Права на изменение состава комиссии подразделения сессии (AC-95) | член комиссии, председатель, HR чужого подразделения или аккаунт без роли меняет состав | integration | add | `PromotionCommissionChangeTests.Ac95_TheCommissionOfAUnitInASession…`: член и председатель получают 403, HR соседнего подразделения и аккаунт без роли 404, состав не тронут, HR подразделения 200; матрица ролей `AuthorizationTests` берёт случайный id и различия «видит, но не вправе» не проверяет |
| Комиссия без председателя после правки состава; задержанные заявки доходят до рассмотрения после назначения (AC-95, AC-66) | снятие или исключение председателя оставляет комиссию без председателя; задержанные заявки не освобождаются | integration | none (новый тест не нужен) | снятие председателя отклоняется в `PromotionWaveTests.Ac66_ATheChairOfAUnitInAnUnfinishedSession_CannotBeRemoved` (тот же `ChairProblemAsync` на любом этапе); замена заблокированного председателя и передача задержанных заявок с письмом — `PromotionHeldApplicationTests.Ac66_AnApplicationOfAUnitWithoutAChair…`; гонка правки комиссии с председателем — `PromotionRaceTests` |
| Письмо членам, добавленным после конца сессии (`ProcessedStage < StageCount` в `MembersAddedAsync`) | письмо о заявках закрытой сессии | — | none | условие — одна строка запроса; у проверки «письма нет» нет детерминированной точки ожидания, а тест с ожиданием проходил бы вхолостую при запоздавшем письме (§17: гипотетический риск) |
| Поиск подразделений в `TreeSelect` без фильтра rc (`useUnitTreeSearch`, `OrgUnitSelect`, `UnitTreeFilter`) | поиск не находит подразделение или различает регистр; предки совпадения или поддерево совпавшего родителя пропадают; путь к совпадению не раскрыт; выбор, неактивное подразделение и `confirm()` фильтра сломаны | unit (хук) + component | add | `useUnitTreeSearch.test.tsx` (5): дерево и раскрытые ключи при поиске, без текста и после очистки; `OrgUnitSelect.test.tsx` (3): раскрытие пути к найденному, выбор найденного, неактивное не выбирается при `isActiveRequired`; выбор и `confirm()` фильтра уже закреплены `UnitTreeFilter.test.tsx`. Скорость и виртуальный список в jsdom не проверяются |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| tests/Competency.Tests/PromotionHeldApplicationTests.cs: Ac94_AUnitWithoutAChair_TellsItsHrOfTheHeldApplicationsOnceInTheSession_AndNoOtherHr, Ac94_AUnitWithoutAChairAndWithoutAnHrUpTheTree_TellsTheAdministrators | мутации (откат записью исходного файла), `dotnet Competency.Tests.dll -filter "/*/*/PromotionHeldApplicationTests/*" -filter "/*/*/PromotionCommissionChangeTests/*"` из `tests/Competency.Tests/bin/Debug/net10.0`: (а) `StageActions.MarkNoChairLettersAsync` без `!member.NoChairLetterSent` → exit 1, красны `…Ac94_AUnitWithoutAChair_TellsItsHrOfTheHeldApplicationsOnceInTheSession_AndNoOtherHr` и `…Ac94_AUnitWithoutAChairAndWithoutAnHrUpTheTree_TellsTheAdministrators` (4 из 7, заодно оба Ac66 класса: шаг не умолкает); (б) `PromotionMail.NoChairAsync` без запасных получателей (`: administrators` → `: []`) → exit 1, `…Ac94_AUnitWithoutAChairAndWithoutAnHrUpTheTree_TellsTheAdministrators` (1 из 7); обе откачены; runs: 2 |
| tests/Competency.Tests/PromotionCommissionChangeTests.cs (3) | мутации тем же фильтром: (а) `PromotionMail.MembersAddedAsync` всегда выходит → exit 1, `…Ac95_Ac63_AMemberAddedMidReview_SeesTheApplicationsAndIsToldOfThem_AndOneTakenOffSeesThemNoMore`; (б) `CommissionEndpoints.SaveAsync` не удаляет исключённых (`RemoveRange(… => false)`) → exit 1, тот же тест; (в) `CommissionRights.Read` пропускает правку (`return Rejection.Forbidden()` → `break`) → exit 1, `…Ac95_TheCommissionOfAUnitInASessionIsChangedByItsHrAndTheAdministratorAlone`; (г) `ReviewEndpoints.PutAsync` пускает любого, кому заявка видна (`!roles.HasFlag(Chair)` → `roles == None`) → exit 1, `…Ac95_TheChairReplacedMidReview_LeavesTheOldChairsScoresAndDecisionStanding_AndTheEntriesToTheNewChair`; каждый раз 1 красный из 7, откачены; runs: 4 |
| web/src/features/org-structure/useUnitTreeSearch.test.tsx (5), OrgUnitSelect.test.tsx (3) | мутации `useUnitTreeSearch.ts`, `vitest run` обоих файлов, откат записью исходного файла: (а) предки не сохраняются (`children.length > 0` → `< 0`) → exit 1, 5 красных, среди них `useUnitTreeSearch… keeps a match with the units above it…` и три в `OrgUnitSelect.test.tsx`; (б) `treeExpandedKeys: undefined` → exit 1, красен только хук-тест `…and opens that path…` (компонент и без нашего списка раскрывает путь сам, его вклад закрывает хук-тест); (в) без регистра (`node.title.includes(needle)`) → exit 1, 5 красных; (г) поддерево совпавшего родителя режется → exit 1, `useUnitTreeSearch… keeps everything below a unit whose title matches…`; runs: 4 |

## Suite run

- Command: `dotnet test --solution Competency.slnx` (бэкенд один) и затем `npm --prefix web test`; сборка и lint: `dotnet build Competency.slnx --tl:off`, `npm --prefix web run lint`
- Scope: full — предохранитель общей инфраструктуры сработал (дельта в `Competency.Platform`: `MailSender`, `WebAddress`, миграция, снимок модели); набор не сужался
- Result: pass — бэкенд 947 прогонов, 0 упало, 0 пропущено, exit 0; веб 83 файла и 850 тестов зелёных, exit 0. До записи: бэкенд 942 зелёных; веб 841 из 842, тайм-аут 5 с в `auth/linkScreens.test.tsx` вне дельты (отдельно 18 из 18)
- Lint / build: pass — `dotnet build` 0 предупреждений и 0 ошибок; `biome ci` и `tsc` чисто
- HEAD: 47b455e4e6716896e4f6652118fc13b1eaeb98aa — измеренное дерево, коммит тестов этой записи; последующие коммиты записи правят только `.asd/sprints/**`

## Defects

Прогон `DeskQueryCountTests` отдельно при бюджете 25: оба случая теории красные, наблюдаемое число команд малых данных — 26 для `/api/v1/desk` и для `/api/v1/desk/count` (сравнение «малые = большие» до него не доходит). SQL-журнал запроса `/api/v1/desk` (26 команд): после команд источника карьеры (№ 1–19) идут № 20 `promotion_commissions`, № 21 `promotion_commission_members`, № 22–25 `org_unit_hr`, `user_roles/users`, `employees`, `user_roles/users` и № 26 `promotion_wave_units/promotion_waves`. Источник Promotion: `CommissionFindingsAsync` (чтение составов комиссий `commissions.ReadAsync` — № 20–21, запрос волн подразделений — № 26, `career.ResponsibleUnitsAsync` — вероятно № 22–25). Достаточно убрать одну команду, например объединить чтение комиссий и волн подразделений в один запрос или вынести повторные проверки ролей.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.Promotion/PromotionDeskSource.cs | Expected small to be between 1 and 25, but found 26. | Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk") и (route: "/api/v1/desk/count") | fixed | 3659c0ae |

## Manual verification (optional)

Не требуется: визуальная сторона пояснений AC-92 и экранов Promotion проверяется ревью по макетам; единственного пути без автоматической проверки нет.

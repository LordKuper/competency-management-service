# Test plan — sprint 002-mail-invite-password-reset, entry 8 (rotated)

Повествовательные строки записи 8, ротация по `artifact-layout.md` "Test plan". Не редактируется.

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 8: дельта — doc-комментарий в `AuthEndpoints.cs` (поведение не менялось) и тестовый код; `RowLock.cs` — общий тестовый помощник, но не сборочная, CI-конфигурация и не общий модуль приложения: его потребители находятся поиском по `tests` (`LastAdministratorTests`, `AccountLinkRaceTests`, `SignInRaceTests`), перегрузка `HoldAsync(host, userId)` сохранила сигнатуру. Предохранитель не срабатывает. Набор: изменённые файлы тестов (`LastAdministratorTests`, `PasswordPolicyTests`, `AccountLinkTests`) плюс потребители `RowLock` (`AccountLinkRaceTests`, `SignInRaceTests`); web не тронут. Предстратегический прогон: backend 44 из 44 (`-c Release`, сборка `Debug` занята процессом пользователя). Строки review-fix записи 7 (`Ac7_…`, строки 36 и 54 прежнего файла) ротированы в `test-plan.entry-07.md` вместе с остальными; удалений для переноса нет.

## Risk → check decisions

Строки записей 1–7 — в `test-plan.entry-01.md` … `test-plan.entry-07.md` (строки review-fix wave-2/iter-01 — в записи 7). Запись 8 (delta с 352584d):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AuthEndpoints.cs` doc-комментарий; правки doc-комментариев `PasswordPolicyTests.cs`, `AccountLinkTests.cs`; `RowLock.HoldActiveAdministratorsAsync` и переписанный `Ac7_…` (AC-7) | поведение приложения не менялось; риск гонки уже закрыт детерминированным тестом с доказательством мутациями (запись 7, review-fix) | — | none | новых рисков нет, новые тесты не нужны (§17); помощник `RowLock` проверяется тестом, который его использует. Остальные потребители `RowLock` прошли в предстратегическом прогоне |

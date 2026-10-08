# Test plan — sprint 002-mail-invite-password-reset, entry 6 (rotated)

Повествовательные строки записи 6, ротация по `artifact-layout.md` "Test plan". Не редактируется.

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 6: дельта — `AuthEndpoints.cs` и `PasswordPolicyResponse.cs` (модуль `UserManagement`), `openapi/openapi.json` и `web/src/api/schema.d.ts` (производные артефакты контракта, добавочно), `passwordPolicy.ts`, `LinkPasswordPage.tsx`, `ChangePasswordModal.tsx` (каталог `features/auth`). Сборочной, CI- или общей инфраструктуры нет: `AuthEndpoints.cs` — файл одного модуля, а не общий модуль. Предохранитель не срабатывает. Набор по ссылкам: `ContractTests` (читает `openapi.json`), `linkScreens.test.tsx` (рендерит оба экрана с хуком), новый `PasswordPolicyTests`; `ChangePasswordModal` тестами не охвачен. Предстратегический прогон: backend 157 из 157, web 82 из 82 (запрос политики без обработчика получает 599, экраны остаются на запасной подсказке). Сборка `Debug` занята процессом пользователя, backend в `-c Release`.

## Risk → check decisions

Запись 6 (delta с cb8ee2a):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `GET /api/v1/auth/password-policy`, `PasswordPolicyResponse { minLength }` из `IdentityOptions.Password.RequiredLength` (AC-19) | число зашито, а не берётся из настройки развёртывания; переопределение `Identity__Password__RequiredLength` не доходит до экрана | интеграционный на реальном хосте (`PasswordPolicyTests`: хост с `Identity__Password__RequiredLength=17`; общий хост отдаёт 10) | add | ровно то, ради чего AC-19: значение следует конфигурации. Путь через реальный Kestrel и привязку опций; юнит на `IOptions` повторил бы реализацию (§17) |
| `.AllowAnonymous()` на новом методе (AC-19) | метод закрыт сессией, экраны ссылки (без сессии) получают 401 | те же интеграционные тесты: `Anonymous()` клиент без входа получает 200 | add | отдельный тест не нужен: оба случая выше идут анонимным клиентом |
| `openapi/openapi.json`: метод присутствует, без 401/429, схема с `minLength` (AC-19) | метод пропал из контракта или получил 401/429 (клиент уводит анонимный экран на вход) | контрактный, по образцу `ContractTests.Ac5_AnonymousLinkMethods_…` | add | `check:api` ловит лишь расхождение файла с кодом, не свойства метода |
| `usePasswordHint()` в `LinkPasswordPage` и `ChangePasswordModal` (AC-19) | подсказка не показывает число сервера; при сбое запроса подсказки нет или экран падает; анонимный экран зовёт `/auth/me` | компонентный (`linkScreens.test.tsx`, оба экрана ссылки): ответ 17 → «Не короче 17 символов;», запрос один, `/auth/me` ноль; ответ 500 → запасной текст без числа, `/auth/me` ноль | add | видимое поведение. `ChangePasswordModal` берёт тот же хук и отдельного теста не получает (число на модальном окне — в `Manual verification`) |
| `schema.d.ts` (сгенерирован) | расхождение с контрактом | `check:api` | keep | существующая проверка |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалены константа `PASSWORD_HINT` и утверждение «подсказка не называет длину»; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о них нет (поиск пуст, кроме нового комментария в `passwordPolicy.ts` о запасном тексте).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| `PasswordPolicyTests.Ac19_PasswordPolicy_ReportsTheLengthThatDeploymentConfigured_WithoutASession`, `…_ReportsTheShippedDefault_WhenNothingIsOverridden` | mutation (эндпоинт отдаёт зашитое `10` вместо `RequiredLength`): `dotnet test --solution Competency.slnx -c Release --filter-class "*PasswordPolicyTests"` → exit 2, упал тест настроенной длины 17 (тест умолчания 10 прошёл, как и ожидалось); runs: 1; мутация откатана |
| `ContractTests.Ac19_PasswordPolicy_IsPublished_AsAnAnonymousGet_WithoutA401Or429_AndWithTheMinimumLength` | n/a: закрепляет уже верный контракт нового метода, без мутации; зелёный на `openapi.json` HEAD |
| `linkScreens.test.tsx`: «password hint on the … screen (AC-19)», 2 теста × 2 экрана | mutation (хук подставляет «Не короче 8» вместо `data.minLength`): `npx vitest run src/features/auth/linkScreens.test.tsx` → exit 1, упали 2 из 16 — оба «names the minimum length…» (по экрану); тест запасного текста мутацию не затрагивает; runs: 1; мутация откатана |

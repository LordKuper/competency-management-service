# Test plan — sprint 002-mail-invite-password-reset, entry 3 (rotated)

Повествовательные строки записи 3, ротация по `artifact-layout.md` "Test plan". Не редактируется.

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 3: `AccountMail.cs` — файл модуля `Competency.UserManagement`, остальные — файлы фич `web/src/features/{auth,users}`; сборочной, CI- и общей инфраструктуры нет, предохранитель не срабатывает, набор — по ссылкам: `linkScreens.test.tsx`, `userMail.test.tsx` (web) и тесты, шлющие письма `AccountMail` (`AccountLinkTests`, `AccountLinkLimitTests`, `AccountLinkRaceTests`, `MailFailureTests`, `MailTransportTests`). Поиск по `tests` и `web/src`: ни один тест не ищет изменённые тексты (`не ждали`, `PASSWORD_HINT`, подсказку поля «Сотрудник»), `alt` логотипа или название продукта. Предстратегический прогон выполнен шире набора, дёшево: backend 157 из 157 зелёных (`dotnet test --solution Competency.slnx -c Release`, exit 0), web 82 из 82 зелёных (`npm --prefix web test`, exit 0). Сборка `Debug` занята процессом `Competency.Api` пользователя, поэтому backend в `-c Release`.

## Risk → check decisions

Запись 3 (delta с 906d167):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AccountMail.cs` (из письма-приглашения убрана фраза «Если вы не ждали этого письма…»), `LinkPasswordPage.tsx`, `passwordPolicy.ts` (`PASSWORD_HINT`), `UserForm.tsx` (убрана подсказка поля), `AuthCard.tsx` (название «Калибр» рядом с логотипом, `alt=""`) (AC-18) | правка текстов ломает существующую проверку; название продукта читается дважды (логотип с `alt` и текст) | — | none | чистая правка формулировок и вёрстки без ветвлений; существующие тесты не опираются на удалённые тексты (поиск выше, 157 + 82 зелёных без правок), поэтому корректировать нечего. Тест на отсутствие фразы или на текст подсказки закрепил бы буквальную прозу (`.claude/agent-memory/asd-dev-critical/project_tests-pin-literal-prose.md`), а не поведение (§17). Вид карточки (название рядом с логотипом) в jsdom не проверяется; пользовательский просмотр — новая строка `Manual verification` AC-18 |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённые термины дельты — фраза «Если вы не ждали этого письма…», подсказка поля «Сотрудник», `alt` логотипа «Калибр»; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о них нет.

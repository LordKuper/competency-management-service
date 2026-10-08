# Test plan — sprint 002-mail-invite-password-reset, entry 10 (rotated)

Повествовательные строки записи 10, ротация по `artifact-layout.md` "Test plan". Не редактируется.

## Risk → check decisions

Запись 10 (delta с 274b926, review-fix wave-3/iter-01):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `useSendUserMail.ts`, `UserListPage.tsx`: повторный клик по пункту меню во время отправки (F1) | второй POST со старой версией даёт 412 и ложную ошибку; администратор не видит, что письмо отправляется | компонентный тест `userMail.test.tsx` (ответ удерживается `deferred`): сообщение «Отправка письма…» показано, пункт меню `aria-disabled`, клик по нему не создаёт второго POST, после ответа сообщение уходит | add | поведение наблюдаемо через экран и сеть; доказано fail-first |
| `passwordPolicy.ts`: `Intl.PluralRules("ru")` (F2) | «Не короче 21 символов» — грамматическая ошибка в подсказке всех трёх экранов пароля | компонентный тест `linkScreens.test.tsx` в `describe.each` (оба экрана): minLength 21 → «Не короче 21 символа;» (форма «символов» уже закрыта тестом на 17) | add | граница правила «one» русского числа; fail-first |
| `useSendUserMail.ts`: текст предупреждения «Письмо не отправлено» (external #1) | текст изменился | существующие тесты `MAIL_NOT_SENT` (`/Почтовый сервер не принял письмо/`) | none | регулярное выражение уже совпадает с новым текстом; правка не нужна, предстратегический прогон зелёный |

## Added tests (запись 10)

| Test | Regression proof |
|---|---|
| `userMail.test.tsx` «shows progress and issues one request only while a send is pending, then removes the progress» (F1) | fail-first: `useSendUserMail.ts` и `UserListPage.tsx` откачены к 8942059~1 в рабочем дереве — тест красный (нет «Отправка письма…»); затем восстановлены из HEAD |
| `linkScreens.test.tsx` «declines the noun by the Russian plural rule: 21 is «символа»…» ×2 экрана (F2) | fail-first: `passwordPolicy.ts` откачен к 8942059~1 (всегда «символов») — оба случая красные; затем восстановлен из HEAD |

## Suite run (запись 10)

- Command: `npm --prefix web test`, `npm --prefix web run lint`, `npm --prefix web run build`, `dotnet build Competency.slnx -c Release --tl:off`
- Scope: запись 10 — impacted set: `userMail.test.tsx` (потребитель `useSendUserMail`, `UserListPage`, `useMailReport` через `UserModal`) и `linkScreens.test.tsx` (потребитель `passwordPolicy` через `LinkPasswordPage`, `ChangePasswordModal`); предохранитель не сработал (общая инфраструктура, сборочная и CI-конфигурация не тронуты). Web-набор мал, выполнен целиком. Backend не тронут — только сборка. Измерено дерево HEAD плюс тесты записи 10
- Result: pass — web 11 файлов / 89 из 89 (86 + 3 новых; при откате кода 3 красных, на HEAD 0 красных)
- Lint / build: pass — `lint` (biome + tsc) exit 0; `build` web ✓; `dotnet build -c Release` 0 ошибок, 0 предупреждений
- HEAD: a3f99b25d55f4acae4cbfdd47e978d627e1884a6

# Test plan — sprint 002-mail-invite-password-reset, entry 5 (rotated)

Повествовательные строки записи 5, ротация по `artifact-layout.md` "Test plan". Не редактируется.

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 5: дельта — `LoginPage.tsx` (удалён абзац-подзаголовок) и `web/src/app/productName.ts` (удалена константа `PRODUCT_TAGLINE`). Каталог `web/src/app/` — не сборочная, CI- или общая инфраструктура: файл держит только константы названия продукта, а удалённая константа имела единственного потребителя (`LoginPage`); поиск по `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` ссылок на `PRODUCT_TAGLINE` и текст подзаголовка не нашёл. Предохранитель не срабатывает. Набор по ссылкам — `linkScreens.test.tsx`/тесты `LoginPage` (web), backend не затронут. Предстратегический прогон выполнен шире набора, дёшево: backend 157 из 157, web 82 из 82. Сборка `Debug` занята процессом пользователя, backend в `-c Release`.

## Risk → check decisions

Запись 5 (delta с 6a13874):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `LoginPage.tsx` (убран абзац-подзаголовок «Компетенции и карьерный рост»), `productName.ts` (удалена `PRODUCT_TAGLINE`) (AC-18) | удаление ломает сборку или тест, ищущий текст; на экране остаётся пустой отступ | — | none | чистое удаление текста без ветвлений; ни один тест не опирается на подзаголовок (поиск выше, 157 + 82 зелёных без правок), `build` и `lint` ловят висячие импорты константы. Тест на отсутствие фразы закрепил бы буквальную прозу (`.claude/agent-memory/asd-dev-critical/project_tests-pin-literal-prose.md`), а не поведение (§17). Вид экрана пользователь уже подтвердил визуально в этом раунде, новая строка `Manual verification` не нужна |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённые термины — `PRODUCT_TAGLINE` и «Компетенции и карьерный рост»; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о них нет.

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|

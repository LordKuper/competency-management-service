# Test plan — sprint 002-mail-invite-password-reset, entry 4 (rotated)

Повествовательные строки записи 4, ротация по `artifact-layout.md` "Test plan". Не редактируется.

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 4: `src/Competency.Api/appsettings.json` — общий конфиг хоста (читается каждым тестовым хостом и всеми модулями), поэтому предохранитель срабатывает: набор вырождается в полный пакет. Остальные файлы дельты (`AuthCard.tsx`, `deploy/README.md`) локальны. Предстратегический прогон на дереве записи: backend 156 из 157 (сбой — `AccountLinkTests` AC-4: срок приглашения 72 ч против `7.00:00:00`), web 82 из 82 (`AuthCard` тестами не закреплён). Сборка `Debug` занята процессом пользователя, backend в `-c Release`.

## Risk → check decisions

Запись 4 (delta с 2d661f6):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `appsettings.json`: `AccountLinks:InvitationLifetime` `3.00:00:00` → `7.00:00:00`; sprint.md Goal и AC-4 — неделя (AC-4) | срок приглашения по умолчанию меняется молча; существующая проверка закрепляет старое значение | `AccountLinkTests.Ac4_…` (интеграционный, срок `link_expires_at - link_issued_at` в БД) | adjust | требование изменилось, это не дефект кода: ожидание `FromHours(72)` → `FromDays(7)`, текст причины обновлён. Новый тест не нужен: тот же путь уже наблюдает срок через БД (§17) |
| `AuthCard.tsx` (название «Калибр» 1.5× от heading-1 рядом с логотипом 80 px) (AC-18) | вёрстка карточки ломается на узкой ширине | — | none | чистая вёрстка (класс/размер), jsdom не считает геометрию; закрепление размера закрепило бы литерал стиля, а не поведение (§17). Новая строка `Manual verification` AC-18 для повторного просмотра |
| `deploy/README.md` (срок приглашения) | проза расходится с конфигом | — | none | документация; проверена сверкой с `appsettings.json` |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённый термин — срок приглашения 3 суток / 72 часа; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о нём не осталось (кроме `bin/` артефактов сборки).

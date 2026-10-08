---
responsibility:
  owns: one subsystem's purpose and the paths holding its key parts
  excludes: registry membership and ids, requirements, decisions, stack
  delegates_to: subsystems.md (registry), requirements/<id>.html (requirements), stack.html
---

# Журнал действий (`audit`)

## Purpose

Append-only журнал: каждая мутация учётных записей, подразделений и сотрудников, события входа и выхода, приглашений, сброса пароля и неудачной отправки письма — без паролей, хэшей, токенов, ссылок и значений-секретов; читает журнал только глобальный администратор. Модуль ссылается только на `Competency.Platform`: остальные подсистемы подключаются к нему через `[Audited]` и `IAuditWriter` из Platform, а не по проектной ссылке.

## Key paths

- `src/Competency.Audit/`: корень модуля; типы `internal`, наружу выходят `AddAuditModule` и `MapAuditEndpoints`.
- `src/Competency.Audit/AuditEvent.cs`, `AuditEntityConfiguration.cs`: таблица `audit_events` (поля `docs/Постановка.md` §29: timestamp, actor, role, action, entity_type, entity_id, old_value и new_value как `jsonb`, reason, request_id); по индексу на каждый фильтр чтения, каждый с временем в конце.
- `src/Competency.Audit/AuditSaveChangesInterceptor.cs`: пишет `<Сущность>.Created|Updated|Deleted` для сущностей с `[Audited]` в том же `SaveChanges`, что и изменение, — журнал фиксируется или откатывается вместе с ним. Журналируются только свойства из allow-list: класс и свойство помечаются `[Audited]` каждый, остальное (хэши, stamp'ы, счётчики, колонки ссылки из письма, версия, производные колонки) не пишется; для `Updated` берутся только изменённые свойства из allow-list. Ключи должны быть назначены до сохранения; `ExecuteUpdate` и `ExecuteDelete` обходят трекер и журнал и поэтому над аудируемыми сущностями не применяются.
- `src/Competency.Audit/AuditWriter.cs`, `AuditEventFactory.cs`, `src/Competency.Platform/IAuditWriter.cs`, `AuditEntry.cs`: события, которые не выводятся из изменения сущности. `WriteAsync` сохраняет событие своим scope, контекстом и транзакцией — оно переживает откат вызывающего (отказы и сбои, итог отправки письма после фиксации). `Stage` добавляет событие в контекст вызывающего — оно сохраняется вместе с изменением и существует тогда и только тогда, когда оно состоялось. Актор, роль и `request_id` берутся из `ICurrentActor`, без вошедшего пользователя (старт, bootstrap, фоновая очередь) — `system` и пустой `request_id`; `AuditEntry` может задать актора и роль явно, а `RequestId` — для работы вне вызвавшего её запроса (фоновая очередь сброса пароля журналирует id исходного запроса). Содержимое событий не логируется.
- События, которые пишет `user-management` (`AuthEndpoints.cs`, `UserEndpoints.cs`, `AccountMail.cs`, `PasswordResetQueue.cs`), тип сущности `AppUser`, id — учётная запись:
  - `Auth.LoginSucceeded`, `Auth.LockedOut` — `WriteAsync`, актор — сама учётная запись; `Auth.LoginFailed` — `WriteAsync`, актор — id известной учётной записи или `unknown`, причина `UnknownUser`, `Invited`, `LockedOut`, `WrongPassword`, `Blocked` или `EmployeeInactive`; `Auth.Logout` — `WriteAsync`.
  - `Auth.PasswordChanged` — `Stage` вместе с новым паролем.
  - `AppUser.InvitationSent` — `WriteAsync` после того, как SMTP-сервер принял письмо-приглашение; причина `Initial` или `Repeat`, актор — администратор.
  - `Auth.RegistrationCompleted`, `Auth.PasswordResetCompleted` — `Stage` вместе с паролем, заданным по ссылке; актор — сама учётная запись (запрос анонимный).
  - `Auth.PasswordResetRequested` — `Stage` вместе с выданной ссылкой сброса: от администратора — актор он; из анонимного «Не помню пароль» — актор сама учётная запись и `request_id` исходного запроса. Анонимный запрос, не выдавший ссылку (неизвестная, приглашённая или заблокированная запись, не работающий сотрудник, раньше `AccountLinks:PasswordResetInterval`), события не оставляет.
  - `Mail.SendFailed` — `WriteAsync` после неудачной отправки; причина — вид письма, `Invitation` или `PasswordReset`, без адреса; актор — администратор, из фоновой очереди — `system`.
  - `AppUser.PasswordReset` (задание пароля администратором) больше не пишется: метод удалён в контракте 2.0.0, прежние записи журнала остаются.
- `src/Competency.Audit/AuditEndpoints.cs`, `AuditQuery.cs`: `GET /api/v1/audit`, только `GlobalAdmin`; фильтры — период, актор, действие, тип и идентификатор сущности, `request_id`; страницы 50 (по умолчанию) до 200 (общие пределы — `ListRequest` в Platform; схема ответа сохраняет имя `AuditPageResponse`). Записи через API нет.
- `src/Competency.Platform/Migrations/` (общая линия миграций), миграции `AuditEvents` и `AuditTriggersEnableAlways`: триггеры `audit_events_reject_update_delete` (строчный) и `audit_events_reject_truncate` (на операцию) отклоняют UPDATE, DELETE и TRUNCATE с SQLSTATE `23001`; оба `ENABLE ALWAYS`, поэтому срабатывают и при `SET session_replication_role = replica`, которым суперпользователь отключает обычные триггеры. **Остаточный риск, принятый пользователем:** учётная запись приложения владеет схемой (`MigrateAsync` при старте) и в развёртывании подключается как `POSTGRES_USER`, суперпользователь кластера, поэтому триггер защищает от ошибок кода и SQL-инъекции в DML, но не от DDL (`DROP`/`DISABLE TRIGGER`, `DROP TABLE`). Закрыть риск целиком можно отдельной ролью БД без права на DDL — в backlog, в этот спринт не вошло.
- `docs/product/requirements/audit.html`: требования; `docs/ux/audit.html`: UX-спецификация.

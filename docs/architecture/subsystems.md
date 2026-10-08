---
responsibility:
  owns: which subsystems exist and their ids (sole subsystem registry); diagram_tool mermaid: the subsystem diagram
  excludes: a subsystem's purpose and key paths, requirements, decisions, stack
  delegates_to: <id>.md (purpose, key paths), c4/ (likec4 diagram source), stack.html
---

# Subsystems

| Id | Name | Role |
|---|---|---|
| [org-structure](./org-structure.md) | Оргструктура | Дерево подразделений и справочник сотрудников (подразделение, должность, статус «работает / не работает»); источник сведений о сотруднике для остальных подсистем |
| [user-management](./user-management.md) | Управление пользователями | Локальные учётные записи, две роли, вход и сессия, приглашение и сброс пароля по ссылке из письма, привязка учётной записи к сотруднику, правило последнего активного администратора |
| [audit](./audit.md) | Журнал действий | Append-only журнал изменений и событий входа, приглашений, сброса пароля и отправки писем; чтение только глобальным администратором |

Общее ядро (`Competency.Platform`) и хост (`Competency.Api`) не подсистемы и в реестр не входят: их механизмы описаны в `stack.html` («Архитектурные принципы»), границы модулей — в `<id>.md`.

## Diagram

Узлы `platform-core`, `postgres` и `smtp-relay` — платформенное ядро и инфраструктура, не подсистемы.

```mermaid
C4Container
  title Калибр — подсистемы и их связи
  Container_Boundary(app, "Workload app — один процесс ASP.NET Core 10") {
    Container(org-structure, "Оргструктура", "модуль Competency.OrgStructure", "Дерево подразделений и справочник сотрудников")
    Container(user-management, "Управление пользователями", "модуль Competency.UserManagement", "Учётные записи и роли, вход и сессия, приглашение и сброс пароля по ссылке из письма")
    Container(audit, "Журнал действий", "модуль Competency.Audit", "Append-only журнал изменений и событий")
    Container(platform-core, "Платформенное ядро", "Competency.Platform и хост Competency.Api", "Миграции, отправка почты, лимиты частоты, общие типы API")
  }
  ContainerDb(postgres, "PostgreSQL 18.6", "workload db", "Схема приложения и журнал audit_events")
  System_Ext(smtp-relay, "SMTP relay", "корпоративный / в разработке и тестах — Mailpit", "Доставка писем-приглашений и ссылок сброса пароля")

  Rel(user-management, org-structure, "Читает сотрудника", "порт IEmployeeDirectory")
  Rel(org-structure, user-management, "Блокирует учётную запись уходящего сотрудника", "порт IEmployeeAccounts")
  Rel(org-structure, audit, "Пишет изменения", "атрибут Audited через Platform")
  Rel(user-management, audit, "Пишет изменения и события", "атрибут Audited и IAuditWriter через Platform")
  Rel(user-management, platform-core, "Отправляет письма после фиксации", "MailSender")
  Rel(org-structure, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(user-management, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(audit, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(platform-core, postgres, "Применяет миграции при старте", "EF Core и Npgsql")
  Rel(platform-core, smtp-relay, "Отправляет письма", "SMTP через MailKit")
```

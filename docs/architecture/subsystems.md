---
responsibility:
  owns: which subsystems exist and their ids (sole subsystem registry); diagram_tool mermaid: the subsystem diagram
  excludes: a subsystem's purpose and key paths, requirements, decisions, stack
  delegates_to: <id>.md (purpose, key paths), c4/ (likec4 diagram source), stack.html
---

# Subsystems

| Id | Name | Role |
|---|---|---|
| [org-structure](./org-structure.md) | Оргструктура | Дерево подразделений, справочник должностей с перечнями допустимых должностей подразделений и справочник сотрудников (подразделение, должность, статус «работает / не работает»); источник сведений о сотрудниках и подразделениях для остальных подсистем |
| [user-management](./user-management.md) | Управление пользователями | Локальные учётные записи, две роли, вход и сессия, приглашение и сброс пароля по ссылке из письма, привязка учётной записи к сотруднику, правило последнего активного администратора |
| [audit](./audit.md) | Журнал действий | Append-only журнал изменений и событий входа, приглашений, сброса пароля, отправки писем и согласования матриц; чтение только глобальным администратором |
| [career-framework](./career-framework.md) | Карьерная модель | Профессиональные направления, карьерные треки и уровни со связью уровней с должностями оргструктуры, категории и каталог компетенций с историей версий, шкалы, версионируемые матрицы компетенций подразделений с маршрутом согласования и каскадной публикацией черновиков; закрепление HR за подразделениями и роли процесса (руководитель подразделения, ответственный HR); письма-напоминания участникам согласования |
| [desk](./desk.md) | Рабочий стол | Ожидаемые действия пользователя из всех подсистем: сбор находок модулей через контракт Platform `IDeskSource`, правило «один объект — один пункт», поглощение и порядок причин, группы, счётчик и API стола; экраны сценариев `/desk/…`; собственных данных не хранит |

Общее ядро (`Competency.Platform`) и хост (`Competency.Api`) не подсистемы и в реестр не входят: их механизмы описаны в `stack.html` («Архитектурные принципы»), границы модулей — в `<id>.md`.

## Diagram

Узлы `platform-core`, `postgres` и `smtp-relay` — платформенное ядро и инфраструктура, не подсистемы.

```mermaid
C4Container
  title Калибр — подсистемы и их связи
  Container_Boundary(app, "Workload app — один процесс ASP.NET Core 10") {
    Container(org-structure, "Оргструктура", "модуль Competency.OrgStructure", "Дерево подразделений, справочник должностей и справочник сотрудников")
    Container(user-management, "Управление пользователями", "модуль Competency.UserManagement", "Учётные записи и роли, вход и сессия, приглашение и сброс пароля по ссылке из письма")
    Container(audit, "Журнал действий", "модуль Competency.Audit", "Append-only журнал изменений и событий")
    Container(career-framework, "Карьерная модель", "модуль Competency.CareerFramework", "Справочники карьерной модели, связь уровней с должностями, матрицы компетенций, их согласование и каскадная публикация, закрепление HR и роли процесса, письма-напоминания")
    Container(desk, "Рабочий стол", "модуль Competency.Desk", "Сбор находок модулей, «один объект — один пункт», поглощение, группы и счётчик; API стола /api/v1/desk; собственных данных не хранит")
    Container(platform-core, "Платформенное ядро", "Competency.Platform и хост Competency.Api", "Миграции, отправка почты, лимиты частоты, общие типы API, контракт находок стола IDeskSource")
  }
  ContainerDb(postgres, "PostgreSQL 18.6", "workload db", "Схема приложения и журнал audit_events")
  System_Ext(smtp-relay, "SMTP relay", "корпоративный / в разработке и тестах — Mailpit", "Доставка писем-приглашений, ссылок сброса пароля и напоминаний о согласовании")

  Rel(user-management, org-structure, "Читает сотрудника, руководителей подразделений и снимок оргструктуры", "порт IEmployeeDirectory")
  Rel(org-structure, user-management, "Блокирует учётную запись уходящего сотрудника", "порт IEmployeeAccounts")
  Rel(org-structure, audit, "Пишет изменения", "атрибут Audited через Platform")
  Rel(user-management, audit, "Пишет изменения и события", "атрибут Audited и IAuditWriter через Platform")
  Rel(org-structure, platform-core, "Отдаёт находки стола", "реализует IDeskSource")
  Rel(user-management, platform-core, "Отправляет письма после фиксации; отдаёт находки стола", "MailSender; реализует IDeskSource")
  Rel(career-framework, org-structure, "Читает подразделения, руководителей, сотрудников и снимок оргструктуры, берёт блокировку дерева; связывает уровни треков с должностями", "порт IEmployeeDirectory; внешний ключ career_level_positions → positions")
  Rel(career-framework, user-management, "Читает учётные записи", "порт IUserDirectory")
  Rel(career-framework, audit, "Пишет изменения и события", "атрибут Audited и IAuditWriter через Platform")
  Rel(career-framework, platform-core, "Отдаёт находки стола; отправляет письма-напоминания после фиксации", "реализует IDeskSource; CareerMailQueue и MailSender")
  Rel(desk, platform-core, "Собирает находки модулей", "контракт IDeskSource")
  Rel(org-structure, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(user-management, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(audit, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(career-framework, postgres, "Читает и пишет", "общий AppDbContext")
  Rel(platform-core, postgres, "Применяет миграции при старте", "EF Core и Npgsql")
  Rel(platform-core, smtp-relay, "Отправляет письма", "SMTP через MailKit")
```

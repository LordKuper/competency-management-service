---
responsibility:
  owns: one subsystem's purpose and the paths holding its key parts
  excludes: registry membership and ids, requirements, decisions, stack
  delegates_to: subsystems.md (registry), requirements/<id>.html (requirements), stack.html
---

# Рабочий стол (`desk`)

## Purpose

Ожидаемые действия пользователя из всех подсистем: сбор находок модулей, правило «один объект — один пункт», поглощение и порядок причин, группы, счётчик и API стола. Стол вычисляется на сервере при каждом запросе; собственных таблиц, миграций и кэша нет. Модуль зависит только от `Competency.Platform`, модули о нём не знают: находки они отдают через контракт `IDeskSource` из Platform.

## Key paths

- `src/Competency.Desk/`: корень модуля — слияние находок, таблицы поглощения и порядка причин, группы, счётчик, методы `/api/v1/desk*`; наружу выходят только `Add…Module` и `Map…Endpoints`, хост регистрирует модуль вместе с остальными.
- `src/Competency.Platform/`: контракт находок — `IDeskSource`, `DeskFinding`, `DeskKind`, `DeskObjectType`; реализации — внутренние классы `org-structure`, `user-management` и `career-framework`, каждый регистрирует свою в `Add…Module`.
- `web/src/features/desk/`: стол и экраны сценариев `/desk/…`; маршруты — `routes.ts`.

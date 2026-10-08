[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | src/Competency.Platform/Migrations/20261005170900_AuditEvents.cs:73 (second trigger at :79) | codex F1 (blocker/critical → critical). Нарушен AC-15 (неизменяемость журнала аудита). Приложение подключается суперпользователем `competency`. Он может выполнить `SET LOCAL session_replication_role = replica`, и тогда обычные триггеры `audit_events_reject_update_delete` и `audit_events_reject_truncate` не срабатывают. Журнал можно менять через UPDATE/DELETE/TRUNCATE без DDL. Codex подтвердил это локальным spike и документацией PostgreSQL 18. Проверка: в src нет `ENABLE ALWAYS` и `session_replication_role`, то есть защиты от обхода нет. | Добавить корректирующую миграцию с `ALTER TABLE ... ENABLE ALWAYS TRIGGER` для обоих триггеров. Добавить тест, что UPDATE/DELETE/TRUNCATE отклоняются и в режиме replica под учётной записью приложения. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none

## Verdict
FAIL: 1

## Next action
Исправить F1 корректирующей миграцией (`ENABLE ALWAYS TRIGGER` для обоих триггеров) и добавить тест на обход через `session_replication_role = replica`. Затем перезапустить impl-review wave 1, iter 2.

- user decision (2026-10-07): finding 1 accepted for fix — migration ENABLE ALWAYS TRIGGER for both audit triggers + test; dedicated non-superuser DB role is a backlog candidate

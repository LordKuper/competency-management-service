[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | src/Competency.Platform/PlatformModule.cs:229 | `Smtp__SecureSocketOptions=99` проходит привязку и проверку при старте; MailKit 4.18.1 трактует неопределённое значение как соединение без TLS — ссылки с токенами и учётные данные SMTP могут уйти открытым текстом. (codex: major) | Проверка при старте `Enum.IsDefined` для `SecureSocketOptions` с именем ключа в ошибке. |
| 2 | medium | src/Competency.Platform/PlatformModule.cs:232 (эффект в MailSender.cs:28) | `Smtp__Timeout=50.00:00:00` проходит проверку на положительность; `CancelAfter` бросает `ArgumentOutOfRangeException` в MailSender.cs:28 до `try` — отправка завершается исключением вместо `false`. (codex: minor) | Отклонять при старте таймаут больше максимума `CancelAfter`, с именем `Smtp:Timeout` в ошибке. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none: 0

## Verdict
FAIL: 2

## Next action
Создатель исправляет находки 1 (high) и 2 (medium) в проверке параметров SMTP в `PlatformModule.cs`, затем повторное impl-review wave-1 iter-02. Codex не собирал проект и не запускал тесты; тестовые файлы вне `files[]` не проверялись.

## User decision

- 2026-10-08 — FAIL #1 и #2 приняты пользователем к исправлению (review-fix): проверка `Enum.IsDefined` для `Smtp:SecureSocketOptions` и верхний предел `Smtp:Timeout` (максимум `CancelAfter`) при старте, с именем ключа в ошибке.

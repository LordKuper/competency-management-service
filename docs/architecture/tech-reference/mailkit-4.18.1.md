---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# MailKit @ 4.18.1

Область: SMTP-клиент (`MailKit.Net.Smtp.SmtpClient`) для уведомлений и писем Identity через корпоративный SMTP relay; письма строит MimeKit 4.18.1 (транзитивно). IMAP/POP3 не используются.

## Canonical source
- Репозиторий и README: https://github.com/jstedfast/MailKit
- Release notes: https://github.com/jstedfast/MailKit/blob/master/ReleaseNotes.md
- FAQ (TLS/SSL, кодировки): https://github.com/jstedfast/MailKit/blob/master/FAQ.md
- NuGet (nuspec 4.18.1): https://api.nuget.org/v3-flatcontainer/mailkit/4.18.1/mailkit.nuspec — лицензия MIT; цели .NET Framework 4.6.2/4.7/4.8, .NET Standard 2.0/2.1, `net8.0`, `net10.0`; зависимости: `MimeKit` 4.18.1, `System.Formats.Asn1` 10.0.0 (+ `System.Threading.Tasks.Extensions` 4.6.3 на netstandard2.0/.NET Framework). Версии ≥ 4.15: 4.15.0, 4.15.1, 4.16.0, 4.17.0, 4.18.0, 4.18.1 (последняя стабильная).
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — 4.15–4.18 (2026-02…2026-09) новее среза знаний; сверены release notes.

## API surface used in project
- Отправка: `using var client = new SmtpClient(); await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct); await client.AuthenticateAsync(user, password, ct); await client.SendAsync(message, ct); await client.DisconnectAsync(true, ct);` (README показывает синхронный вариант; все API отменяемы и имеют async-версии).
- `SecureSocketOptions`: стандартные порты (25, 587) — `None`/`StartTls`/`StartTlsWhenAvailable`; SSL-порты (465) — `SslOnConnect`; `Auto` — автоопределение. Выбор определяется параметрами relay (открытый вопрос Q8).
- Сертификаты: `client.ServerCertificateValidationCallback` (проверка по CN/эмитенту/отпечатку вместо `=> true`), `client.CheckCertificateRevocation = false` — когда CRL/OCSP недоступны (в изолированном контуре применимо), `client.SslProtocols` — только для устаревших серверов.
- Письма (MimeKit): `MimeMessage` (`From`/`To` — `MailboxAddress`, `Subject`, `Body` — `TextPart`/`BodyBuilder`); кириллица в теме и теле кодируется библиотекой (UTF-8); `System.Text.Encoding.CodePagesEncodingProvider` (пакет `System.Text.Encoding.CodePages`) нужен только для устаревших кодовых страниц, не для UTF-8.
- Диагностика протокола: `new ProtocolLogger(...)`/`ProtocolLogger` — выводит содержимое SMTP-сессии.
- Интеграция с Identity: реализация `IEmailSender<TUser>` (`Microsoft.AspNetCore.Identity`) поверх `SmtpClient`.

## Version-specific notes
- 4.18.1 (2026-09-27): исправлена завышенная в 100 раз телеметрия длительностей (histograms) на Linux/macOS; 4.18.0 (2026-09-13): исправления телеметрии, IMAP PARTIAL.
- 4.17.0 (2026-05-26): улучшения парсера ACL/QUOTA (IMAP). 4.16.0 (2026-04-15): безопасность — внутренние буферы протокольных потоков сбрасываются после апгрейда SSL/TLS. 4.15.1 (2026-03-04): безопасность — обновление MimeKit против CRLF-инъекции в адресах почтовых ящиков в SMTP-командах. 4.15.0 (2026-02-15): поддержка .NET 10; свойства SSL-шифров помечены устаревшими в сборках .NET 10 (использовать `SslCipherSuite`).
- Настройки TLS: с 4.11.0 по умолчанию `SslProtocols.None` (системные настройки); с 4.13.0 отключён fallback на `ServicePointManager` в .NET Core.
- .NET 6 не поддерживается (с 4.9.0); NTLM/GSSAPI-механизмы SASL (4.12.0+) в проекте не нужны.
- README в репозитории ещё перечисляет .NET 6 — ориентироваться на nuspec (`net8.0`, `net10.0`).

## Deprecations and breaking changes from prior version
- 4.15.0: свойства SSL cipher (так названы в release notes) помечены устаревшими в сборках для .NET 10 → использовать `SslCipherSuite` (поимённый список свойств в изученных источниках не приведён).
- 4.13.0/4.11.0: изменения умолчаний TLS (см. выше) — актуальны при подключении к устаревшему relay.
- Прочих несовместимых изменений между 4.14.x и 4.18.1 для SMTP-отправки в release notes не обнаружено.

## Project conventions
- Транспорт — корпоративный SMTP relay (внешняя зависимость, параметры аутентификации/TLS — Q8); учётные данные и хост — только из Kubernetes Secret/ConfigMap.
- Отправка не блокирует HTTP-запрос: уведомления идут через outbox/`BackgroundService` (повторы, идемпотентность — design); `SmtpClient` не использовать одновременно из нескольких потоков (FAQ формулирует правило для `ImapClient`: `SyncRoot`; для SMTP применять то же) — экземпляр на отправку/пачку.
- Контур изолирован: проверка отзыва сертификатов (CRL/OCSP) недоступна → `CheckCertificateRevocation = false` только при подтверждённой необходимости; предпочитать валидацию по доверенной внутренней CA, а не `=> true`.
- Адреса получателей из пользовательского ввода валидировать до построения `MailboxAddress`; тексты писем и тема — русские, шаблоны в ресурсах.
- В production не включать `ProtocolLogger` (PII, содержимое писем и данные аутентификации); значения не логируются.

## Known issues and workarounds
- `SslHandshakeException`: неверная комбинация порт/`SecureSocketOptions`, недоверенный сертификат (внутренняя CA), недоступные CRL/OCSP, устаревшие протоколы сервера — см. FAQ (callback валидации, `CheckCertificateRevocation`, `SslProtocols`).
- В 4.18.1 исправлено завышение в 100 раз телеметрических гистограмм длительностей на Linux/macOS (nuspec); важно только при включённых метриках/OTLP (в стеке — условный пункт).

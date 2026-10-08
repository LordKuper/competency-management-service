---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# MailKit @ 4.18.1

Область: SMTP-клиент (`MailKit.Net.Smtp.SmtpClient`) для писем через корпоративный SMTP relay — приглашения и ссылки сброса пароля (`user-management`); письма строит MimeKit 4.18.1 (транзитивно). IMAP/POP3 и `IEmailSender<TUser>` Identity не используются.

## Canonical source
- Репозиторий и README: https://github.com/jstedfast/MailKit
- Release notes: https://github.com/jstedfast/MailKit/blob/master/ReleaseNotes.md
- FAQ (TLS/SSL, кодировки): https://github.com/jstedfast/MailKit/blob/master/FAQ.md
- Исходник `MailService` (умолчание `CheckCertificateRevocation`): https://raw.githubusercontent.com/jstedfast/MailKit/master/MailKit/MailService.cs
- NuGet (nuspec 4.18.1): https://api.nuget.org/v3-flatcontainer/mailkit/4.18.1/mailkit.nuspec — лицензия MIT; цели .NET Framework 4.6.2/4.7/4.8, .NET Standard 2.0/2.1, `net8.0`, `net10.0`; группа `net10.0`: `MimeKit` 4.18.1, `System.Formats.Asn1` 10.0.0. Версии ≥ 4.15: 4.15.0, 4.15.1, 4.16.0, 4.17.0, 4.18.0, 4.18.1 (последняя стабильная на 2026-10-05).
- Граф `net10.0` в lock-файлах проекта: MailKit 4.18.1 → MimeKit 4.18.1 (MIT; https://api.nuget.org/v3-flatcontainer/mimekit/4.18.1/mimekit.nuspec, группа `net10.0`) → `BouncyCastle.Cryptography` 2.7.0 (MIT, без зависимостей) и `System.Security.Cryptography.Pkcs` 10.0.0 (MIT, группа `net10.0` пуста). `System.Formats.Asn1` в lock-файлах нет: библиотека входит в платформу .NET 10.
- Last verified: 2026-10-08
- Оценка риска знаний (Phase 5): **MEDIUM** — 4.15–4.18 (2026-02…2026-09) новее среза знаний; сверены release notes.

## API surface used in project
- `src/Competency.Platform/MailSender.cs` — одно соединение на письмо, вся отправка под `CancellationTokenSource.CancelAfter(Smtp:Timeout)`, связанным с токеном вызывающего; example:
  ```csharp
  using var client = new SmtpClient { CheckCertificateRevocation = settings.CheckCertificateRevocation };
  await client.ConnectAsync(settings.Host, settings.Port, settings.SecureSocketOptions, timeout.Token);
  if (!string.IsNullOrEmpty(settings.UserName)) await client.AuthenticateAsync(settings.UserName, settings.Password, timeout.Token);
  await client.SendAsync(message, timeout.Token);
  await client.DisconnectAsync(true, timeout.Token);
  ```
- `SecureSocketOptions`: стандартные порты (25, 587) — `None`/`StartTls`/`StartTlsWhenAvailable`; SSL-порт (465) — `SslOnConnect`; `Auto` — автоопределение. В проекте — значение `Smtp:SecureSocketOptions`, по умолчанию `StartTls`.
- `CheckCertificateRevocation`: в MailKit по умолчанию `true` (конструктор `MailService`); в проекте — `Smtp:CheckCertificateRevocation`, тоже `true`. `ServerCertificateValidationCallback` и `SslProtocols` не используются.
- Письма (MimeKit): `MimeMessage` с `Subject` и `TextPart(TextFormat.Plain)`; `From`/`To` — `MailboxAddress.Parse`; `MailboxAddress.TryParse` проверяет `Smtp:From` при старте. Кириллица в теме и теле кодируется библиотекой (UTF-8); `CodePagesEncodingProvider` не нужен.

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
- Один конкретный класс `MailSender` в `Competency.Platform`, без интерфейса, singleton: соединение не держит, поэтому общий для запросов и фоновой очереди; `SmtpClient` — новый на каждое письмо и не используется из нескольких потоков.
- Настройки `Smtp`: `Host`, `Port`, `SecureSocketOptions`, `UserName` и `Password` (только из Secret, оба или ни одного; без логина `AuthenticateAsync` не вызывается), `From`, `Timeout` (по умолчанию 15 с), `CheckCertificateRevocation`. Проверяются при старте вне build-time генерации OpenAPI: имя хоста, порт, **определённое** значение `SecureSocketOptions` (`Enum.IsDefined` — неизвестное число иначе связалось бы и отправляло без TLS), `0 < Timeout ≤ 49.17:02:47.294` (предел `CancelAfter`, `uint.MaxValue - 1` мс), адрес `From` с доменом.
- Когда отправлять (решение пользователя 2026-10-08, вместо прежнего «отправка не блокирует HTTP-запрос»): письма по действиям администратора — синхронно сразу после фиксации транзакции, не дольше `Smtp:Timeout`, итог возвращается в ответе (`mailSent`); анонимный «Не помню пароль» — фоновая очередь в памяти (`Channel` + `BackgroundService`, `PasswordResetQueue`), потеря при перезапуске принята. Outbox и автоматических повторов нет. Никогда не отправлять внутри транзакции БД или под блокировкой.
- Отказ: `SendAsync` возвращает `false` на любое исключение, кроме отмены вызывающим, и пишет в лог только тип исключения — без адресов, текста письма и ответа сервера. `ProtocolLogger` не используется (содержимое писем, ссылки и данные аутентификации).
- Тексты и темы писем — русские, строковыми константами в коде модуля (`src/Competency.UserManagement/AccountMail.cs`), не `.resx`: писем два; вернуться к ресурсам, когда писем станет много или появится редактор шаблонов.
- Адрес получателя проверяется на границе API до построения `MailboxAddress`.
- Доверие внутреннему CA relay — шаг развёртывания (`SSL_CERT_FILE`, `deploy/README.md`); callback `=> true` запрещён; `CheckCertificateRevocation = false` — только явным решением оператора, когда CRL/OCSP недоступны из пода.
- В разработке письма уходят в Mailpit (`mailpit-1.31.4.md`), режим `None` без аутентификации; личная SMTP-песочница — через .NET User Secrets (`deploy/dev/README.md`).

## Known issues and workarounds
- `SslHandshakeException`: неверная комбинация порт/`SecureSocketOptions`, недоверенный сертификат (внутренняя CA), недоступные CRL/OCSP, устаревшие протоколы сервера — см. FAQ и `deploy/README.md` («Доверие внутреннему CA»).
- `AuthenticateAsync` к серверу без AUTH (Mailpit по умолчанию) бросает `NotSupportedException("The SMTP server does not support authentication.")`; `StartTls` к серверу без STARTTLS — тоже `NotSupportedException`. Поэтому при пустом логине аутентификация пропускается, а для перехватчика — `None`.
- В 4.18.1 исправлено завышение в 100 раз телеметрических гистограмм длительностей на Linux/macOS; важно только при включённых метриках/OTLP (в стеке — условный пункт).

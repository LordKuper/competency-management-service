[REVIEW-impl-combined]: APPROVE

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-1/iter-02
- **Severity floor (this iter)**: medium

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

Находок уровня medium и выше нет.

## Coverage (internal reviewers only)

```json
{"manifest_digest":"d27a92b93c3681000a514790223ecbfe25c104199f2fc2e9ba62e1e046579078","findings":[],"files":[{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"deploy/README.md","s":"checked"},{"i":"deploy/dev/README.md","s":"checked"},{"i":"src/Competency.Api/appsettings.json","s":"checked"},{"i":"src/Competency.Platform/MailSender.cs","s":"checked"},{"i":"src/Competency.Platform/PlatformModule.cs","s":"checked"},{"i":"src/Competency.Platform/SmtpOptions.cs","s":"checked"},{"i":"tests/Competency.Tests/PlatformTests.cs","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"},{"i":"Overall quality","s":"reviewed"}]}
```

Примечания к покрытию (без находок):
- Исправления iter-01 закрыты. External #1: `Enum.IsDefined(smtp.SecureSocketOptions)` (PlatformModule.cs:233), строка теории `99`. External #2: `MaxSmtpTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1)` (PlatformModule.cs:55, 235) — это ровно 4294967294 мс, предел `CancelAfter`; граница проверена с двух сторон (`…294` с реальной отправкой, `…295` отказ старта), и `MailSender.cs:28` больше не может бросить исключение до `try`. F2: `Uri.CheckHostName` отсекает пустое значение, `<SMTP_HOST>` и `host:port`; допускает `_` в корпоративных именах, IDN, IPv4 и IPv6. F1: `SmtpClient { CheckCertificateRevocation = settings.CheckCertificateRevocation }` (MailSender.cs:31), ключ `true` в `appsettings.json`, в `deploy/README.md` описаны оба случая, CA не доверен и CRL/OCSP недоступны; это соответствует `mailkit-4.18.1.md` (строки 23, 43, 48). F3: в `deploy/dev/README.md` порт задаётся через user-secrets.
- Решения пользователя 2026-10-08 (decisions-log.md:111, 113) не оспариваются: умолчание `true` задано и в классе, и в `appsettings.json`, предел `CancelAfter` выбран точным.
- Документация: список ключей в `deploy/README.md` (строки 75–86) совпадает с проверками `AddMail`, включая значение `49.17:02:47.294`. В утверждениях файла памяти тестера имя теста `Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_FailsNamingTheSetting` и чтение поставляемого `appsettings.json` из bin теста совпадают с кодом.
- Ручная проверка: строка AC-18 с результатом fail относится к прошлой записи (Task 9); повтор этой строки прошёл, к дельте волны 1 он не относится.
- Здесь не перепроверялось: сборку и тесты не запускал (нет shell). Результаты прогона (backend 166/166) и доказательства fail-first взяты из записи 7 в `test-plan.md`. Поведение `CancelAfter` и `Uri.CheckHostName` сверено по исходникам .NET и по скрипту тестера.

## Verdict
APPROVE

## Next action
wave-1 закрыта со стороны combined; при APPROVE от external — impl-review wave 2.

## Escalations (optional)

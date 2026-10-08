---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-08 — route Task 1: standard, dispatch HEAD c44e2a7; risk none
- 2026-10-08 — wave 1 done: Task 1 (85cbdfb)
- 2026-10-08 — route Task 2: standard, dispatch HEAD 433614a; risk artifact: cross-module refactor via git diff openapi.json + check:api
- 2026-10-08 — route Task 3: critical, dispatch HEAD 433614a; risk change: security
- 2026-10-08 — wave 2 done: Task 2 (1e54769), Task 3 (c79f50d, 3771b84, memory 5f26620); paths within Task scopes; openapi.json unchanged. Flagged choices (Platform `Rejection` name, undocumented `PageResponse<T>` with CS1591 pragma to keep openapi.json; MailSender catches non-cancel exceptions → false; UserName/Password both-or-neither validation; key/port names `Smtp:SecureSocketOptions`, `DEV_MAIL_SMTP_PORT`/`DEV_MAIL_UI_PORT` 11025/18025; CA trust documented in deploy/README.md, no MS-N) held for impl assessment; ApiHost lacks Smtp/App settings → integration tests fail at startup until impl-test
- 2026-10-08 — route Task 4: critical, dispatch HEAD 7f85c6d; risk change: authentication, change: migration, change: public contract
- 2026-10-08 — wave 3 done: Task 4 (3c092bd); SPA route /accept-invitation, POST /auth/accept-invitation, POST /users/{id}/resend-invitation, UserResponse.isInvited/mailSent, ListUsers isInvited. Flagged choices (mailSent on shared UserResponse; new invited record version 1; AccountRowLock helper shared with VerifyPasswordAsync; AccountMail singleton; config section AccountLinks; any e-mail string change clears link; invitation-specific 400 text) held for impl assessment; dev memory note committed by orchestrator
- 2026-10-08 — route Task 5: critical, dispatch HEAD a683d9d; risk change: authentication, change: security, change: public contract
- 2026-10-08 — wave 4 done: Task 5 (bc591a7); SPA route /reset-password, POST /auth/forgot-password (202), POST /auth/reset-password, POST /users/{id}/send-password-reset, LinkPasswordRequest shared by accept/reset, info.version 2.0.0; one password-reset rate-limit counter per IP across three endpoints. Minimal web compile fix (ResetPasswordModal removed, menu item removed). Flagged choices (LinkPasswordRequest rename; employee read under row lock in worker/admin path; defaults interval 5 min, limit 10/60 s, queue 1000; change-password vs reset → 412 as before; Mail.SendFailed actor system in background) held for impl assessment
- 2026-10-08 — route Task 6: critical, dispatch HEAD c4a90ec; risk change: authentication
- 2026-10-08 — wave 5 done: Task 6 (5435f37); completion gate: build 0 warnings/0 errors, web build ok, lint exit 0; all paths within Task scopes

## 2026-10-08 — impl assessment approved

- **Decision**: Пользователь принял реализацию Task 1–6 и выборы dev 1–9 (без XML-документации `PageResponse<T>` ради неизменного `openapi.json`, класс `Rejection`; `MailSender` → false на любом отказе, кроме отмены; логин/пароль SMTP — оба или ни одного; ключи `Smtp:SecureSocketOptions`, `AccountLinks:*`, порты Mailpit 11025/18025; CA relay — в `deploy/README.md`; `mailSent` в `UserResponse`, версия 1 у новой записи; `LinkPasswordRequest`; общий счётчик лимита 10/60 с на IP, интервал 5 мин, очередь 1000; чтение сотрудника под блокировкой строки; меню без подтверждения, «письмо не отправлено» — модальное окно; токен удаляется из адреса).
- **Rationale**: Выборы в рамках AC и решений аудита; ручную проверку отправки через Mailtrap пользователь выполнит сам (инструкции даны в чате), она не блокирует impl-test.
- **Affected docs**: plan.md
- 2026-10-08 — route impl-test entry 1: critical, dispatch HEAD 342baa6; risk change: authentication

## 2026-10-08 — scope amendment AC-17 (Task 7, wave 6)

- **Decision**: Добавлен AC-17: значения SMTP по умолчанию (Mailpit) и публичный адрес — в `appsettings.Development.json`; личные параметры SMTP (Mailtrap Sandbox) — через .NET User Secrets вне репозитория. Task 7 в новой последней волне 6.
- **Rationale**: Пользователь хочет постоянную настройку без ручных действий при запуске и без секретов в репозитории; переменные окружения из `launchSettings.json` перекрывали бы user secrets. Затраты AC-17: 0 итераций ревью, 0 раундов исправлений (новый критерий). Audit не переоценивается: поправка — конфигурация разработки без изменения контракта, схемы или поведения в production.
- **Affected docs**: sprint.md AC-17, plan.md Task 7
- 2026-10-08 — Task 7 (AC-17, undispatched) amended: + `MP_SMTP_DISABLE_RDNS: "true"` for dev Mailpit (impl-test entry 1 observation: ~8–10 s SMTP greeting via reverse DNS); within AC-17 "Mailpit по умолчанию", no new criterion
- 2026-10-08 — impl-test entry 1: suite green (full via safety valve, -c Release: backend 157/157, web 82/82, lint/build/check:api clean), 8 test files added, 0 tests removed (5 rewritten in place for AC-4/AC-9); manual verification rows AC-11, AC-3 deferred to the first green entry
- 2026-10-08 — impl-test: defects D-1, D-2 (agent memory, asd-dev-critical) → impl test-fix (digest 702cd83ca8c9c9d848d9341a9cd5feca9c29ab332bb4b8db89b0c0d2320c051f); the same impl entry then runs unticked Task 7 (AC-17) in initial mode
- 2026-10-08 — route test-fix D-1,D-2: standard (memory-fix dispatch to owner asd-dev-critical), dispatch HEAD eacf980; risk none via grep of the two memory files
- 2026-10-08 — impl test-fix: defects D-1, D-2 resolved (1798700, memory-fix by owner asd-dev-critical); continuing the same impl entry in initial mode over unticked Task 7 (F-1)
- 2026-10-08 — route Task 7: standard, dispatch HEAD a0d9c1d; risk none
- 2026-10-08 — wave 6 done: Task 7 (7bc40e2); completion gate: build 0/0, lint 0, paths within Task 7; impl assessment for Task 7 passed adaptively (user-authorized AC-17, risk none, Flagged choices: none); NEXT impl-test entry 2
- 2026-10-08 — route impl-test entry 2: standard, dispatch HEAD 4700f99; risk none via test run
- 2026-10-08 — impl-test entry 2 smoke check (user): AC-11 pass, AC-3 pass; remarks → AC-18

## 2026-10-08 — scope amendment AC-18 (Task 8, wave 7)

- **Decision**: Добавлен AC-18 по замечаниям smoke-проверки: убрать фразы из письма-приглашения, подсказки о пароле, вступления «Задание пароля» и подсказку поля сотрудника; на экранах без сессии рядом с логотипом — название «Калибр». Task 8 в новой последней волне 7.
- **Rationale**: Решение пользователя; название «Калибр» уже утверждено (`docs/ux/app-shell.html` «Бренд», `web/src/app/productName.ts`) — новой брендовой директивы нет. Затраты AC-18: 0 итераций ревью, 0 раундов исправлений (новый критерий). Audit не переоценивается: тексты и разметка без изменения контракта.
- **Affected docs**: sprint.md AC-18, plan.md Task 8, test-plan.md Manual verification
- 2026-10-08 — impl-test entry 2: suite green (full via safety valve; backend 157/157, web 82/82, lint/build/check:api clean), 0/0 tests (AC-17 decision none); unticked Task 8 (AC-18) → NEXT impl initial (F-2)
- 2026-10-08 — route Task 8: standard, dispatch HEAD 2ed9c4d; risk none
- 2026-10-08 — wave 7 done: Task 8 (f39d5c0); completion gate: build 0/0, lint 0, paths within Task 8; impl assessment passed adaptively (flagged: logo alt "" beside visible «Калибр» as in AppShell, plain bold name without tile); NEXT impl-test entry 3
- 2026-10-08 — route impl-test entry 3: standard, dispatch HEAD e4a8416; risk none via test run
- 2026-10-08 — impl-test entry 3: suite green (impacted, valve not fired; backend 157/157, web 82/82, lint/build/check:api clean), 0/0 tests; smoke AC-18 (user): fail — name too small, invitation lifetime must be a week

## 2026-10-08 — scope amendment (Task 9, wave 8)

- **Decision**: AC-18 уточнён: «Калибр» у логотипа — крупным шрифтом, соразмерным логотипу. Срок ссылки-приглашения по умолчанию — 7 дней вместо 72 часов (допущение в `sprint.md` Goal, AC-4). Task 9 в новой последней волне 8.
- **Rationale**: Замечания пользователя на smoke-проверке. Затраты AC-18 и AC-4: 0 итераций ревью, 0 раундов исправлений (ревью ещё не было).
- **Affected docs**: sprint.md, plan.md Task 9, test-plan.md Manual verification
- 2026-10-08 — impl-test entry 3 → NEXT impl initial for unticked Task 9 (F-2)
- 2026-10-08 — route Task 9: standard, dispatch HEAD 0fb31c2; risk none
- 2026-10-08 — wave 8 done: Task 9 (af3f5c6); completion gate: build 0/0, lint 0, paths within Task 9; impl assessment passed adaptively (flagged: name 45 px = 1.5× heading-1 token, above DESIGN.md scale — wordmark token to be proposed at design-promote, hard gate); known test AccountLinkTests.cs:51 asserts 72 h → impl-test
- 2026-10-08 — route impl-test entry 4: standard, dispatch HEAD 303e5c5; risk none via test run
- 2026-10-08 — impl-test entry 4: suite green (full via safety valve; backend 157/157, web 82/82, lint/build/check:api clean), 1 test adjusted (invitation lifetime 7 days); smoke AC-18 repeat (user): pass
- 2026-10-08 — scope amendment (user): AC-18 + no tagline «Компетенции и карьерный рост» on anonymous screens → Task 10, wave 9; cost 0 iterations / 0 fix rounds; NEXT impl initial (F-2)
- 2026-10-08 — route Task 10: standard, dispatch HEAD f4cf5c7; risk none
- 2026-10-08 — wave 9 done: Task 10 (ca1b233); completion gate: build 0/0, lint 0, web build 0; impl assessment passed adaptively (Flagged choices: none)
- 2026-10-08 — route impl-test entry 5: standard, dispatch HEAD c4d1c7a; risk none via test run
- 2026-10-08 — impl-test entry 5: suite green (impacted, valve not fired; backend 157/157, web 82/82, lint/build/check:api clean), 0/0 tests (tagline removal: none)

## 2026-10-08 — scope amendment AC-19 (Task 11, wave 10)

- **Decision**: Подсказка о пароле называет точную минимальную длину; число — из нового анонимного `GET /api/v1/auth/password-policy` (`Identity:Password:RequiredLength`). Task 11 в новой последней волне 10.
- **Rationale**: Запрос пользователя; источник — API, а не константа во frontend: это сохраняет решение спринта 001 (archived `001-project-init-org-structure` decisions-log 2026-10-07 «review-fix wave-3/iter-01 … Password hint no longer states the length», review F8, `code-style.md` §12) и даёт точное число. Затраты AC-19: 0 итераций ревью, 0 раундов исправлений.
- **Affected docs**: sprint.md AC-19, plan.md Task 11
- 2026-10-08 — route Task 11: standard, dispatch HEAD 41667f4; risk none
- 2026-10-08 — wave 10 done: Task 11 (2bc7c32): GET /api/v1/auth/password-policy (GetPasswordPolicy) → {minLength}, usePasswordHint; completion gate: build 0/0, lint 0; impl assessment passed adaptively (Flagged choices: none)
- 2026-10-08 — route impl-test entry 6: standard, dispatch HEAD 13afa16; risk none via test run
- 2026-10-08 — impl-test entry 6: smoke AC-19 (user): pass
- 2026-10-08 — impl-test: impacted set green (backend 160/160, web 86/86, lint/build/check:api clean), 4/0 tests (entry 6); all Manual verification rows have results; NEXT impl-review
- 2026-10-08 — impl-review division: 3 waves (lines 5756, files 113, bytes 405921 vs thresholds 3000/34/180000): wave 1 — platform, mail transport, AC-16 shared types, Api config, deploy (38 files); wave 2 — user-management backend, migration, openapi.json, backend tests (54); wave 3 — web screens, users UI, web tests (21)
- 2026-10-08 — impl-review wave 1 iteration 1 (floor low)
- 2026-10-08 — impl-review wave-1/iter-01: combined CONCERNS (F1 medium CRL/OCSP, F2 low Smtp:Host placeholder, F3 low README port), external FAIL (#1 high SecureSocketOptions undefined value → no TLS, #2 medium Smtp:Timeout above CancelAfter max). User: #1, #2 fix; F1 answer — add Smtp:CheckCertificateRevocation (default true) + README both cases → impl review-fix (review_fixes_pending = wave-1/iter-01)
- 2026-10-08 — route review-fix wave-1/iter-01: critical, dispatch HEAD d4be2fc; risk change: security
- 2026-10-08 — review-fix wave-1/iter-01 done (cf381d1 #1 SecureSocketOptions defined, #2 Smtp:Timeout ≤ 49.17:02:47.294, F2 Uri.CheckHostName; 2e616dc F1 Smtp:CheckCertificateRevocation default true + README both CA cases; 0e43e68 F3 README user-secrets port); completion gate build 0/0, lint 0; flagged choices accepted (class default true for CheckCertificateRevocation; exact CancelAfter max)
- 2026-10-08 — impl fix for wave-1/iter-01: findings resolved
- 2026-10-08 — route impl-test entry 7: critical, dispatch HEAD 2a188ac; risk change: security
- 2026-10-08 — impl-test entry 7: suite green (full via safety valve; backend 166/166, web 86/86, lint/build/check:api clean), 6 tests added (fail-first proven), 0 removed; NEXT impl-review
- 2026-10-08 — impl-review wave 1 iteration 2 (floor medium)
- 2026-10-08 — impl-review wave-1/iter-02: combined APPROVE, external APPROVE (both latched); wave 1 roster met → wave 2 iteration 1 (floor low)
- 2026-10-08 — impl-review wave-2/iter-01: combined CONCERNS (F1 low: doc comments — AC id in PasswordPolicyTests summary, sprint reference in AccountLinkTests summary, AuthEndpoints access matrix lacks GET password-policy), external CONCERNS (#1 high: LastAdministratorTests Ac7 race with invited admin not deterministic, AC-14 rule) → impl review-fix (review_fixes_pending = wave-2/iter-01): dev chain AuthEndpoints.cs doc comment (standard), tester chain test files F1 parts + external #1 (critical)
- 2026-10-08 — route review-fix wave-2/iter-01: standard, dispatch HEAD 2048ba4; risk none via build; route review-fix wave-2/iter-01 tests: critical; risk change: race test determinism
- 2026-10-08 — review-fix wave-2/iter-01 done (7000d33 dev: AuthEndpoints access matrix; 0eb1c83 tester: deterministic last-admin race via RowLock.HoldActiveAdministratorsAsync + pg_stat_activity waiters, doc comment fixes; 97b4db7 tester memory); fail-first: both locks dropped / registered predicate dropped → red; single-lock drops equivalent (recorded); backend 166/166; completion gate build 0/0, lint 0
- 2026-10-08 — impl fix for wave-2/iter-01: findings resolved
- 2026-10-08 — route impl-test entry 8: standard, dispatch HEAD 226300b; risk none via test run
- 2026-10-08 — impl-test entry 8: impacted set green (valve not fired; backend 44/44 impacted, build 0/0, lint/web build 0), 0/0 tests; NEXT impl-review
- 2026-10-08 — impl-review wave 2 iteration 2 (floor medium)
- 2026-10-08 — impl-review wave-2/iter-02: external APPROVE (latched), combined CONCERNS (F1 medium: asd-tester-critical memory line 12 gate-start race advice contradicts lines 18/40 and the locking rule) → impl review-fix (review_fixes_pending = wave-2/iter-02): memory-fix dispatch to owner asd-tester-critical
- 2026-10-08 — route review-fix wave-2/iter-02: standard (memory-fix, owner asd-tester-critical), dispatch HEAD 040fd05; risk none via grep of memory file
- 2026-10-08 — review-fix wave-2/iter-02 done (e260b3d memory-fix by owner asd-tester-critical: gate-start races only without a multi-row invariant; held-lock races with one known status); completion gate build 0/0, lint 0
- 2026-10-08 — impl fix for wave-2/iter-02: findings resolved
- 2026-10-08 — route impl-test entry 9: standard, dispatch HEAD 7750700; risk none via test run
- 2026-10-08 — impl-test entry 9: empty code delta; backend 166/166, lint 0; 0/0 tests; NEXT impl-review
- 2026-10-08 — impl-review wave 2 iteration 3 (floor high): external latched (iter 2), combined only
- 2026-10-08 — impl-review wave-2/iter-03: combined APPROVE (latched), external inherited APPROVE; wave 2 roster met → wave 3 iteration 1 (floor low)
- 2026-10-08 — impl-review wave-3/iter-01: combined CONCERNS (F1 medium resend/send-reset without pending guard → 412 on double click; F2 low Russian plural «символов»; F3 low «Учётная запись сохранена» in shared not-sent warning), external CONCERNS (#1 medium = combined F1, deduplicated) → impl review-fix (review_fixes_pending = wave-3/iter-01): dev chain F1+#1, F2, F3 (standard); tests via impl-test
- 2026-10-08 — route review-fix wave-3/iter-01: standard, dispatch HEAD 4bc52a0; risk none via lint/build
- 2026-10-08 — review-fix wave-3/iter-01 done (8942059: useSendUserMail {sendMail,isSending} with pending guard, loading message, disabled menu item; Intl.PluralRules «символа/символов»; shared not-sent warning without «Учётная запись сохранена»); flagged choices accepted (menu item disabled on every row while any send is pending; new loading text «Отправка письма…»); completion gate lint 0, web build 0
- 2026-10-08 — impl fix for wave-3/iter-01: findings resolved
- 2026-10-08 — route impl-test entry 10: standard, dispatch HEAD a3f99b2; risk none via test run
- 2026-10-08 — impl-test entry 10: web 89/89, lint/build 0, backend build 0/0; 3 tests added (fail-first); NEXT impl-review
- 2026-10-08 — impl-review wave 3 iteration 2 (floor medium)
- 2026-10-08 — impl-review wave-3/iter-02: external APPROVE (latched), combined CONCERNS (F1 high: in-body comment in userMail.test.tsx it(...); F2 medium: useSendUserMail JSDoc detached by SENDING_KEY const and stale) → impl review-fix (review_fixes_pending = wave-3/iter-02): dev chain F2, tester chain F1 (standard)
- 2026-10-08 — route review-fix wave-3/iter-02: standard (dev F2, then tester F1), dispatch HEAD 41c3c22; risk none via lint
- 2026-10-08 — review-fix wave-3/iter-02 done (29f4ba0 dev F2 JSDoc placement/text; ffa3b15 tester F1 isClosing helper with JSDoc, web 89/89); completion gate lint 0, web build 0
- 2026-10-08 — impl fix for wave-3/iter-02: findings resolved

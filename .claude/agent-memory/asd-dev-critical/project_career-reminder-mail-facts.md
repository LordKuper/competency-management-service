---
name: career-reminder-mail-facts
description: Career reminder mail - where recipients are computed, why names avoid UserNamesAsync, Audit has no kind list, nothing run against SMTP
metadata:
  type: project
---

As of 2026-10-13 nothing was run against Mailpit or PostgreSQL.

- `CareerMail` (scoped) computes recipients and texts after commit; `CareerMailQueue` (singleton + hosted, like `PasswordResetQueue`) rechecks account/employee and journals `Mail.SendFailed`. Call sites: end of `MatrixWorkflow.TransitAsync` and the four draft-creating handlers (named catalogue, track, competency, scale), never levels.
- Awaiting mail rule is "after the transition `after.Turn` has an approver" (null turn = published/draft), not per-action branching.
- `CatalogStore.UserNamesAsync` falls back to the account e-mail; letters must carry no addresses, so `CareerMail` looks up the bound employee name itself, with a role word fallback. Only a Return by an unbound HR hits the fallback.
- `Competency.Audit` holds no list of mail kinds: the kind is only the `Reason` string, so "mail kinds in Audit" needs no Audit code.

**Why:** tests (impl-test) find mail by recipient address; desk and later mail work must not re-derive recipients differently.
**How to apply:** new reminder kinds go through `CareerMail`; when a reminder test is red, suspect the Mailpit address lookup or the account/employee recheck first. See [[password-reset-link-facts]], [[scenario-read-facts]].

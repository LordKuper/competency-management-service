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

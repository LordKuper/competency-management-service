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

## 2026-10-13 — plan gate: правило утверждения текстов и макетов на plan удаляется

- **Decision**: по указанию пользователя AC-21 (макеты на gate plan) отозван; правило «User-visible texts approval» удаляется из `.asd/project/custom-common-rules.md` (Task 1, вместе с упоминанием в `docs/architecture/tech-reference/dnd-kit-6.3.1.md`); из AC-15 и AC-17 убраны оговорки об утверждении текстов в plan; в retro-backlog строки `002-mail-invite-password-reset#P-1` и `003-positions-career-competency#P-1` — rejected.
- **Rationale**: hard `new or changed scope` — указание пользователя на gate plan; стоимость AC-15, AC-17, AC-21: 0 итераций / 0 раундов исправлений. Разделы плана «Тексты интерфейса и писем» и «Макеты экранов» убраны; тексты остаются в постоянных UX-документах.
- **Affected docs**: sprint.md, plan.md, .asd/project/retro-backlog.md, .asd/project/custom-common-rules.md (Task 1)

## 2026-10-13 — `.asd/sprints/004-role-desktop-user-journeys/plan.md` accepted

- **Decision**: пользователь принял план (Task 1–13, 9 волн) и решения плана П-1 (матрица по черновому треку своего подразделения независимо от автора трека; правило career-framework «черновая запись — только в матрицах подразделений её автора» для трека уступает AC-8.13) и П-2 (доступность «Доработать», мастера и ссылок страницы матрицы — по `actions` версии с сервера). Заглушек к решению нет (`stubs.md` пуст).
- **Rationale**: gate plan; П-1 меняет канон career-framework — правка требований в Task 5 и при promote следующего спринта не нужна: канон AC-104 уже разрешает черновой трек своего подразделения.
- **Affected docs**: plan.md

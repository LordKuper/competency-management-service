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

## 2026-10-08 — workflow: lite

- **Decision**: Спринт 002 идёт по workflow `lite` (выбор пользователя на scope step 1; рекомендовался `standard`).
- **Rationale**: Решение пользователя; рекомендация `standard` опиралась на UI-экраны, изменение аутентификации и ретро P-2.
- **Affected docs**: state.json

- 2026-10-08 — documents frozen: audit=true (scope меняет поведение, контракт API и схему БД), prd/ux_spec/adr=true (config enabled), c4=true (diagram_tool mermaid + subsystem_decomposition enabled); user_gates=adaptive

## 2026-10-08 — scope answers: registration and admin reset

- **Decision**: Регистрация — только по приглашению администратора (учётная запись без пароля, одноразовая ссылка на e-mail). Вместо задания пароля администратором — «Отправить ссылку для сброса пароля» и «Отправить приглашение повторно».
- **Rationale**: Ответы пользователя на scope (рекомендованные варианты).
- **Affected docs**: sprint.md AC-4…AC-9, AC-11

## 2026-10-08 — retro intake: HEAD re-verification

- **Decision**: Кандидаты `retro-candidates` (5 строк `001-project-init-org-structure#P-1…P-5`, upstream-строк нет) проверены на HEAD cd6ce3b; все 5 не решены и вынесены на scope gate как AC-12…AC-16.
- **Rationale**: P-1 — `custom-coding-rules.md` всё ещё требует «full verification cycle» перед impl-test; P-2, P-4 — в `custom-common-rules.md` нет правил (P-4 частично покрыт только памятью `.claude/agent-memory/asd-dev-critical/project_scratch-process-control.md`, другие агенты не покрыты); P-3 — правила о блокировках в `custom-coding-rules.md` нет; P-5 — копии `PageResponse` (OrgStructure, UserManagement, `AuditPageResponse`), `Rejections` (OrgStructure, UserManagement), `MaxPageSize` (Audit/AuditQuery.cs, OrgStructure/ListQueries.cs, UserManagement), экранирование LIKE (OrgStructure/TextSearch.cs, UserManagement/UserListQuery.cs) на месте.
- **Affected docs**: sprint.md AC-12…AC-16

## 2026-10-08 — scope accepted

- **Decision**: Скоуп `sprint.md` принят пользователем (accept): AC-1…AC-12, AC-14…AC-16; один спринт без разделения; документы не пропускаются (audit/prd/ux_spec/adr/c4 = true).
- **Rationale**: Ретро-диспозиции (затраты по каждому критерию: 0 итераций ревью, 0 раундов исправлений — новые критерии): P-1, P-3, P-4, P-5 включены как AC-12, AC-14, AC-15, AC-16; P-2 отложена (deferred), AC-13 не используется.
- **Affected docs**: sprint.md, .asd/project/retro-backlog.md

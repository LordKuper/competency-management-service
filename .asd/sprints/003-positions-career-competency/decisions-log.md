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

## 2026-10-09 — workflow: lite

- **Decision**: Спринт 003 идёт по workflow `lite` (выбор пользователя на scope step 1; рекомендовался `standard`).
- **Rationale**: Решение пользователя; рекомендация `standard` опиралась на новую подсистему, новую доменную модель и новые экраны.
- **Affected docs**: state.json

- 2026-10-09 — documents frozen: audit=true (scope меняет поведение, контракт API и схему БД), prd/ux_spec/adr=true (config enabled), c4=true (diagram_tool mermaid + subsystem_decomposition enabled); user_gates=adaptive

## 2026-10-09 — scope answers: positions catalog, career framework, roles

- **Decision**: Справочник должностей — общий, у каждого подразделения свой перечень допустимых должностей; сотруднику доступны должности перечня своего подразделения и всех вышестоящих. Карьерные треки и матрицы — в полном объёме постановки §5–7 (включая маршрут согласования, формулу рейтинга, пороги готовности, блокирующие правила); должность справочника с карьерным уровнем трека в этом спринте не связывается; для маршрута согласования вводятся новые роли с областью действия.
- **Rationale**: Ответы пользователя на уточняющие вопросы scope; рекомендации совпали по справочнику и наследованию, по объёму матриц (рекомендован базовый), связи (рекомендована необязательная привязка) и согласующим (рекомендованы конкретные люди) пользователь выбрал иначе.
- **Affected docs**: sprint.md

## 2026-10-09 — retro intake: HEAD re-verification

- **Decision**: Кандидаты `retro-candidates` проверены на HEAD c227e5e: `002-mail-invite-password-reset#A-2`, `#P-1`, `#P-2` и `001-project-init-org-structure#P-2` (deferred) не решены и выносятся на scope gate; `002#A-1`, `#A-3`, `#P-3` — `upstream: true`, только показываются.
- **Rationale**: grep `.asd/project/custom-common-rules.md` и `custom-coding-rules.md` по «памят», «memory», «тексты», «бренд», «комментари», «одной подсистем» — совпадений нет: правил о точечной правке памяти (A-2), об утверждении текстов до impl (P-1), о самопроверке диффа на комментарии (P-2) и об одной подсистеме на спринт (001#P-2) нет.
- **Affected docs**: sprint.md, .asd/project/retro-backlog.md

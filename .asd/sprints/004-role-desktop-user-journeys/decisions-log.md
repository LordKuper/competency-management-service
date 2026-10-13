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

## 2026-10-12 — поправка скоупа: связь должностей с уровнями, проверки целостности, общее правило публикации

- **Decision**: добавлены AC-24…AC-34: связь «должность — уровень трека» (модель А); запреты для треков и матриц без должностей у уровней; проверки руководителя, HR, администратора, сотрудника (22 + «уровень без должности»); правило без дублей с поглощением; сценарии «Связать должности с треком», «Привести каталог в порядок», «Навести порядок в оргструктуре»; общее правило «нельзя опубликовать сущность со ссылками на неопубликованные» с каскадной публикацией и явным перечнем публикуемого. Изменены AC-2, AC-8 (C-1: черновой трек публикуется каскадом с версией), AC-14 (уровень и следующие уровни сотрудника), AC-20 (формулировка по C-4). Детали оркестратора 1–7 приняты пользователем. «Пересмотреть матрицу» и «Моё подразделение» не включены.
- **Rationale**: замечания пользователя при обсуждении сценариев до принятия PRD; hard gate `new or changed scope`; стоимость AC-2, AC-8, AC-14, AC-20: 0 итераций / 0 раундов исправлений (ревью не было). Плана ещё нет — Task и волна назначаются в plan. Каскад сохраняет канон career-framework AC-9 (согласование версии делает действующими её черновики), добавляя явный перечень и распространяя правило на все сущности; связь «должность — уровень» отменяет решение спринта 003 «должность и карьерный уровень не связаны». Audit-флаг остаётся true.
- **Affected docs**: sprint.md, state.json; черновик design/prd.html будет переписан

## 2026-10-12 — `.asd/sprints/004-role-desktop-user-journeys/design/prd.html` accepted

- **Decision**: пользователь принял PRD (AC-1…AC-34, выводы аналитика 1–14) с двумя ответами, вносимыми правкой без повторного круга: связь «должность — уровень» при деактивации должности или исключении из перечня остаётся, уровень только с неактивными должностями считается уровнем без должности (AC-24); версию матрицы нельзя отправить на согласование, пока у уровня её трека нет действующей должности — часть проверки полноты (AC-25; заменяет вывод аналитика AC-8.15 «отправка разрешена»).
- **Rationale**: write-then-review-accept; поправка AC-24, AC-25 — hard `new or changed scope`, стоимость 0 итераций / 0 раундов исправлений. Принят и побочный эффект AC-25: после выката новые версии матриц по существующим трекам не отправляются, пока не заведены связи.
- **Affected docs**: sprint.md, design/prd.html
- 2026-10-12 — PRD: правки AC-24, AC-25 внесены (asd-ba); AC-25.2 уточнён оркестратором по источнику — «последняя действующая должность» (следует из AC-24.10)
- 2026-10-13 — ux-spec: замечание пользователя — текст документа использовал шрифт и цвет приложения (тёмный на тёмном в тёмной теме ОС); asd-ux убрал font/color из корня .ds, оставил их только на .mockup-preview; тот же дефект в docs/ux/career-framework.html — исправить на design-promote
- 2026-10-13 — ux-spec: замечание пользователя — номера в таблицах документа переносились по дефису; оркестратор добавил .ids/.id (white-space: nowrap) для таблицы решений U-N и столбца критериев таблицы покрытия, таблицы превью не затронуты

## 2026-10-13 — `.asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html` accepted

- **Decision**: пользователь принял UX-спецификацию (22 макета, 6 схем потоков, решения U-1…U-23) после двух правок вёрстки; DESIGN.md не меняется, `design-md-delta.yaml` не создан.
- **Rationale**: write-then-review-accept; материальное UX-направление (рабочий стол — главная, группа «Справочники») утверждено пользователем.
- **Affected docs**: design/ux-spec.html; на design-promote — docs/ux/app-shell.html, career-framework.html (в т.ч. дефект корня .ds), org-structure.html, user-management.html, строка контраста accent/primary и горизонтальный Steps в DESIGN.md

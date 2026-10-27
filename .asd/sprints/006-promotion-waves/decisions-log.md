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
- YYYY-MM-DD — cleanup checkpoint <sha>
- YYYY-MM-DD — cleanup: removed <paths/symbols>; reverted <ids or none>; verified <build, lint, tests @sha>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-27 — route Task 1: standard, dispatch HEAD 4290e66
- 2026-10-27 — Task 1 flagged choices accepted: CompetencyType/ScalePoint public for the port; PublishedTrackMatricesAsync keyed by matrix unit (AC-47); extra public port records; Score edges (null threshold = none, no score → share 0 and not met); education row in MatrixBlocks; no earlier disposition found
- 2026-10-27 — route Task 2: standard, dispatch HEAD 2c26acc
- 2026-10-27 — Task 2 flagged choices accepted: ApplicationServices.cs authorised (module registration lives there, needed for EF design-time); lock files committed; TimeZone validated in AddPromotionModule; promotion_wave_matrices carries track_id/track_name; PUT waves/{id} takes 5006001 exclusive; commission is no unit-deletion blocker (released with the unit), wave units are; commission PUT semantics and 409/400 rules; date ceiling today+5y; Task 5 needs its own idempotency marker for stage-1 fixing; open gap: employee deletion blocked by promotion FKs with a generic 409 — carried to impl assessment; no earlier disposition found

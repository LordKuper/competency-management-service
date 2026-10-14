---
name: position-matrix-shared-block-facts
description: Sprint 004 Task 23 position matrices and shared blocks - where the one composition rule lives, legacy-document defaults, snapshot shape choices, test pins it breaks; nothing run on PostgreSQL
metadata:
  type: project
---

Sprint 004 Task 23 (2026-10-14), commits da17b1c, ffddd1b, a7c471c. Nothing was run against PostgreSQL.

- "Competencies of a version" = `MatrixContent.CompetenciesWith(compositions)` (own and outside-block items first, then block members, duplicates kept so `DuplicateCompetency` can see them) with `MatrixVersion.CompositionsOf(current)` deciding snapshot vs live. `CatalogStore.CompositionsAsync` loads live ones. Reuse these, never re-expand by hand.
- Legacy rows: `SharedBlocks` is an optional constructor parameter (`= null`) plus a redeclared property `= SharedBlocks ?? []`. STJ binds the parameter, so a document without the key reads empty. OpenAPI then shows `sharedBlocks?: null | array` in requests and responses alike.
- A position matrix has one level, `levelId = positionId`, created with version 1. Its snapshot has empty `Levels` and an `entries.position` (a `MatrixNamedEntry`, active or inactive). Direction and track are null. Completeness passes `positionId` as `MatrixProblem.LevelId`.
- New raw SQL never run: `MatricesUsing` (competency through blocks, block), `VersionsOn(scale)`, `BlocksHolding`, the extra `UnusedEntriesAsync` union arm (`jsonb_array_elements_text`), the block arm of `MatricesUsingCategory`. Suspect these first when impl-test is red.
- Desk: one extra query (blocks in `CatalogAsync`). The my-place matrix check reuses the loaded versions for involved users, and is one query otherwise.
- `web/src/features/career/catalogRoutes.test.ts` pins `CatalogKind` and `CatalogSegment` by `satisfies Record<…>`, so any new catalogue kind breaks `npm run lint`/`build` until the tester updates it.

**Why:** Task 24 (SPA) and impl-test build on these choices.
**How to apply:** read before touching matrix documents, snapshots, blocks or the career desk source. See [[career-desk-source-facts]], [[publication-cascade-facts]], [[career-record-deletion-facts]].

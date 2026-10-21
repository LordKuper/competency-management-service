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

- Task 28 (2026-10-15, e519c30, 069cc47): versions have no scale. A required score is a point of `LoadedEntries.ScaleOf`, which is the competency's own scale since Task 33 (9f9784b) removed the block scale. Old snapshots still carry `sharedBlocks[].scale`, which reading skips. Snapshots carry `competencies[].scaleId` and `scales`, written into old rows once by the `BlockScopeNoMatrixScale` SQL, which has never run. On the web, `scaleOfCompetency` (`matrixGroups.ts`) is the one per-competency scale lookup.
- Block scope: `CatalogViewer.HeadedSubtreeIds` is filled only by `CatalogStore.ViewerAsync` (from the walk `MatrixRoute.UnitsBelowHeadedAsync` does). Any other viewer gives a head no block rights. `CatalogEntry.ListedUnitId` (a block's scope) feeds unit names in responses and the drafts unit filter. The cascade right is `MayPublishWith`.
- `[Description]` (`[property: Description]` on record parameters) becomes the OpenAPI `description`; that is how deprecations are marked here.

- Task 38 (2026-10-20, ba069de): there are no competency categories. Released 3.0.0 surface is kept as deprecated stubs: `CompetencyCategoryEndpoints` serves an empty list and a 410, and category fields are null. Surface that never shipped was simply deleted. `NoCompetencyCategories` Down adds the columns with a placeholder default, then drops the default via `AlterColumn(oldDefaultValue:)`. That drop has never been seen to run.

- Task 47 (2026-10-20, 05f3a99): a competency's type is per version. It lives in `content.competencies[].type` and `sharedBlocks[].competencyTypes`. `MatrixContent.WithTypes` (called in `EditAsync`) stores each type explicitly; `CompetenciesWith` fills in Profile when none is set. `MatrixRequirement.Type` is deprecated and moved last, so it is optional in the schema. The migration's helper SQL functions (`migration_*`) are created and then dropped inside it, and have never run.

- Task 52 (2026-10-20, 5b3c19b): a requirement is a score alone. `MatrixContent.Blocks` is a get-only `[]`: still serialized, never read, so stored documents contain `"blocks":[]`. Every deprecated field in the docs is null. `RequirementIsScore` is one plpgsql DO block with `pg_temp` helper functions and temp tables. Its strip step selects documents by jsonpath on non-null deprecated values, so documents written by the new code are left alone. It has never run.

- Task 58 (2026-10-21) superseded the Task 38, 47 and 52 bullets. The deprecated stubs and the `Blocks` getter are gone, the migrations were squashed into one `InitialCreate`, and requirements are always filled. See [[contract-reset-and-squash-facts]].

**Why:** Task 24 (SPA) and impl-test build on these choices.
**How to apply:** read before touching matrix documents, snapshots, blocks or the career desk source. See [[career-desk-source-facts]], [[publication-cascade-facts]], [[career-record-deletion-facts]].

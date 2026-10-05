---
name: version-bump-at-pr
description: In this self-hosting repo the asd_version/CHANGELOG bump happens in the pr phase, so a migration named for the NEXT version against a not-yet-bumped release-manifest is expected at impl-review, not a defect
metadata:
  type: project
---

A migration script `.asd/migrations/<next-version>.js` shipping while `.asd/release-manifest.json`'s `asd_version` still names the OLD version is the normal state at `impl-review`. `asd-phase-pr.md` open mode step 2 does the bump ("For self-hosting, first bump version and changelog, commit them on the sprint branch"), and sprint plans state the coupling as a DoD line ("the migration filename version equals the bumped `asd_version`").

**Why:** `update.js`'s `pendingMigrations` runs a migration only when `migrationVersion > oldVersion && <= newVersion`, so mid-sprint the pair *looks* broken — a reviewer reading only the manifest would raise a false critical "migration can never run".

**How to apply:** at impl-review, verify the plan/DoD assigns the bump to `pr` and stop there; only raise it if no such assignment exists. Pairs with [[review-method-no-shell]] — you cannot run `git log` to see the bump land, so read `plan.md` + `asd-phase-pr.md` instead.

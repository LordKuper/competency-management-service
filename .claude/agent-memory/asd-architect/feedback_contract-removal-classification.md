---
name: contract-removal-classification
description: Read config backward_compat and custom-common-rules first; under none (no compat before 1.0) delete outright with no history; under migration deprecate what the released contract had, remove what only the unreleased version added
metadata:
  type: feedback
---

First read `backward_compat` in config and `.asd/project/custom-common-rules.md`. With `none` and a "no backward compatibility before 1.0" rule, delete removed surface from code, contract and docs outright. Leave no deprecation note, no stub endpoint, no old-data reading branch, no "Переход на контракт API N" section and no data-migration description. The migration line is one `InitialCreate`. The rest of this note applies only under `migration`.

When a scope change removes something from the public API while the plan fixes an unreleased minor version, sort each item by the released contract. Get it with `git show main:openapi/openapi.json` and list its paths, schemas and properties.
- Present in the released version: deprecate, do not remove. Request fields become optional and ignored. Response fields become `null`. A filter on a removed concept returns an empty result. A list endpoint returns an empty page. Create returns 410. Per-id routes are left unmapped so they return 404, as for a missing record. Mark all of it deprecated in the OpenAPI description and remove it at the next major.
- Added only in the unreleased version: remove outright, with no deprecation notice.
State exactly what a client of the released version sees, per endpoint and per field.

**Why:** design-principles require a version bump for a breaking change, and the plan's DoD pinned the minor version. The orchestrator accepted this split for the version scale, the block scale and the category removals. Later the user switched the project to `backward_compat: none` until 1.0, and every deprecation stub made under the split had to be deleted.

**How to apply:** check the policy before every contract change; use the split only under `migration` in a sprint whose contract version is not yet released.

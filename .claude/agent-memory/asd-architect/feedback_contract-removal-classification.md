---
name: contract-removal-classification
description: Under backward_compat migration with a fixed unreleased version, deprecate what the released contract had and remove outright what only the unreleased version added; read the released OpenAPI from main
metadata:
  type: feedback
---

When a scope change removes something from the public API while the plan fixes an unreleased minor version, sort each item by the released contract. Get it with `git show main:openapi/openapi.json` and list its paths, schemas and properties.
- Present in the released version: deprecate, do not remove. Request fields become optional and ignored. Response fields become `null`. A filter on a removed concept returns an empty result. A list endpoint returns an empty page. Create returns 410. Per-id routes are left unmapped so they return 404, as for a missing record. Mark all of it deprecated in the OpenAPI description and remove it at the next major.
- Added only in the unreleased version: remove outright, with no deprecation notice.
State exactly what a client of the released version sees, per endpoint and per field.

**Why:** design-principles require a version bump for a breaking change, and the plan's DoD pinned the minor version. The orchestrator accepted this split for the version scale, the block scale and the category removals.

**How to apply:** use it for every contract change in a sprint whose contract version is not yet released.

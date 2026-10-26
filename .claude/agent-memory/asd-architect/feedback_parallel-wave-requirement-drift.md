---
name: parallel-wave-requirement-drift
description: In a wave shared with UX/BA tasks, requirements and UX can change mid-task; re-check git log and fold the diffs before committing architecture docs
metadata:
  type: feedback
---

Before you commit an architecture fold that runs in the same wave as UX or BA tasks, run `git log` since the dispatch HEAD. Read every new `docs/product/` and `docs/ux/` commit and the decisions-log lines that came with them, and align the fold before you commit. Watch for UX notes that hand a trigger or rule to the architecture ("по модели данных Task N"). Define that rule in the doc. Prefer deriving it from an endpoint that already exists over adding stored state, and report any UX wording that the chosen rule makes stale.

**Why:** while an architecture fold was being written, the orchestrator aligned the requirements and the UX (draft shared blocks became includable in versions). The UX also left a marker trigger to the architecture model. Committing from the first read would have missed both.

Mid-task coordinator notes can also reverse a premise the task was written on: a single role became a set, and "the role grants no rights" became "the role grants catalogue rights". Leave the storage key and the rights model unwritten until BA commits the ACs, and draft the rest first. The BA wording decides which entity a relation belongs to. "Закрепление остаётся за сотрудником" moved `org_unit_hr` from the account id to the employee id.

**How to apply:** run this check before every architecture commit in a mixed wave, and again after any "file modified on disk" notice on plan.md.

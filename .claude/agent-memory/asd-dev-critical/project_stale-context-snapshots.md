---
name: stale-context-snapshots
description: In a wave-ordered sprint, file content injected into your context at dispatch can predate earlier waves' commits — re-read canon from disk before claiming what it says
metadata:
  type: project
---

File bodies that arrive with your dispatch (project instructions, quoted rule text, "current state" summaries) are a snapshot from session start, not from your dispatch moment. In a wave-ordered sprint, earlier waves commit to canon while your session is alive, so that snapshot can be several commits stale.

**Why:** sprint 010 Task 9 audited `AGENTS.md`'s economy paragraph. The injected copy showed the pre-Task-8 full paragraph; Task 8 had already reduced it to a pointer on disk two commits earlier. Recording a finding against the snapshot would have shipped a false claim into `audit.md`, where the next task acts on it.

**How to apply:** before asserting what a canon file says — a finding, a review claim, an SSoT check — read it from disk or `grep` it. Cheap tell: grep the exact wording your snapshot shows; no match means a sibling wave already moved it. See [[parallel-agent-commit-sweep]] for the write-side version of the same shared-worktree hazard.

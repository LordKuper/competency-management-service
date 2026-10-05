---
name: agents-md-sync-state-drift
description: AGENTS.md is an ordinary managed block in every repo since sprint 006 — the orchestrator re-baselines it normally; the old self-sourced carve-out and its permanent-drift trap are gone
metadata:
  type: project
---

This repo's root `AGENTS.md` is a normal `managed-block` target: its block is rendered from `.asd/templates/t_AGENTS.md` and the orchestrator's `sync.js --apply AGENTS.md` re-baselines `.asd/sync-state.json` like any other target. Framework-dev prose specific to this repo lives BELOW `<!-- asd:end -->`, outside the block, where sync never reaches — edit it freely, it does not affect the digest.

**Why:** until sprint `006-workflow-cost-routing`, `AGENTS.md` was *self-sourced* under `self_hosting: enabled` — content was never generated, yet the digest was still compared, and `runApply` short-circuited every self-sourced target with `applied: false`. So no command could re-baseline a hand-edit: `--check` stayed `modified-foreign` forever and the drift test could only be kept green by narrowing its assertion (which sprint 006 caught as a high finding across three reviewers). The user's resolution was to delete the carve-out rather than complete it — `isSelfSourcedAgentsMd`, `statusSelfSourcedManagedBlock`, `isInitializedConsumerProject` and the `runApply` branch are all gone. ASD is simultaneously a consumer project in which the framework is developed, so one uniform ownership model applies.

**How to apply:** after editing `AGENTS.md` or `t_AGENTS.md` (which renders this repo's block too), never run `sync.js --apply` yourself — the orchestrator re-baselines it once per wave or fix round. If you edited the framework-dev tail only, nothing changes in the digest. Never hand-patch `.asd/sync-state.json`. Related: [[asd-self-hosting]].

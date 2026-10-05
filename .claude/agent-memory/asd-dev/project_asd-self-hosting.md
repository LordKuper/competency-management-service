---
name: asd-self-hosting
description: This repo (agentic-software-development) is the ASD framework's own source, self-hosting via /asd-sprint on itself
metadata:
  type: project
---

`D:\Projects\agentic-software-development` IS the ASD framework, not a project built with ASD. `.asd/project/config.yaml` has `self_hosting: enabled`. No application code — every file is workflow infrastructure (rules, templates, agent/skill defs, hooks). "Work" = authoring/editing that infrastructure via ASD's own sprint workflow, dispatched through the ten phases like any consumer project.

**How to apply:** when asked to implement a dev task here, the "production code" is `.asd/rules/*.md`, `.asd/agents/*.md`, `.asd/workflows/*.md`, `.asd/templates/t_*`, README.md, CHANGELOG.md — not app source. Generated `.claude/`, `.codex/`, `.agents/skills/` are always read-only; edit the canonical `.asd/` source only; never run `sync.js --apply` — the orchestrator resyncs the views once per wave or fix round. `AGENTS.md`'s managed block (between `<!-- asd:begin -->`/`<!-- asd:end -->`) generates from `.asd/templates/t_AGENTS.md` like any consumer — this repo's own framework-dev prose lives below `<!-- asd:end -->` as a hand-edited override (the older "AGENTS.md stays self-sourced" carve-out was removed by user decision in sprint 006 because it made the drift-detection assertion permanently unreachable). See [[asd-mirror-ownership]].

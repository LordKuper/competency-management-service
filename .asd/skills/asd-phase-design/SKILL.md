---
{
  "name": "asd-phase-design",
  "description": "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): the main orchestrator drafts inline, section by section with lock-in, each document independently enabled via documents.* (prd.html, ux-spec.html if ux_spec enabled, adr.html, c4-full/) and runs the design-system gate only if design_system is enabled — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task Skill" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-design.md`.

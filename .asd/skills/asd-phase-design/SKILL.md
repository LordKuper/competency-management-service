---
{
  "name": "asd-phase-design",
  "description": "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): dispatches creators sequentially, one per document independently enabled via documents.* (asd-ba for prd.html, a design-system gate only if ux_spec enabled, asd-ux for ux-spec.html, asd-architect for adr.html and c4-full/) — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob AskUserQuestion Task Skill Bash(node .asd/runtime.js:*)" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-design.md`.

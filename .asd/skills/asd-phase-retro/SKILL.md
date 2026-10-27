---
{
  "name": "asd-phase-retro",
  "description": "Runs the ASD retro phase: the phase orchestrator reads the sprint friction log, derives remediation traced to F-N entries plus systemic proposals from how the sprint actually ran, merges findings sharing one root cause, drops any an existing rule in its home already covers, gives each survivor a one-line Guardrail and its Home split into consumer-project and ASD-framework actions (proposed, never applied), writes retrospective.html, in a consumer project publishes its upstream-ASD rows as GitHub issues, and posts a short chat summary. An absent or entry-free log takes the empty-log branch — remediation skipped, systemic proposals still produced — and still completes to pr. Use when asd-sprint dispatches the retro phase, or when the user explicitly asks to run or re-run retro for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash(node .asd/runtime.js:*) Bash(gh auth status:*) Bash(gh issue create:*)" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-retro.md`.

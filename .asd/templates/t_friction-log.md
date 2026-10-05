---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint {{SPRINT_ID}}

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/rules/sprint-lifecycle.md "Retro phase").
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| {{F-N}} | {{phase}} | {{one-line problem}} | {{D-N \| MS-N \| reviews/<phase>/[wave-<K>/]iter-NN/<reviewer> \| —}} |

## F-{{N}} — {{one-line problem}}

- **Phase**: {{phase the friction arose in}}
- **Surface**: {{rule | phase | gate | agent | skill | template | provider tool}} — {{exact path or id}}
- **What happened**: {{observed workflow behaviour}}
- **Impact**: {{what it cost — blocked work, rework, wrong artefact, extra round-trip}}
- **Refs**: {{id owned by an adjacent file when part of the problem lives there; otherwise —}}

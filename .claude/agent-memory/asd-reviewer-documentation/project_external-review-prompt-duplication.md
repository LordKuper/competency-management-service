---
name: external-review-prompt-duplication
description: Accepted SSoT precedent - t_prompt-external-{impl,design}.md restate the scope-vs-readability rule whose home is review-policy.md "Scope hand-off"; do not raise the duplication, only contradiction
metadata:
  type: project
---

Home: `.asd/rules/review-policy.md` "Scope hand-off" (list = only normative scope; fingerprint-named `.diff`
= change content; whole files = context only; no reviewer runs git). `external-review.md` "Phase-scoped
payload" links it, never restates it, and owns only the External-specific manifest: `phase`, `iteration`,
`wave` (impl-review only), `files[]`, `diff` (path; `null` at design-review iteration 1). No exclude_paths,
no ref pair - since sprint 017 the manifest points at a runtime-rendered diff file. Both prompt templates
restate the rule in substance (files[] = sole finding locations; diff file under `.asd/sprints/**` is
readable context, never a finding location; "never derive, widen or narrow the scope yourself");
`.asd/agents/asd-external-review.md` states a compressed variant (Operating rules, the `files[]` line) plus
the citation, and its `wraps_invoke_args` a one-line form.

**Why:** the prompts are stdin payload for the other provider's CLI, which receives only that text - a link
is not a reliable instruction carrier for an external process. Sprint 006 iter-04 accepted this after an
earlier iteration flagged contradictory paraphrases (the defect was contradiction, not duplication).

**How to apply:** raise drift/contradiction between these sites, never the duplication. `tests/run.js` pins
the prompts' carve-out sentence ("the project-context reference paths below included — is context only,
never a finding location"), their `Scope hand-off` link, the no-git/no-ref-pair ban, and the hand-off
section's list/whole-files lines; the agent-file variant and `wraps_invoke_args` are unpinned - check by hand.

---
name: doc-entries-on-code-delta
description: when a re-review manifest un-gates the Documentation entries only because agent-memory md files are in scope, how to apply the in-code doc comment entry to a code delta
metadata:
  type: feedback
---

A wave re-review manifest (sprint 001 wave-2/iter-02) did not n/a SSoT, In-code doc comments, Documentation economy and the
rest because `.claude/agent-memory/**/*.md` files were in the scope list; iter-01 had them n/a. Wave-3/iter-01 did the same.

**How to apply:**
- Read the home entry literally: "type-level doc duplicates or summarizes its members' docs" fires only when the members carry
  docs; an internal class with undocumented members and a descriptive class doc is not a finding. A class doc that merely
  restates the interface contract is a below-floor observation.
- Run the body-comment and `AC-N` Greps over `src`, `tests` and `web/src` (zero hits is the pass; `AC-N` in a test's
  `describe` or `it` name is allowed), then grep `TODO(sprint` for the stub-resolution row.
- Agent-memory files: check only for stale names (a deleted class still cited) and claims the diff contradicts; a memory
  finding is located in `.claude/agent-memory/<owner>/` and routes to a memory-fix dispatch for that owner.
- Language: when the dispatch header lists `language.docs`, write the report prose in that language (language-policy lists
  reviews as user-facing) and keep template headings and tokens in English; with no language line, keep English like the
  earlier persisted reviews of the sprint.
- Tools in a combined-review subagent may lack `Edit` and `Bash`; rewrite a file with `Write` and keep one source of text.

**Why:** avoids a medium-floor false positive on style that the project uses everywhere, and a second review-fix cycle for a doc nit.

---
name: check-host-claims-against-own-dispatch
description: A canon claim about what the host grants a reviewer (tools, memory write, maxTurns) is checkable against this dispatch's own tool list and the host docs - retro evidence about host behaviour goes stale
metadata:
  type: feedback
---

When a sprint "corrects" a canon claim about host behaviour (what tools a reviewer gets, whether memory is writable, what a frontmatter field does), compare it with your own dispatch: canon `tools`/`disallowedTools` in `.asd/agents/<you>.md` against the tool list you actually hold, plus the host subagent docs (https://code.claude.com/docs/en/sub-agents).

**Why:** sprint 019 impl-review wave-1/iter-01. AC-14 rewrote `review-policy.md` to say the host gives a reviewer no memory write tool. That was based on 017 F-4. But the docs say Read/Write/Edit are auto-enabled for a `memory` subagent. Reviewer `disallowedTools` did not name `Write`, and this dispatch held `Write`. A retro row records what the host did at that time, not what it does now.

**How to apply:** for each host-capability sentence in the diff, name the evidence (the dispatch's tool list, a docs quote), not only the retro row. Pairs with [[trace-ac-to-motivating-case]].

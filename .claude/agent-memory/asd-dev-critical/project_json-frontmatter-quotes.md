---
name: json-frontmatter-quotes
description: Agent/skill `description` lives in JSON frontmatter — a `"Section"` citation inside it breaks sync.js parsing
metadata:
  type: project
---

Canonical agent/skill frontmatter is JSON, so a double-quoted section citation (`review-policy.md` "X") pasted into `description` makes `sync.js --check` throw "canonical frontmatter is not valid JSON".

**Why:** body prose cites sections with bare double quotes; copying that style into the description string silently breaks the JSON.

**How to apply:** in `description`, drop the section citation or phrase it without quotes; run `node .asd/sync.js --check` right after any frontmatter edit.

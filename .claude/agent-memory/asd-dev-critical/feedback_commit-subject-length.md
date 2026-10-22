---
name: commit-subject-length
description: Conventional Commit subjects here must be at most 50 characters and English only; measure before committing, a dispatched dev cannot amend afterwards
metadata:
  type: feedback
---

Count every commit subject before `git commit`: at most 50 characters including the `fix(web): ` prefix, English only (a quoted UI label such as «Отмена» breaks the English rule). A natural-sounding subject with a scope prefix is usually 52-56 characters, so draft it short first.

**Why:** a whole review-fix chain once landed with a dozen subjects over 50; a dispatched agent may not amend or rebase, so only the orchestrator's pre-push squash can rewrite them, and the overrun has to be reported as a flagged item.

**How to apply:** check with `node -e "console.log([...process.argv[1]].length)" "<subject>"` (Git Bash `wc -m` counts Cyrillic as bytes) before each commit; never redirect scratch output to `$TEMP/..` (see [[temp-root-shared]]).

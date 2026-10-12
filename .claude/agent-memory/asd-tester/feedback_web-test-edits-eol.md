---
name: web-test-edits-eol
description: Scripted edits of web/ test files on Windows must keep LF line endings or biome ci fails the whole file
metadata:
  type: feedback
---

Edit `web/` test files with LF endings only. A Python `open(..., 'w')` rewrite on Windows writes CRLF, and `biome ci` then reports the whole file as a format diff.

**Why:** the repo is LF; one scripted edit turned 7 files red in lint.
**How to apply:** use `newline=''` in Python or run `sed -i 's/\r$//'` on touched files, then rerun `npm run lint` in `web/`.

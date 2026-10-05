---
name: no-python-scripted-edits
description: `python - <<EOF` hangs the Bash tool on this Windows box; script edits with node -e or the Edit tool instead
metadata:
  type: project
---

A `python - <<'EOF' ... || node -e ...` fallback hung for the full 120s timeout (C:\Python314\python.exe sat waiting) and had to be killed by pid; nothing was written.

**Why:** the heredoc-to-python path does not return under this Git Bash host; the `||` fallback never runs.
**How to apply:** for scripted multi-file text replacements use `node -e` (Node is always present here) or the Edit tool; never probe for python first. Pair with [[crlf-canon-edits]].

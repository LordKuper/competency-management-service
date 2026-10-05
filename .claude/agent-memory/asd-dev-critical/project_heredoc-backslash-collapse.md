---
name: heredoc-backslash-collapse
description: A quoted bash heredoc (<<'EOF') still collapsed `\\` to `\` when writing a Node script here, breaking a regex escape; write helper scripts with the file-write tool instead
metadata:
  type: project
---

Writing a Node helper via `cat > file <<'EOF'` turned `/[.*+?^${}()|[\]\\]/g` into `...[\]\]/g` (SyntaxError), despite the quoted delimiter. Seen sprint 019 review-fix wave-1/iter-02.

**Why:** the shell layer on this Windows/Git-Bash host rewrites backslashes before bash sees the heredoc, so quoting does not protect them.

**How to apply:** any script containing backslashes goes to the scratchpad through the file-write tool, then `node <path>`. Prefer `split/join` over regex escaping in ledger/edit helpers. Related: [[no-python-scripted-edits]], [[crlf-canon-edits]].

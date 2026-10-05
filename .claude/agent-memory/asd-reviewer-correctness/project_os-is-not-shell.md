---
name: os-is-not-shell
description: In this framework the host OS does not decide which shell runs a command - Claude Code's run-command op is `Bash` (POSIX, Git Bash on Windows) while Codex's shell tool is PowerShell on Windows; check any OS-keyed command syntax against the provider
metadata:
  type: project
---

A rule that picks command syntax (heredoc vs PowerShell here-string, path separators, quoting) from the OS alone is wrong for one provider on Windows. `providers.md` maps "run a command" to `Bash` under Claude Code, and `asd-external-review.md`'s Claude tool list grants only `Bash`, so a `win32` host under Claude still needs POSIX syntax.

**Why:** sprint 013 impl-review iter 1. AC-16 moved External Review's stdin syntax from the user-set `system.os` onto `external-preflight`'s `process.platform`, which removed the only override. Audit risk "External Review picks the wrong stdin syntax" had already named this, and nothing mitigated it. Archived sprint 001 `reviews/impl/iter-02/external.md` shows a heredoc actually working on this Windows host.

**How to apply:** when a diff keys shell syntax on an OS value, trace which tool runs it for each provider (`providers.md` semantic-op table plus the agent's frontmatter tool grants) before accepting it. Pairs with [[review-method-no-shell]]'s acting-site trace.

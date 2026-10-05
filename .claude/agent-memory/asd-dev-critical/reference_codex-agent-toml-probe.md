---
name: codex-agent-toml-probe
description: How to check locally, without auth or tokens, whether codex-cli accepts a key in a custom agent TOML (probe result for web_search on 0.156.1, 2026-09-25)
metadata:
  type: reference
---

- `codex debug prompt-input` parses config.toml/`-c` overrides but does NOT load agent role files (a broken agent TOML gives no warning). It also rejects `--strict-config`.
- `codex exec --strict-config --skip-git-repo-check "hi" </dev/null` with a throwaway `CODEX_HOME` (no auth) loads `$CODEX_HOME/agents/*.toml` and prints "Ignoring malformed agent role definition ... unknown variant/field" before the turn fails with 401. Valid keys load silently. A 401 request does go out, but no tokens are spent.
- On 0.156.1 the agent-role deserializer accepts `web_search` with the enum disabled|cached|indexed|live. Not proven: whether a spawned sub-agent applies it at runtime.

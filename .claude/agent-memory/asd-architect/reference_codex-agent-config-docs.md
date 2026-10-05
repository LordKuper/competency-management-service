---
name: codex-agent-config-docs
description: Where Codex custom-agent TOML and web_search config are documented (developers.openai.com/codex/* now 308-redirects to learn.chatgpt.com), and what they said on 2026-09-25
metadata:
  type: reference
---

Codex docs moved: `developers.openai.com/codex/config-reference` -> `https://learn.chatgpt.com/docs/config-file/config-reference`; `.../codex/subagents` -> `https://learn.chatgpt.com/docs/agent-configuration/subagents`. WebFetch does not follow the cross-host 308, so fetch the learn.chatgpt.com URL directly.

As read 2026-09-25 (codex-cli 0.156.1 on PATH):
- Custom agent TOML: required `name`, `description`, `developer_instructions`; "other supported config.toml keys" allowed (examples `model`, `model_reasoning_effort`, `sandbox_mode`, `mcp_servers`, `skills.config`). Omitted keys inherit from the parent session.
- Top-level `web_search = disabled | cached | indexed | live`, default `cached` (OpenAI index, no live external access); `live` = unrestricted retrieval. `features.web_search_request`/`web_search_cached` are deprecated aliases. There is no separate URL-fetch tool.
- `sandbox_workspace_write.network_access` (bool, default off) gates shell network access, separate from `web_search`.
- Not verified locally: whether 0.156.1 honours `web_search` inside an agent TOML. Re-check before relying on it.

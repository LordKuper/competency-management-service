---
name: host-agent-liveness
description: What each host actually exposes to observe/stop a running subagent (Claude transcript jsonl, Monitor, CronCreate, TaskStop; Codex wait_agent/close_agent) — verified 2026-09-29 for sprint 021 AC-7
metadata:
  type: reference
---

Verified 2026-09-29 (sprint 021 audit, Claude Code desktop on Windows):

- Claude progress signal: `~/.claude/projects/<project-slug>/<sessionId>/subagents/agent-<agentId>.jsonl` (documented in code.claude.com/docs/en/sub-agents). Live: it grows once per tool call and does NOT grow while a tool call is in flight (a 20 s Bash call left size/mtime unchanged). Sibling `agent-<id>.meta.json` holds agentType/description/requestShape.
- Not a signal: background agent `tasks/<id>.output` is 0 bytes running or finished (orchestrator observation). `ListAgents` shows `running` + elapsed only (not in the tools reference).
- Wake-up without user input: `Monitor` (deadline 5 min default, ≤30 min, ≤10 min under `-p`; unavailable on Bedrock/Vertex/Foundry or with DISABLE_TELEMETRY / CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC). `CronCreate` fires only while idle, 1-min granularity, deterministic per-task jitter, disabled by CLAUDE_CODE_DISABLE_CRON. `ScheduleWakeup` is only for self-paced `/loop` and is removed from subagents.
- Stop: `TaskStop` by task/agent id; a stopped subagent never auto-resumes.
- Bash: default timeout 2 min, max 10 min; a timed-out command is auto-moved to background, not killed.
- Codex: tools `spawn_agent`/`send_input`/`resume_agent`/`wait_agent`/`close_agent` (config reference, `features.multi_agent`). No documented progress read; `wait_agent(timeout_ms)` can overrun by hours under a runtime stall (openai/codex#24951, open).

**How to apply:** any rule that times out or stops agents must exempt an in-flight tool call still inside its own timeout (External Review's wrapped CLI runs up to 10 min). Re-verify before citing; see [[codex-agent-config-docs]].

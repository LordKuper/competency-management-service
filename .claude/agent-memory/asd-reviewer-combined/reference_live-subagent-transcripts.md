---
name: live-subagent-transcripts
description: Verify Claude subagent-transcript claims (agent-liveness, stop_reason shape) against this session's own finished subagent transcripts under ~/.claude/projects/<slug>/<session>/subagents/
metadata:
  type: reference
---

Finished subagent transcripts of the current orchestrator session sit at `C:\Users\Michieru\.claude\projects\D--Projects-agentic-software-development\<sessionId>\subagents\agent-<id>.jsonl`. The session id is the scratchpad dir name. They are readable with Read/Grep, with no shell.

Observed 2026-09-29 (sprint 021 impl-review): assistant entries carry `"stop_reason":null`, except the file's last line, which carries `"end_turn"` (86 lines, 1 end_turn).

**How to apply:** when a diff claims a host transcript shape (for example `runtime.js` `agentLiveness`/`transcriptProgress`), grep one finished transcript for `"stop_reason":` before accepting or rejecting the claim.

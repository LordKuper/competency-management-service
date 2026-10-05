# Memory Index

- [Codex invocation mechanics](reference_codex-invocation.md) — prompt-only stdin, manifest by path; stdout echoes payload, verdict at tail; foreground, 600000 ms timeout, never redirect to disk
- [Bash tool command-length limit](reference_bash-tool-limits.md) — >~4.5 KB command = bogus quote-EOF error; only the fixed prompt stays in the heredoc, keep it <=3 KB; merge stderr
- [Scope-manifest transport](reference_scope-manifest-transport.md) — emit-manifest writes external.scope.json, prompt names its path; wrapped CLI reads files[]/diff itself; cache/failure recording are the orchestrator's; quota handling

<!-- asd:begin v=1 -->
### Core rules

Read `.asd/rules/core.md` before any workflow action (model, interaction protocol, invariants). Codex does not support `@file` imports (plain concatenation only, `project_doc_max_bytes` capped) — this is a "read the file" instruction for every agent/provider, not an automatic include.

### Project-specific rules

Read `.asd/project/custom-common-rules.md` — universal, every agent, every phase.

Phase-scoped rules (read per phase, not globally): `.asd/project/custom-design-rules.md` (design / design-review), `.asd/project/custom-coding-rules.md` (impl / impl-test / impl-review).

### Language policy

Read `.asd/rules/language-policy.md`.

### Slash commands / skills

Invocation form differs per provider — same skill either way:

| Skill | Claude Code | Codex |
|---|---|---|
| Initialize workflow or edit settings | `/asd-init` | `$asd-init` |
| Start new sprint or continue active one | `/asd-sprint` | `$asd-sprint` |

Codex also supports `/skills` (picker) and implicit invocation by matching the skill's description; see `.asd/rules/providers.md` for the full semantic-operation → host-convention map.

### Configuration

Workflow settings: `.asd/project/config.yaml`.

Deterministic routing, external readiness and coverage validation: `.asd/runtime.js`.

External Review wraps the other provider's CLI. `system.tools.codex_command` (Claude Code) and `system.tools.claude_command` (Codex) override PATH lookup; an unavailable resolved command is recorded as an explicit review skip.

### Folder structure

Authoritative path map: `.asd/rules/artifact-layout.md`.

When subsystem decomposition is enabled (`project.subsystem_decomposition`), persistent docs are organized per subsystem.

### Rule docs (`.asd/rules/`)

`core.md` "See also" indexes every rule doc with its scope — not restated here.

### Hard rules

- Never modify workflow infrastructure (`.asd/rules/`, `.asd/templates/`, generated agent/skill/hook trees). Only `/asd-init`/`$asd-init` edits settings — run by the user, or by `impl` for a plan-declared settings change — or by a release migration `/asd-update`/`$asd-update` runs, limited to release-mandated key renames and removals, plus the value mappings, key insertions and shipped-comment rewrites that carry a renamed or removed key's or value's intent.
- All project work flows through `/asd-sprint`/`$asd-sprint`. No ad-hoc edits to project code outside a sprint.
- One active sprint at a time. New sprint blocked until active one archived, except a merged sprint, which the next sprint's scope archives.
<!-- asd:end -->

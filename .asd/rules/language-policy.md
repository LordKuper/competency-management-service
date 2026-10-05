# Language Policy

## Matrix

| Artifact type | Language |
|---|---|
| Chat with user | `language.chat` from config |
| User-facing artifacts (PRD, ADR, UX-spec, plan.md, sprint.md, reviews, persistent docs) | `language.docs` from config |
| Machine-readable files (state.json, config.yaml, settings.json, env files) | English, dense, always |
| Workflow infrastructure (rules, agents, skills, hooks, CLAUDE.md) | English, dense imperative prose, always |
| File names, git branches, commit subjects, PR titles | English, always |
| `DESIGN.md` (Google Labs format) | English, format-mandated YAML + Markdown |
| LikeC4 `.c4` files | English, format-mandated DSL |

## Translate at write time

Agents may reason internally in `language.chat`. Before `Write`/`Edit` to a user-facing artifact, translate to `language.docs`. Before writing a machine file, render in English.

## Quote translation

When an agent quotes a user-facing artefact during chat, the quote MUST be rendered in `language.chat`, even if the source file is in another language. Translate inline. Cite the source path so user can open the original.

## User-decision options

Every interactive option presented to the user — a user-decision request's `question`, `header`, every option `label`/`description`, the Problem/Options/Recommended/Consequences block, any free-text approval prompt — MUST be rendered in `language.chat`. Includes standard control options (`Lock in` / `Revise this section` / `Skip` / `Approve` / `Edit` / `Reject` / `Confirm` / `Rollback` / etc.). Translate at the call site; never emit English option labels when `language.chat` is non-English.

Internal signal tokens crossing the agent boundary (`COMPLETED`, `FAILED`, `QUESTION`, `ABORT`, `APPROVE`, `CONCERNS`, return-contract strings) stay in English — machine signals, not user-facing text.

Free-text approval is not a substitute for a required hard-gate decision. Gate class, exact existing authorization reuse, and adaptive evidence are governed only by `checkpoints.md`; this file sets the language. A strict write-then-review-accept gate requires explicit `accept`; revision feedback remains free text.

## Write-then-review-accept: chat-language self-sufficiency

For `write-then-review-accept` gates, the written artifact is in `language.docs`, which may differ from `language.chat`. The delta summary posted in chat (`checkpoints.md` step 2) MUST be self-sufficient for the user to give informed feedback in `language.chat` — do not just say "see the file"; summarize what changed and why, in `language.chat`. Key changed passages MAY be quoted-and-translated per "Quote translation" above, but never the full artifact body (`checkpoints.md` no-content-dumps rule).

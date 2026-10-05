---
name: scope-manifest-transport
description: scope manifest travels by path (external.scope.json written by emit-manifest, named in the prompt via the manifest-path slot), never inline in stdin; manifest fields; diff is a precomputed file path read by the wrapped CLI; cache/failure recording belong to the orchestrator; quota-error handling; never redirect or write to disk
metadata:
  type: reference
---

Keyed by topic; fold new lessons into a heading. Loads on every dispatch.

## Transport: manifest by path, prompt only on stdin

`external-review.md` "OS-specific invocation" and "Phase-scoped payload" are the SSoT. The orchestrator runs `node .asd/runtime.js emit-manifest --reviewer external --iteration <N> [--wave <K>]`, which writes `external.scope.json` (`t_review-scope.json`) and its diff file into the review output dir BEFORE this agent is dispatched. This agent's prompt = per-phase template + project context + the manifest PATH (slot `{{SCOPE_MANIFEST_PATH}}`). Only that prompt goes via heredoc/here-string stdin. The manifest, `files[]` and the diff file are never concatenated into stdin and never rendered inline: the wrapped CLI reads the manifest first, then `files[]` and the `diff` file from the repo with its own read-only tools. This agent writes nothing to disk.

Manifest fields: `phase`, `iteration`, `wave` (impl-review only), `files[]`, `diff`. No `exclude_paths[]`; the phase applies pathspec exclusions when building `files[]`. `files[]` is the sole normative scope and the only valid finding-location set.

`diff` is the path of a runtime-written, fingerprint-named `.diff` file for exactly that `files[]` list (deletions included), under the sprint's reviews dir: readable context, never a finding location. `null` only on the first design-review iteration. The wrapped CLI never computes a diff itself; never pipe a live `git diff` on stdin.

If the manifest path in the prompt is missing or the file is absent, that is a precondition failure before invocation: abort, do not recreate the manifest.

## Cache path and failure recording are the orchestrator's

Canonical cache path is `.asd/project/external-cache.json` (gitignored). This agent never calls `.asd/runtime.js` or names the path: `external-preflight` and `external-record-failure` are phase-orchestration calls; this agent only consumes the preflight result handed to it. A dispatch instructing otherwise contradicts canon: flag it.

`external-record-failure` syntax, for reading orchestrator output: `--input` takes a PATH or `-` for stdin, not inline JSON. Fields: `fingerprint` (64-hex), `status` (`authentication|quota|reachability|command`), `cachePath`, `retryAfter` (epoch-ms number: the provider-reported reset; runtime caps it at one hour, uses one hour when none reported), optional `now`.

## Quota errors

The wrapped CLI can print `ERROR: You've hit your usage limit ... try again at <time>` on the first real request, and the same on an immediate retry.

- A negative-cache TTL expiring is not proof the provider quota reset; `local-ready` is not proof the paid request succeeds.
- Retrying with the same quoted reset time is pointless: one retry only.
- `2>/dev/null` hides the failure (empty result); always merge stderr with `2>&1`.
- After the retry fails return `external review interrupted: quota exhausted (reset <provider time>)`; never an availability skip after invocation; never fabricate findings. The orchestrator records the failure.

## Instructed to redirect output to disk

Contract: no file writes at all, review text via captured stdout only, no temp file. If a dispatch payload asks to redirect the wrapped CLI's output to a file (for example to survive an interruption), decline and cite the tool-policy line "captured stdout IS the review text". On an interrupted turn re-run the invocation clean; never add a disk hop, and never record such a workaround as guidance.

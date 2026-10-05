---
responsibility:
  owns: external review aggregation report (kept findings + dropped-category counts per iteration)
  excludes: wrapped-CLI raw prompt, internal reviewer output, per-finding dropped accounting (category counts only)
  delegates_to: t_prompt-external-{design,impl}.md (prompts), t_review.md (internal reviewer output)
---

[REVIEW-{{REVIEW_PHASE}}-external]: {{APPROVE | APPROVE (skipped: external review unavailable: <specific status>) | CONCERNS | FAIL}}

# External Review Report

- **Phase**: {{design-review | impl-review}}
- **Iteration**: {{design-review: N | impl-review: wave-<K>/iter-NN}}
- **Severity floor (this iter)**: {{low | medium | high | critical}}
- **Unreviewed files**: {{skip record only — every `files[]` path it would have sent; external-review.md "Iteration semantics"}}

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | {{low/medium/high/critical}} | {{location}} | {{description}} | {{fix}} |

<!-- Severity cell: exactly one of low|medium|high|critical, nothing else (no CLI label) -->
<!-- when no findings, leave one row: -->
<!-- | — | — | — | no findings | — | -->

## Dropped findings (counts only)

- Below severity floor (iter {{N}}, floor {{floor}}): {{count}}
- Nitpick, by category: {{nitpick category}}: {{count}}{{, ...}}

## Stalemate
{{stalemate FAIL only — omit otherwise; options and effects: external-review.md "Stalemate detection"}}

Stalemate: {{N}} iterations, identical findings — options: stop / continue fixing / abort

## Verdict
{{APPROVE | CONCERNS: <count> | FAIL: <count>}}

## Next action
{{what creator/orchestrator must do next}}

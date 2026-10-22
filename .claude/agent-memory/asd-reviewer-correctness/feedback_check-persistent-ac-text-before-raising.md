---
name: check-persistent-ac-text-before-raising
description: Product impl-review in this app - an odd-looking rights or scope rule in code is usually the literal text of a persistent requirement; read the AC row in docs/product/requirements/*.html before raising it
metadata:
  type: feedback
---

Before you raise a rights, visibility or scope rule that looks inconsistent, read the matching AC row in `docs/product/requirements/<subsystem>.html`. Do this whenever the code's doc comment states the rule on purpose ("whatever the level's own status", "except in a cascade").

Persistent AC ids are not the sprint's `sprint.md` AC-N. After design-promote the ids are renumbered per subsystem. Grep `id="AC-<n>"` plus a key phrase instead of mapping by number. A long row comes back as "[Omitted long matching line]"; Read that one line by offset.

**Why:** in a backend impl-review wave, four suspected asymmetries each turned out to be the literal requirement text, so none was a defect:
- any HR approves a unit-scoped shared block directly, but in a cascade only the unit's responsible HR may approve it;
- "no track for your position" is checked across all units;
- unlinking the last active position of an inactive level is refused;
- the HR-role removal check is keyed on the requested employee binding, because unbinding is explicitly allowed.

**How to apply:** for each candidate finding, quote the AC row. Raise it only when the code contradicts the row, or when no row covers the case. The architecture doc `docs/architecture/<subsystem>.md` also states endpoint contracts, such as which actions are `publishingActions`; check it before you raise a Contracts finding. Related: [[design-review-derived-actor-paths]], [[review-method-no-shell]].

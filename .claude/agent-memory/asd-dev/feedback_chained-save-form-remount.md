---
name: chained-save-form-remount
description: Forms keyed by record version remount on every rebase and drop server field errors set after a chained save-then-act; key by a reread counter instead.
metadata:
  type: feedback
---

An edit form keyed by `base.version` (the usual `useEditBase` + `rebase` pattern) remounts on every rebase. When one action saves the record and then calls a second endpoint (save draft, then submit), a refusal of the second call arrives after the rebase: `useFormSubmit` puts the field errors on the old form instance and they vanish.

**Why:** a remount rebuilds the antd form from the saved record, so errors set with `setFields` belong to an instance that is already gone.

**How to apply:** for such a flow key the editor by a counter that grows only when `useEditBase` swaps `base` after a 412 (mark the refusal in a ref in `onError`, bump the counter in an effect on `base`), and let the view read the live record through props. Choose the screen branch (new draft, existing draft, filed) once from the first loaded list, so the list refetch after a create does not switch the branch and remount the form. Single-request forms can keep the version key.

Also: an antd `Form.Item` with a `name` must wrap the control itself, not a `Flex` holding control and button; a component defined inside another component's render remounts its inputs on each keystroke, so use a plain render function there.

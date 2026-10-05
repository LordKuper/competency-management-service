---
name: re-review-mirror-pointers
description: when re-reviewing a fix that rewords a placement or order rule, check the mirrors' pointer phrases and the tester's recorded ceilings, not only the pinned sites
metadata:
  type: feedback
---

A fix that makes a rule's order explicit leaves its mirrors (templates, format-rule bullets) carrying pointer phrases such as
"same placement" or "as above". A chain of such pointers inherits the changed rule and can compute an order the home forbids,
for example the third item reading "ahead of" the second when the home puts it after.

**Why:** the pins the tester adds read the sites they name; a pointer line next to them is read by no assert, and the
test plan records it only as a ceiling.

**How to apply:**
- After a rule-wording fix, grep the mirrors for the reworded span, then read each sibling bullet that points back with
  "same" or "likewise" and resolve the chain against the home.
- Read the "Ceilings" text of the test-plan entry files: a ceiling the tester names as a template or canon edit outside its
  grant is a reviewer finding to raise, not a note to pass.
- A finding on a file outside the manifest is anchored on the in-scope rule whose mirror it is; say so in the row.

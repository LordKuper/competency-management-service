---
name: invitation-link-facts
description: Sprint 002 Task 4 gotchas - Identity CreateAsync overwrites the security stamp, positional-record OpenAPI required-ness, CS1573 on partial param docs
metadata:
  type: project
---

- `UserManager.CreateAsync` sets a fresh SecurityStamp itself, so anything bound to the stamp (the link's `LinkSecurityStamp`) must be issued after it, which costs a second save in the same transaction: a just-created invited account answers with version 1, not 0.
- Microsoft.AspNetCore.OpenApi marks every positional-record constructor parameter `required`, even with `[JsonIgnore(WhenWritingNull)]`. An optional field that is omitted on the wire would then be a contract lie, so `UserResponse.mailSent` is always serialized (null when no mail was sent).
- Documenting only some positional-record params with `<param>` risks CS1573, which TreatWarningsAsErrors turns into an error; put the facts in `<summary>` instead.
- An anonymous endpoint mapped on the `/api/v1/auth` group inherits its `ProducesProblem(401)`. Map it on a fresh `MapGroup(Route).WithTags(Tag)` so the contract has no 401 the SPA's global 401 handler would act on.

**Why:** each of these cost a rebuild or a contract re-check in Task 4.
**How to apply:** Task 5 (reset links) and later account-link work.

---
name: identity-efcore-store-gotchas
description: ASP.NET Core Identity 10 on a plain DbContext - the stock EF user store marks every column modified and swallows concurrency into a result, needs only the users table; EF HasOne(string) takes a navigation name
metadata:
  type: project
---

Verified on SDK 10.0.400, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12 (store source read at tag v10.0.12), EF Core 10.0.12, Npgsql provider 10.0.3, with an InMemory logic host and a SQL-capture host (no PostgreSQL):

- A plain `DbContext` works as the Identity context: `AddIdentityCore<TUser>().AddUserStore<MyStore>()` with a store derived from `UserOnlyStore<TUser, TContext, TKey>`, and only the user entity mapped. Create, find, password check, access-failed counting, change password and security-stamp update touched nothing but the users table, so no role, claim, login, token or passkey table is needed until a feature calls those APIs. `IdentityUser` members that are not mapped (e-mail, phone, two-factor) are `Ignore`d on the entity; the base class members stay usable in memory.
- `UserOnlyStore.UpdateAsync` does `Attach`, a new `ConcurrencyStamp`, `Update(user)` (every column flagged modified) and `SaveChanges`, and turns `DbUpdateConcurrencyException` into `IdentityResult.Failed(ConcurrencyFailure)`. By the audit interceptor's `IsModified` rule that would list every allow-listed property as changed on every save of a tracked user, even a failed sign-in counter (inferred from the source, the stock store was not run); an override that skips `Update` (the entity is already tracked) writes only changed columns (`UPDATE users SET concurrency_stamp, is_blocked, security_stamp, version WHERE id AND concurrency_stamp AND version`). A stale `If-Match` therefore comes back as a failed result to map to 412 yourself, not as the exception the platform handler maps.
- Every `UserManager.UpdateAsync`/`UpdateSecurityStampAsync` first runs the user validator, a `FindByName` query on `normalized_user_name`; password hashes are the v3 format (`AQAAAAIAAYag...`: HMAC-SHA512, 100 000 iterations). The user-name validator allows only `A-Za-z0-9-._@+` by default.
- `UserManager.GetUserAsync(principal)` reads `IdentityOptions.ClaimsIdentity.UserIdClaimType` (default `ClaimTypes.NameIdentifier`); set it to the claim the sign-in actually issues or it returns null for every principal.
- `EntityTypeBuilder.HasOne(string)` takes a navigation name, not a type name: a foreign key to an entity whose CLR type is internal to another assembly is `HasOne("Full.Clr.Name", navigationName: null).WithMany().HasForeignKey("Prop")`; the other entity must already be in the model, so the contributor that configures it must be registered first.
- When a rejected session cookie (revoked stamp) arrives with a fresh sign-in, the response carries two `Set-Cookie` headers, the deletion then the new ticket, and a cookie jar ends with the new one.
- EF InMemory applies the entries of one `SaveChanges` one by one without rollback: an audit row added by an interceptor in the same call survives a concurrency failure on the other row. PostgreSQL rolls the whole batch back, so such a row in an InMemory run is not evidence of a bug.

**Why:** each decided a design choice (store override, 412 mapping, schema size, cross-module foreign key) or explained a confusing result.
**How to apply:** touching sign-in, account updates, audit of Identity entities or cross-module foreign keys.

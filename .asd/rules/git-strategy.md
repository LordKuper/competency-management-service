# Git Strategy

## Branch

Created in `scope` from `git.base_branch` per `git.branch_pattern`. Default pattern `sprint/{n}-{slug}`. `{n}` zero-padded to 3 digits. `{slug}` kebab-case, max 30 chars, derived from scope.

Before creating: fast-forward local `git.base_branch` to `origin/<base_branch>` (below), working tree clean (see "Pre-existing uncommitted changes"). Never commit or push directly to `git.base_branch` — every change lands via PR.

**Fast-forward**, run at `/asd-sprint` start (before sprint detection) and again before creating. Base not checked out (sprint branch, detached HEAD): `git fetch origin <base_branch>:<base_branch>` — moves the ref only, works on a dirty tree, refuses a non-fast-forward. Base checked out: `git fetch origin`, then `git merge --ff-only origin/<base_branch>`; never `--update-head-ok`. Any refusal (diverged, an incoming change overlapping a dirty file) halts and asks the user to resolve. Unreachable remote: warn, continue on local state.

## Commits

- Conventional Commits: `<type>(<scope>): <subject>`
- Subject ≤ 50 chars, imperative mood, English
- Body describes WHY, not WHAT
- One commit per task when possible; phase-grouped acceptable for small tasks
- A dispatched agent commits in the one-command form of "Commit before review"
- A dispatched `asd-dev`/`asd-tester` commit carries one trailer line `ASD-Task: <id>` per id it covers — `Task N`, a review finding id (its id in the reviewer's ledger `findings`, prefixed by its review file when another file of the iteration reuses it: `testing.md F-1`), `D-N`, `impl-test entry N` for impl-test's own commits, or `impl-review wave-<K>/iter-NN suite` (its iteration id, `sprint-lifecycle.md` "Review iteration counters") for impl-review step 9's in-place test fix; impl-review's in-place fix of low-severity test-only findings (`review-policy.md` "Low-severity test-only findings") uses the review finding id form
- Before push: squash local WIP/fixup commits into task-level commits (`git reset --soft` + recommit, or non-interactive rebase). Applies to unpushed commits only — published history stays untouched (see Forbidden)

## Forbidden

- Never force-push
- Never rebase published commits
- Never use `--no-verify` or skip hooks
- Never commit `.env`, credentials, or `.gitignore`-matching files
- Never commit or push directly to `git.base_branch`

## TODO stubs

In-code TODO created during a sprint must be marked `// TODO(sprint-NNN): <reason>` and registered in **project-global** `.asd/project/stubs.md` (open stubs only) with: sprint of origin (NNN-slug), file path and line, reason (prefix `(accepted-debt)` for known debt that should not block PR), owner agent.

On resolution: row **deleted** from stubs.md (no status column; deletion = resolution). On migration: deleted, new row created under the receiving sprint.

`pr` phase blocks if any stub has `Sprint = <current-NNN-slug>` and Reason does NOT start with `(accepted-debt)`. Devs must resolve, migrate, or mark accepted-debt before PR.

## Commit before review

`impl` (and its review-fix/test-fix modes) and `impl-test` each commit all work before the sprint advances to `impl-review` — the clean-worktree precondition there (`sprint-lifecycle.md` "Impl-review clean-worktree precondition") blocks entry on any uncommitted change, since the reviewed diff is computed from commits. `impl-test`'s own commit obligation is stated once, in `sprint-lifecycle.md` "Impl-test commits its own output".

`impl-review` itself also commits: when it dispatches `asd-tester` to fix a test in place, that fix must land as a commit before the phase's `Suite run` records its `HEAD` and before `pr` open-mode's `git diff --quiet` skip check runs — an uncommitted in-place fix is invisible to both.

The main orchestrator commits its own bookkeeping — `state.json`, `decisions-log.md`, review files, `friction-log.md`, and any agent-memory writes whose author cannot commit them (a reviewer holds no commit tool; a concurrent co-author holds the file mid-edit, so neither author may stage it, per `artifact-layout.md` "Agent memory"; `review-policy.md` "Change-surface rule") — at phase exit, same precedent as `impl-test` above. It likewise commits the drafts of a dispatched creator holding no commit tool. "Holding a commit tool" is role policy, not tool grant: `asd-dev` and `asd-tester` hold one; reviewers, `asd-advisor` and `asd-ba`/`asd-ux`/`asd-architect` do not, whatever shell they hold. Ownership is symmetric for a dispatched agent holding a commit tool: it stages only the paths it authored (never a whole-tree command — `git add -A`/`-u`, `git add --renormalize`, `commit -a`, `git stash` — even where it is the only form that answers the question, since concurrently dispatched tasks share one worktree: a whole-tree stage sweeps a sibling's in-progress edit into the wrong commit, a whole-tree stash takes it off disk), commits every path it authored before signalling completion (an authored file no one commits reaches neither the reviewed diff nor `HEAD`), and never commits orchestrator-owned files it did not author, even to leave a clean tree for the next gate. It stages and commits in one compound command, so no path stays staged between commands: `git add -- <paths> && git diff --cached --check -- <paths> && git commit --only -- <paths>`, one `<paths>` list covering never-tracked paths and both paths of a rename; if it fails, `git reset -q -- <paths>`. Such an agent's own agent-memory writes are paths it authored, so it commits them; the orchestrator clause above covers only a write whose author cannot. That whole-tree ban is unconditional for a dispatched agent, which cannot observe whether a sibling dispatch is in flight; the main orchestrator can, and runs such a command only when none is.

**Memory content check**: at the commit of agent-memory writes it holds (the clause above), the orchestrator stages them and runs `node .asd/runtime.js memory-check` first; it reads the staged diff, prints a JSON array of `{path, line, token}` hits and exits 1 on a hit (rule: `artifact-layout.md` "Agent memory"). A violating write is unstaged and not committed; it returns to its owner's memory-fix dispatch (`review-policy.md` "Memory-fix dispatch"). A dev's or tester's own memory commit is not gated; legacy memory text is not swept.

## PR self-review checklist

The main orchestrator confirms before opening PR:

- Studied existing code in touched areas
- Can explain every changed line
- PR scoped to requested feature; no unrelated improvements
- Commit messages describe why, not what
- Full test suite green, once, at the end of `impl-review` (per `test-plan.md` `Suite run`; `sprint-lifecycle.md` "Impacted test set")
- Documentation reviewer verdict = APPROVE

## PR creation

Triggered only after DoD met and the active `checkpoints.md` policy permits publication. In strict this needs user confirmation; adaptive publication needs recorded authority/evidence and any host permission.

- PR title MUST follow Conventional Commits (`<type>(<scope>): <subject>`) — becomes the squash-merge commit subject
- Push branch, then `gh pr create` with body from `t_pr-description.md`
- `gh` is the only PR path (`/asd-init` requires it). `gh` itself failing is `FAILED` naming the fix for its cause: not installed → install gh (https://cli.github.com); not authenticated → `gh auth login`; host unreachable → "host unreachable, retry online". Never a manual-PR fallback. Sole exception: a head-branch lookup outside `phase="pr"` warns and resumes (`sprint-lifecycle.md` "PR phase", "Merged-unclosed").

## Merging a PR

Sole statement of who merges. A sprint has exactly one PR; its closure never opens another. The main orchestrator merges the sprint PR itself — `gh pr merge --squash`, after checks pass and the PR is mergeable; it never waits for a human to click merge.

The merge completes the sprint; no user gate follows it. It writes nothing on `git.base_branch`, so the sprint stays at its active path there, merged-unclosed, until the next sprint's scope archives it mechanically: terminal write and archive move as that branch's first commit (`sprint-lifecycle.md` "PR phase", "Closure write"). The self-hosting release follows the merge ("Versioning & Changelog (self-hosting only)").

Legacy recovery: an open `chore/finalize-sprint-<NNN-slug>` PR left by an earlier workflow version, found via `gh pr list --head chore/finalize-sprint-<NNN-slug>`, is merged as already approved (that flow opened it only after the user approved closing the sprint), and its closure write is skipped.

A merge blocked by a failing check, a conflict or a branch-protection rule is reported, not forced: never `--admin`, never a local merge pushed to `git.base_branch`.

## Pre-existing uncommitted changes

If working tree is dirty at `/asd-sprint` start, the main orchestrator stops and asks user to commit or stash before sprint creation. No silent stashing.

## Versioning & Changelog (self-hosting only)

Applies only when `self_hosting: enabled` (`sprint-lifecycle.md` "Self-hosting") — a consumer project's own app version is unrelated to ASD's `asd_version`.

`pr` phase, open mode, before composing the PR: bump `asd_version` in `.asd/release-manifest.json` per [SemVer](https://semver.org/), inferred from the sprint's Conventional Commit types (highest wins): `fix`→PATCH, `feat`→MINOR, `!`/`BREAKING CHANGE` footer→MAJOR. Add a matching `## v<version>` section to root `CHANGELOG.md` (newest first, English), grouped `Added|Changed|Deprecated|Removed|Fixed|Security`, describing consumer-facing impact — not implementation detail. Under `backward_compat: migration`, this bump is also the blocking DoD check that `max(.asd/migrations/*.js filename version) <= asd_version` — a migration a consumer never reaches because the version bump does not cover it fails the bump, not just the migration.

In `pr` merge mode, once the merge is confirmed (`sprint-lifecycle.md` "PR phase"): `git fetch origin <git.base_branch>` (the squash merge commit is not local on the sprint branch), then find the release commit: the sprint PR's merge commit, `mergeCommit` from `gh pr view <pr.number> --json mergeCommit`, when it carries this sprint's bump — its `asd_version` (`git show <merge_commit>:.asd/release-manifest.json`) above its parent's (`git show <merge_commit>^:.asd/release-manifest.json`) and a `## v<asd_version>` heading in its `CHANGELOG.md`. A merge commit without them (a PR merged before open mode's bump) yields to the first later commit of `git log --reverse --format=%H <merge_commit>..origin/<git.base_branch> -- .asd/release-manifest.json` passing the same check, a follow-up PR's merge; none is `FAILED` naming the recovery "merge a follow-up PR bumping `asd_version` with its CHANGELOG section, then re-run". Read `asd_version` at the release commit. Each step is skipped when already done: create annotated tag `v<asd_version>` on that commit unless it exists locally (`git rev-parse -q --verify refs/tags/v<asd_version>`) or on `origin` (`git ls-remote --tags origin v<asd_version>`), and `git push origin v<asd_version>` unless it exists on `origin`; then `gh release create v<asd_version> --verify-tag --title v<asd_version> --notes-file <extracted CHANGELOG section>` (notes file under `.asd/tmp/`, `artifact-layout.md` "Scratch directory"), unless `gh release view v<asd_version>` succeeds. The tag and release are a tag ref and a host object, not a branch write.

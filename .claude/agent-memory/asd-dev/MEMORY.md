- [AGENTS.md is an ordinary managed block](project_agents-md-sync-state-drift.md) — since sprint 006 the orchestrator re-baselines it normally; framework-dev prose lives below the end marker.
- [ASD framework repo self-hosts](project_asd-self-hosting.md) — this repo IS the ASD framework, not a consumer; sprint work edits `.asd/` canon directly.
- [Mirror-heavy repo, strict file ownership per wave](feedback_asd-mirror-ownership.md) — in multi-wave review-fix tasks, only touch assigned files even when a stale reference lives in an unowned file.
- [antd button state specificity](feedback_antd-button-state-specificity.md) — override --ant-btn-* vars, not selectors; verify computed styles.
- [Scripted edits on Windows](feedback_windows-scripted-edits.md) — Python text mode writes CRLF; use newline='' and the Edit tool for Cyrillic anchors.

- [No screenshots, keep user browser](feedback_no-screenshots-keep-user-browser.md) — no headless renders; never kill chrome/msedge.
- [Measure perf on a temp database](project_perf-measure-temp-db.md) — own temp DB inside the dev Postgres container, own API on a spare port, stop by PID, drop by name.
- [Chained saves and form remount](feedback_chained-save-form-remount.md) — version-keyed forms drop field errors after save-then-act; key by a reread counter.
- [Hosted services and validated options](feedback_hosted-service-ctor-options.md) — no IOptions.Value in a hosted service ctor chain; OpenAPI build constructs it.

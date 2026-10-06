---
name: antd-button-state-specificity
description: antd 6 button hover/active rules are (0,4,0); override --ant-btn-* variables, not state selectors, and verify via computed styles
metadata:
  type: feedback
---

antd 6 paints button hover/active via `:where(.css-hash).ant-btn:not(:disabled):not(.ant-btn-disabled):hover` (0,4,0) reading `--ant-btn-text-color-hover` / `--ant-btn-bg-color-hover` (+ `-active`), which are set on `.ant-btn.ant-btn-color-default.ant-btn-variant-text`.

**Why:** a `.cls.ant-btn:hover` (0,3,0) override silently lost; my contrast numbers were for colours never applied.
**How to apply:** override the variables on `.cls.ant-btn.ant-btn-color-default.ant-btn-variant-text` (0,4,0, wins on order); never use `!important`. Verify with getComputedStyle in headless Chrome + CDP `CSS.forcePseudoState`, not on paper. On this Windows box ports 5235-5893 are excluded (EACCES); use e.g. `--host 127.0.0.1 --port 5900`.

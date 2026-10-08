---
responsibility:
  owns: project-owner custom rules read by all agents in all phases
  excludes: phase-specific rules (design-only, coding-only)
  delegates_to: custom-design-rules.md (design/design-review), custom-coding-rules.md (impl/impl-review), .asd/rules/ (workflow rules)
---

# Custom Common Rules

Universal project rules read by every ASD agent in every phase. Put here: domain glossary, naming conventions, compliance requirements, project-wide vocabulary, anything holding across design AND code.

Phase-scoped rules go elsewhere — design constraints to `custom-design-rules.md`, code/test constraints to `custom-coding-rules.md`. ASD never overwrites this file.

## Docker safety

- Agents never run destructive Docker commands (`down -v`, `volume rm`, `volume prune`) against the user's dev stack or volumes with a fixed name. Temporary runs use only their own named volumes and delete them by exact name.

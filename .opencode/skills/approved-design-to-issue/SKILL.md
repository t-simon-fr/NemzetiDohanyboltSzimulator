---
name: Approved design to issue
description: Készíts jóváhagyott briefből GitHub issue-t a feature-sablonnal (megfigyelhető eredmény, függőségek, 3-6 feltétel)
---

Use when a design brief was approved (for example via `brainstorm`) and the next step is a tracked issue. Do not implement anything in this skill.

## Steps

1. Re-read the approved brief; do not expand scope.
2. Create the issue with the `github` MCP `issue_write` (method `create`), Hungarian body following [.github/ISSUE_TEMPLATE/feature.yml](../../../.github/ISSUE_TEMPLATE/feature.yml):
   - Megfigyelhető eredmény (player-visible, one section)
   - Függőségek
   - 3-6 ellenőrizhető elfogadási feltétel
   - Kizárások
   - Célzott ellenőrzés (type decides the checks: [Docs/WORKFLOW.md](../../../Docs/WORKFLOW.md) §2.2)
3. Return the issue number and URL. Implementation starts only on explicit user approval (an approved batch needs no further prompt, per WORKFLOW.md §2).

Standing rules: [AGENTS.md](../../../AGENTS.md) (Hungarian chat, English code and commits, one issue per independently reviewable feature).

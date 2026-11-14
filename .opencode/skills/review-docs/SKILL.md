---
name: Review docs
description: Markdown-linkellenőrzés változott fájlokon a Review-Docs.ps1-gyel — hibák osztályozása és javítása
---

Use when Markdown files changed (docs, workflow, STATUS, reports) or before opening a docs PR.

## Steps

1. Run from the repo root on the changed files only (note: `pwsh -File` does not bind file arrays — use direct invocation):
   `& ./Tools/Review-Docs.ps1 -Path @(<files>)` (repo-relative paths).
2. Classify each failure: missing target, escaping the repo root (`..` above root), or wrong anchor. Implementation: [Tools/Review-Docs.ps1](../../../Tools/Review-Docs.ps1) via [Tools/Workflow.Common.psm1](../../../Tools/Workflow.Common.psm1).
3. Fix the links (usually the text, not the target) and re-run until green. No Unity command is involved.
4. Documentation-only changes need just diff plus this link check — no Unity or WebGL build (WORKFLOW.md §2.1).

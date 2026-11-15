---
name: Verify project
description: Unity-ellenőrzés futtatása és értelmezése — suite/profil választás, report.json, sourceFingerprint, evidence-rögzítés
---

Use when gameplay or runtime code changed and [Docs/WORKFLOW.md](../../../Docs/WORKFLOW.md) §2.2 requires compilation plus targeted suites.

## Steps

1. Choose scope first and state it (type-to-checks matrix: WORKFLOW.md §2.2). Inspect the plan without Unity:
   `& ./Tools/Verify-Project.ps1 -Suite @('<id1>','<id2>') -PlanOnly`
   or `-Profile smoke|checkout` (registry: [Tools/Verification/suites.json](../../../Tools/Verification/suites.json)).
   NOTE: `pwsh -File` does not bind arrays to `-Suite`; always invoke via `&` with `@(...)` (same for `Review-Docs.ps1 -Path`).
2. Run the selected suites (needs Unity; a single owner controls Play/build — see WORKFLOW.md §3):
   `& ./Tools/Verify-Project.ps1 -Suite @('<ids>')`
3. Read the fresh `Builds/Verification/<timestamp>/report.json`: compilation, per-suite and total timings, HEAD plus fingerprint. Never relabel an old run as a fresh pass; carry unaffected checks only with baseline, changed paths, and fresh proof.
4. Record evidence (suite/profile, report path, fingerprint, result) into the issue or PR. Prior evidence is invalidated when sources changed.

Do not run unrelated suites repeatedly. Do not commit `Builds/`, `Library/`, or local reports.

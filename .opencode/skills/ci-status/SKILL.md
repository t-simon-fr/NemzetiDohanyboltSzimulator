---
name: CI status
description: CI-futások lekérdezése és értelmezése az Actions API-n (runs, jobs) — PR-kapcsolat, piros job teendők
---

Use when a PR is open (or just merged to main) and you need the live CI verdict. Note: the `check-runs` REST endpoint rejects this repo's fine-grained PAT (403 — no "Checks" permission exists for it), so always use the Actions API instead.

## Steps

1. Latest run for a branch:
   `GET /repos/t-simon-fr/NemzetiDohanyboltSzimulator/actions/runs?branch=<branch>&per_page=1`
   Note `status` (`queued`/`in_progress`/`completed`) and `conclusion`.
2. Jobs of a run:
   `GET .../actions/runs/<run_id>/jobs` — per-job `name`, `status`, `conclusion`.
   Expected jobs from [.github/workflows/ci.yml](../../../.github/workflows/ci.yml): `Tooling self-check (no Unity)`, `Docs link check (changed Markdown, PR only)`.
3. Green only if every job is `completed` plus `success`. (A skipped `if:` still reports success; a PR without Markdown changes is fine.)
4. On red: read the failing step, fix in a new commit on the PR branch. Never merge red — `main` branch protection enforces both jobs with `strict: true`.
5. Report run and job URLs as evidence in the PR.

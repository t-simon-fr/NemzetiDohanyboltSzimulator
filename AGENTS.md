# Repository working agreement

Read `Docs/WORKFLOW.md` before implementation and `Docs/Workflow/STATUS.md` when resuming. GitHub issues are the task source of truth; `Docs/CONTINUE.md` contains historical notes.

- Communicate in Hungarian; code, identifiers and commit messages stay English.
- One issue per independently reviewable feature or bug. Record observable acceptance criteria, dependencies and exclusions before editing. A user-approved batch authorizes its implementation without another approval prompt.
- Implement one coherent slice, inspect its diff, run the relevant `Tools/Verify-Project.ps1` profile, then commit it separately. Do not bundle unrelated features. Use a short feature branch/PR for new feature work; keep review focused on that issue. Do not merge or publish unless authorized.
- Use `Tools/Invoke-Pipeline.ps1` for Unity. Compilation must complete before entering Play; top-level transport success alone is not test success. Never reuse a previous run as evidence for changed source.
- Add meaningful coverage for state, inventory and money invariants. Do not write tests that only mirror implementation or repeatedly run unrelated suites. Register maintained suites in `Tools/Verification/suites.json`; historical checks are not automatically current.
- For visible changes complete `Docs/Workflow/VISUAL_REVIEW.md`: capture actual Game view including GUI, inspect the images and exercise the changed interaction. Screenshots alone are not a visual pass. Record reviewer, observations, resolution, source fingerprint and evidence paths.
- Track implemented, Unity verified, visual verified, WebGL verified and published separately. Issue checkboxes must describe the achieved stage. A successful Editor test is not a WebGL or deployment result.
- Pages publication requires explicit authorization. Current session instruction: source-only work; no Pages deployment. Door/sign day flow (#13) has shipped. The current user request authorizes layout and arrangement implementation (#15, slices #16–#18), without merging or Pages deployment. The current follow-up also authorizes the door-mounted outward sign (#21) and under-counter contraband storage (#20).
- Preserve unrelated user files (`Assets/_Recovery/`, `Assets/_Recovery.meta`, `Data/`). Never regenerate the scene or restore user edits to get a clean diff.
- Keep production code readable; do not compress new classes/methods into long single lines. Never commit temporary scripts, tokens, Library, Builds or local reports. Repository tools must resolve paths from `$PSScriptRoot`, not a machine-specific drive.

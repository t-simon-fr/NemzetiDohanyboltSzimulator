# #35 — Official Hungarian forint surfaces

Implemented on source commit `cfba5ec`: all six renewed banknotes and all six circulating coin denominations use the MNB originals, with front/back surfaces. The drawer, selected change tray and customer palm share cached materials and meshes. Source images and existing MINTA marks are intact; [sources, hashes and digital-use terms](../../Assets/FORINT.md) are recorded. The in-game H guide includes attribution.

## Verification

- Fresh compile and checkout: **448 assertions**, 18 suites; **59** targeted forint assertions. [Unity report](unity.json).
- Source fingerprint: `58a086f3496cba877d640b3af61679d8f2dc127d8ac9984cb69ae2abd09300d7`. The test ran before the source commit; the same fingerprint was confirmed after committing. Scope includes the pre-existing local user rendering settings, which were preserved and excluded from this feature commit.
- Native actual Game view: `Builds/Verification/visual-20261104-204241-306/current.png`; physical raycasts selected/deselected all six palm denominations, exact payment conserved each individual drawer count and rejected double settlement.
- Local WebGL build: `2026-11-04T20:48:44.297Z`, 23178076 bytes, 3 warnings, 0 errors; build hashes in the reports.
- Actual browser mouse/keyboard: **15 cash-register checks** + **12 offered-coin checks**, 0 console errors. [Cash](cash-register.json), [coins](forint-coins.json). Local raw reports: `Builds/Verification/acceptance-1793825480943/report.json`, `Builds/Verification/acceptance-1793825530502/report.json`.
- Original-image SHA-256 verification: all 18 downloaded files match the provenance manifest.

## Visual / interaction review

Reviewer: Codex, 2026-11-04. Actual Game view and local WebGL player screenshots inspected; browser exercised denomination clicks, undo, clear, exact selection, approval, duplicate rejection and camera restoration. Browser captures cover **1280×720 and 1920×1080**. Notes retain their proportions and actual designs; coins retain numeric and motif faces with readable lettering; selected change uses the same real imagery. Native review found mirrored coin UVs; corrected before the final 448-assertion run and WebGL build, with regression checks for unmirrored mapping. Low-resolution official coin illustrations remain deliberately unchanged. Existing unrelated cash-HUD limitation: after an intentionally rejected payment, the earlier error sentence can remain visible while the selected amount is corrected; the register display and actual settlement are correct. This texture change does not modify that feedback flow.

Result: **passed**. Source-only change; **no GitHub Pages publication**.

![Official forint drawer and selected change](1280x720-cash-register-selected.png)

![Official forint coins in the customer palm](1280x720-forint-coins-ready.png)

![Large register view](1920x1080-cash-register-selected-large.png)

![Large palm selection](1920x1080-forint-coins-selected-large.png)

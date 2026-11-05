# Wall seam and counter mats — #39, #40

The starter divider wall is continuous across its previous 10 cm opening, with collision at the join. Its rear boundary stays fixed; expansion removes both divider sections and a new game restores them. The gray customer-product mat is centered on its existing placement slot and no longer overlaps the blue handover tray or countertop edges. The user withdrew the L-corner concern; no L-corner change was made.

- Tested runtime source: `3dfa6c480166ba8f34002806860fae1e58ee5619`; wall commit `fe539e7`.
- Source fingerprint: `c4c710c2c8debc2b9ce72d61bcb72487370fb568467d57ef11934aeea988e948`. Recomputed after the build and unchanged from the native verification. Pre-existing local rendering overrides were preserved and included in verification/build, excluded from the feature commits. No authored scene was saved or regenerated.
- Unity checkout: **498 passed assertions / 21 suites**, including 18 new wall checks and 11 new mat checks, compilation passed. [Machine report](unity.json). The report's HEAD is the wall commit with the mat changes then uncommitted; its content fingerprint exactly matches the final tested runtime source.
- Actual player input: **E and left mouse button each placed a held product on the visible gray mat**, using queued Input System device states through the normal player Update path; cash remained 6000 Ft. The fixture provided the item and initial player position. Native checks also cover retrieval/cancellation, physical targeting, multiple identical deliveries, expansion/reset and inventory/money invariants. [Interaction review](interaction.json).
- Shipping WebGL: **0 errors, 3 warnings**, 23,157,544 bytes, 280.8 seconds. [Build result](shipping-build.json). No SHOP_ACCEPTANCE define; decompressed code/data scanned for acceptance-probe class and state markers. [Release metadata and SHA-256 manifest](release.json).
- Local shipping browser: actual menu Start click, pause/resume, B opens TobaccOS; **1280×720 and 1920×1080**, no console/runtime/HTTP errors. [Report](local-player.json).

Reviewer: Codex, 2026-11-05. Inspected the actual native Game view (1918×911), top view, wall close view and both shipping-browser sizes. Gray/blue footprints are separated, labels remain on their intended surfaces, and the starter wall has no vertical opening. Interaction and visual result: **passed**.

Published at the user's explicit request: **[Play the live release](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=3dfa6c4)**.

- PRs [#41](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/41) and [#42](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/42) merged in dependency order; main merge `37df35d1ddc68ee235dca3861dd0ca89d1230015` has the exact tested feature-branch tree. Runtime content is unchanged from `3dfa6c4`.
- Pages commit: `1770ce140d15715bb096007c36dcd8c92402ef20`, `gh-pages` in the WebGL repository; GitHub Pages build confirmed built.
- Live shipping browser: Start by mouse, pause/resume and TobaccOS by keyboard, 1280×720 and 1920×1080, **0 console/runtime/HTTP errors**. [Live report](live-player.json). Captures at both sizes were inspected: the gray and blue mats remain separate and the wall renders continuously.
- Served HTML, loader, framework, data and WASM each match the local shipping release byte-for-byte by **SHA-256**. [Live asset verification](live-assets.json). Versioned build URLs use `?v=3dfa6c4`.

Final result: **published and live-verified**. These visual fixes received fresh native and shipping-player checks; the previous release's full acceptance-player suite is historical and is not claimed as a new run.

![Counter top with separate mats](native-counter-top.png)

![Continuous starter wall](native-wall-seam.png)

![Local shipping player](local-1280x720-started.png)


![Live player](live-1280x720-started.png)

![Large live view](live-1920x1080-world.png)

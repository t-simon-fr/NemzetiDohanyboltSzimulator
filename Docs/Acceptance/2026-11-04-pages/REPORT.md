# All merged shop improvements — Pages release

Published at the user's explicit request on 2026-11-04: **[Play the live release](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=fa37203)**.

- Runtime source: `fa372031d3a53d4475fd7d6bd8cc00d1657546c7` (main, through #37).
- Pages commit: `a5b74842edd6a35f7e6b106af89aafcf103214b6`, branch `gh-pages` in `NemzetiDohanyboltSzimulator-WebGL`; GitHub Pages build confirmed built. Initial binary publish: `bab9cbd`; the final commit only aligns manifest text hashes with Git-normalized line endings.
- Source fingerprint: `0f778f96a60ededb4f9b2cc9028733d1594f23b4e3f6d2ad2e1541bd640bb5b2`. This exactly matches the [469-assertion Unity verification](../2026-11-04-customer-hand/unity.json). Pre-existing local rendering overrides were preserved and included in the verified build, not committed as feature changes.

Includes all completed merged runtime work: left-side counter and shop arrangement, starter-door access, door sign/day flow, private under-counter stock, updated UI, sprint/jump, physical card terminal, coffee display, inventory-driven shelf labels, original register model/close view, official forint imagery and player-side clickable coin hand. Unfinished character concepts (#11) remain outside this release.

## Verification

- Shipping build through `ShopWebGLBuild.Build`: **0 errors, 3 warnings**, 23154955 bytes, 545.9 seconds. [Build result](shipping-build.json).
- Shipping player has **no acceptance probe**; the build uses no SHOP_ACCEPTANCE define, and decompressed player data/code were scanned for probe class/state markers. [Release manifest and hashes](release.json).
- **156 successful browser acceptance steps**, 0 console errors, covering cash/coins, physical card keys, repeated goods, actual pickup/place, tools, all coffee recipes and shortages, pause/settings/rebinding/persistence, dialogue, UI, arrangement, door/sign/private storage and next-day flow. [Coverage](browser-coverage.json). The legacy pickup fixture initially aimed below the pack after controller settling; the same unchanged player passed actual mouse-aimed and E interactions with the [release-only runner adjustment](mouse-aim.patch). Completed cases were not needlessly repeated.
- Local shipping player: menu Start by mouse, pause/resume, TobaccOS by B, both viewport sizes, 0 runtime/HTTP/console errors. [Report](local-player.json).
- **Live Pages**: same real browser interactions, 0 runtime/HTTP/console errors. [Report](live-player.json). The served HTML, loader, framework, data and WASM all match the normalized local release **byte-for-byte by SHA-256**. [Live hash verification](live-assets.json).
- Build URLs carry `?v=fa37203` so previous cached binary files do not hide the update.

Reviewer: Codex. Actual shipping-player captures were inspected at **1280×720 and 1920×1080**. New register and room geometry render, menu starts the game, pause resumes correctly and the live computer opens. Result: **published and live-verified**.

![Live player](live-1280x720-started.png)

![Live TobaccOS](live-1280x720-computer.png)

![Large live view](live-1920x1080-world.png)

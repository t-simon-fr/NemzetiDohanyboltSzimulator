# Free warehouse carton placement — Pages publication

Published at the user's explicit request on 2026-11-05: **[Play the live release](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=ec97eb9)**.

- Source: `ec97eb9ed1d514b983c4f99b3f39e80929c477a6` (main, merged #44 / issue #43). The compiled runtime matches the already locally verified `42b7d95` source; subsequent changes were the browser runner and evidence/documentation.
- Publish: `9e8db97de1656675b5a637be58f3c44cdb48320d`, `gh-pages` in `NemzetiDohanyboltSzimulator-WebGL`; GitHub Pages reports this exact commit built.
- Source fingerprint including preserved existing local rendering overrides: `35cc4fda50b0b7846efe21432be0fa1e1a402a711c807e6e3e0e6422b19ee035`. [Source record](source.json).
- Shipping build through `ShopWebGLBuild.Build`, without the acceptance define/probe: **0 errors, 4 warnings**, 23,160,986 bytes, 447.4 seconds. [Build result](shipping-build.json).
- Versioned asset URLs use `?v=ec97eb9`. Text files were normalized to LF before packaging and the release manifest records SHA-256 hashes. [Release metadata](release.json).

This release adds free placement of warehouse cartons on all 24 physical storage places, inventory-driven labels, physical pickup and unit returns, occupied-place protection and safe delivery/cancellation handling. All earlier published improvements are retained.

**Verification policy:** the user explicitly requested relying on the completed local checks. No new native tests, browser smoke tests, live interaction tests or served-file checksum comparison were run for this publication. Existing evidence is **536 native assertions / 22 suites and 14 local WebGL input checks, 0 browser errors**, from the [implementation report](../2026-11-05-warehouse-storage/REPORT.md). The publishing branch push and GitHub Pages deployment status were confirmed; this is not a claim of a new live gameplay test.

Result: **published**, using the previously completed local verification.

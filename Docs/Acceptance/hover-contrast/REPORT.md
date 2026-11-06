# TobaccOS hover contrast — #92

Hovering a label on the cream workspace inherited a near-white Unity skin color. All eight label states now explicitly use brown on light surfaces and cream on dark surfaces. Button hover text also explicitly follows its palette. Layout, input handling and business behavior are unchanged.

Implementation: 359bbd54eb38746079474b35d3b28677800a0002. [Source identity](source.json) matches the fingerprint in the native report; the native run preceded the implementation commit. Existing unrelated local settings/material changes were preserved.

- [Native report](unity.json): compilation passed; 12 targeted computer-ui assertions passed.
- [Fresh WebGL build](webgl-build.json): successful, zero errors, three warnings.
- [Browser report](browser.json): four checks passed, no console errors. Actual mouse input opened the app and hovered labels/buttons without purchasing or changing application state.
- Visual inspection passed at 1280×720 and 1920×1080: dark text stays readable on cream; cream header text stays readable on green. Colors were reviewed in screenshots, not asserted by pixel tests.

[Native upgrade screen](native-upgrades.png), [720p label hover](1280x720-hover-label.png), [720p button hover](1280x720-hover-button.png), [1080p label hover](1920x1080-hover-label-large.png), [1080p header hover](1920x1080-hover-header-large.png).

This was a targeted verification pass. The broader gameplay evidence remains in the [preceding visual refresh report](../2026-11-05-trafik-refresh/REPORT.md); it was not rerun for this color fix. No Pages deployment was performed.

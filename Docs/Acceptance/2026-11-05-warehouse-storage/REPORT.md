# Physical warehouse carton storage — #43

Cartons can be placed on any empty warehouse storage place, regardless of the former brand assignment. Runtime targets cover **24 physical places**, including previously unassigned shelf rows. Carton pickup, labels and individual-product returns use the physically present carton. Occupied places reject replacement, including empty cardboard whose product label is blank. New deliveries choose available space; cancellation returns to the actual pickup place, using the existing loose-carton area if that place has become occupied. Multiple same-brand cartons retain their own quantities when a unit is returned to a specific carton.

- Runtime source: `42b7d95`, following `cdb524a`; PR [#44](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/44).
- Native/build source fingerprint: `68e676abfa643bd31ffab0f7a522f7d155ec174b91dad37b5f540d1113f9db12`. The native report records the earlier HEAD with runtime changes then uncommitted; the built runtime commit has those same contents. After the build only the browser runner changed to send pointer-locked clicks without a cursor-move event; compiled Assets/Packages/ProjectSettings remain unchanged. Existing local rendering overrides were preserved and included in verification/build, excluded from feature commits. No scene regeneration or scene save.
- Native checkout: **536 passed assertions / 22 suites**, including **38 warehouse checks** for arbitrary placement, labels, occupied places, physical targeting, partial/empty cartons, exact money/stock, new deliveries, same-brand carton returns, cancellation, next day and reset. [Native report](unity.json).
- Actual native Input System E: pickup and placement on the lower row. Actual left mouse button: pickup from lower row and placement on another original brand's dock. The player stands in the existing aisle. Actual Game view including GUI inspected at **1918×911**; blank labels, moved label, carton visibility and consistent label plates confirmed.
- Fresh local acceptance WebGL: **0 errors, 3 warnings**, 23,185,395 bytes, 246.4 seconds. [Build result](webgl-build.json), [source and build-file hashes](webgl-source.json). This is a local test player containing the acceptance probe.
- **14 successful browser checks**, **0 console/runtime errors**: actual menu Start, E pickup/cross-brand placement, vacant source cannot remotely pick up goods, mouse pickup, occupied-place rejection, E lower-row placement, mouse lower-row retrieval, exact one-unit stocking, mouse placement of part-filled carton and individual-unit return to its relocated carton. Cash and the other brand's carton remain unchanged. **1280×720 and 1920×1080** captures inspected. [Browser report](browser.json).

Reviewer: Codex, 2026-11-05. Result: **implemented, native/interaction/visual verified, local WebGL verified**. Fixtures provide purchased stock and initial player aim; the browser executes real keyboard/mouse actions. The first fixture attempt overlapped the neighboring rack and aimed through the label plate; the final fixture uses the actual aisle and visible front of the chosen row. The final successful run uses the source identified above.

**Not published to Pages**: this request authorized the repair; the live Pages release remains `1770ce1` / runtime `3dfa6c4`.

![Lower shelf after actual E placement](native-lower.png)

![Relocated carton after mouse placement](native-relocated.png)

![Local WebGL lower-row placement](1920x1080-warehouse-lower-relocated.png)

![Local WebGL returned goods](1280x720-warehouse-returned.png)

# Terminal auto-close, after-hours upgrades and warehouse ingredients — #47–#49

- **#47 / PR #50:** successful card payment restores the original player camera/lens and normal locked-cursor input. Incorrect/empty amounts keep the terminal open. The successful amount/status remains on the physical display, the triggering event is consumed and repeated confirmation cannot charge again.
- **#48 / PR #51:** expansion, extra shelf, coffee machine, fridge and panic button are purchasable before opening or after closing in the empty shop, until the player leaves and finalizes the day. Open/occupied/settled-day restrictions, exact costs, one-time ownership and next-day persistence remain enforced.
- **#49 / PR #52:** a warehouse storage selection trigger no longer intercepts a visible ingredient package. The nearest actual solid surface must be that tool, so genuine walls, shelf boards or cartons still block pickup. Milk and coffee have distinct tooltips; normal empty-cell carton placement remains intact.

## Shared verification

- Runtime source: `6b4d053`, with individual commits `2c86c2b` (#47), `da281e9` (#48). Source fingerprint **44b47ee93b55845a52d5337298c6e5d8258d69fe09e864db1f4dc7a2a6673ffe**. The native report's HEAD is the preceding branch with final ingredient changes then uncommitted; its contents match the final build. Existing local rendering overrides were preserved and included in verification/build, excluded from commits; no scene regeneration or scene edits.
- **621 passed assertions / 25 suites**, including **32 physical-terminal**, **27 after-hours-upgrade** and **21 ingredient-pickup** checks. [Native report](unity.json). Coverage includes real solid occlusion, empty-cell carton return, paid ingredient conservation, exact development prices/drawer totals, duplicate rejection, departing customers, finalized day and next-day persistence.
- Actual native Input System **E** picked up coffee and refilled it; actual **left mouse** picked up milk, then E refilled it. Result: **10 coffee / 10 milk**, no extra payment. Actual keypad input **2200 + Enter** paid one real card sale and returned to normal world view. Game view including GUI inspected at **1918×911**. Camera restoration and input-event consumption also checked by the maintained terminal suite.
- **One combined final local WebGL build**, 0 errors, 3 warnings, 23,183,475 bytes, 370.3 seconds. [Build result](webgl-build.json), [source/build hashes](webgl-source.json). Local acceptance player includes the probe; it is not a shipping player.
- **32 successful browser checks, 0 console/runtime errors**. Actual E/mouse ingredient pickup and E refill; actual computer clicks purchase all five developments after closing and reject duplicate billing; actual physical terminal buttons correct invalid input and settle once, automatically returning focus/input to the world. **1280×720 and 1920×1080** captures inspected. [Browser report](browser.json). Fixtures buy stock/set starting conditions and position the player; the browser carries out real inputs.

Reviewer: Codex, 2026-11-05. Result: **implemented, native/interaction/visual and local WebGL verified**. Pages publication has not been requested for this batch. The issue reported on the live site is reproduced by overlapping the real package and warehouse selection trigger in this local player.

After the build, the browser runner's ingredient assertions were corrected to compare the held-tool enum (`CoffeeRefill`) rather than the tooltip text. The first attempt had already picked the package but tested the wrong snapshot field. No runtime source changed, no second build was needed, and the final complete 32-check run passed.

![Coffee pickup prompt](1280x720-ingredient-coffee-target.png)

![Machine after real coffee and milk refills](1920x1080-ingredient-machine-refilled-large.png)

![After-hours developments purchased through the computer](1920x1080-after-hours-upgrades-owned-large.png)

![World view after successful card payment](1280x720-physical-terminal-approved.png)

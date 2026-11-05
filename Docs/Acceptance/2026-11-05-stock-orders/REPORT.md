# Orders from on-hand assortment, without queue reservations — #45

New customers select only goods that physically exist when they arrive: shop stock (including unpaid items in waiting baskets), arrived warehouse cartons, prepared/brewing cups or coffee recipes supported by delivered ingredients. Pending shipments, an expansion alone or an empty coffee machine do not create demand. Preferred order patterns remain where possible, and quantities within each basket share a finite planning budget.

**User's clarification:** waiting orders do not reserve stock. Several customers may request the last unit, and the product may run out before a queued customer reaches checkout. Existing shortage/substitution/refusal behavior remains for those already placed orders. This is deliberately different from reserving items for every waiting customer. When all stock is gone, no impossible new customer order is created; normal replenishment and manual closing remain available.

- Runtime source: `d0090d7`, PR [#46](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/46). Source fingerprint: `970020b2db97c8d21a93ed676535d9c6c8401a9d5d6766186ee0d3135d227c9c`. The native report records the previous HEAD with these changes then uncommitted; its content fingerprint matches the built source. Pre-existing local rendering overrides preserved and included in verification/build, excluded from feature commits. No scene edits/regeneration.
- **570 passed native assertions / 23 suites**, including **34 new stock-order checks**. [Report](unity.json). Covers never-bought energy drinks, expansion without actual goods, arrived/pending supplies, exact repeated-unit quantities, shared coffee/milk ingredients and prepared/brewing final cups. A real last-unit card sale between two queued customers verifies that the second customer's existing request can become unavailable. Planning itself does not change inventory/cash; an empty shop creates no artificial missed sale and remains closable.
- Actual native **E** delivers a real paid energy drink to a generated order in an otherwise empty expanded shop. State becomes Payment, order 1/1 delivered, ledger unchanged before payment. Actual Game view including GUI inspected at **1918×911**. [Capture](native-delivery.png).
- **One final local WebGL build**, 0 errors, 3 warnings, 23,185,350 bytes, 457.0 seconds. [Build result](webgl-build.json), [source/build hashes](webgl-source.json). This local test player includes the acceptance probe.
- **6 successful browser checks, 0 console/runtime errors**, actual menu Start and actual E delivery; generated order contains only the genuinely purchased energy drink, warehouse-only stock shows no false shortage, real product transfer starts payment without booking a sale. Inspected **1280×720 and 1920×1080**. [Browser report](browser.json). Fixture buys stock through normal APIs and positions the player; it does not fabricate the customer's order.

Reviewer: Codex, 2026-11-05. Result: **implemented, Unity/interaction/visual and local WebGL verified**. Pages publication is separate and has not been requested for this change.

![Warehouse-stock order](1280x720-stock-order-warehouse.png)

![Actual E delivery](1920x1080-stock-order-delivered-large.png)

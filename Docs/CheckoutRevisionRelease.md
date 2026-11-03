# Checkout and controls revision

Implemented the seven open items in issue #1 on 2026-11-03.

- Card checkout requires the cashier to type the exact total. Digits and numpad digits enter amounts, Backspace corrects, Delete clears, Enter confirms. Empty/wrong totals and repeat confirmation do not charge.
- Cash and card revenue share one spendable balance, represented by physical cash in the drawer. The legacy opening-bank constructor argument is merged once; BankBalance remains zero for compatibility. Purchases, upgrades and day rollover use the combined cash value. CardRevenue remains a reporting metric.
- Two counter zones: blue player intake and a single customer mat containing three separated item positions. Player deliveries move into the customer group. The layout avoids monitor collisions. Missing item counts account for repeated SKUs.
- Left click performs ordinary interaction/pickup/placement alongside the configured interaction key. Bat and mop retain their tool action.
- At the coffee machine, a visible recipe hint lists short, long and latte: 1/2/3 selects, Q cycles, E or click operates. The monitor retains its recipe buttons.
- Startup main menu and Escape pause menu provide start/resume, new game, settings and quit. Settings persist gameplay keyboard bindings in PlayerPrefs and swap conflicting bindings; reset restores defaults. Terminal/menu keys and digits remain reserved. Opening a menu pauses the clock and blocks world input.
- Customers request their basket in one sentence. T opens dialogue. A shortage exposes a substitution response; a suitable stocked, affordable alternative may be accepted or the customer leaves. Cancellation remains available.

## Verification

Unity compilation completed successfully. CheckoutRevisionChecks.Run passed 20 assertions covering merged funds, physical drawer reconciliation, exact card entry and no double charge, recipe selection, pause/resume and a real stock-shortage substitution. NextFeatureChecks.BasketDay passed 41 assertions over an actual eight-customer shift, including five multi-item baskets. RecoveryAndCancellation passed 9 assertions covering NPC public-shelf goods, removal/re-delivery and cancellation without money or stock loss.

Total: 70 passing assertions. No WebGL build or Pages deployment was performed for this revision. The planned physical shop-sign/door day flow and hidden contraband redesign remain separate work.

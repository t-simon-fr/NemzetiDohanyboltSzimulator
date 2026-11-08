# #111 – Tea and hot chocolate

- Outcome: five readable recipes on the existing machine; tea and cocoa physically delivered/refilled, prepared cups handed to the correct customer.
- Scope: one tea (1 tea, 450 Ft) and one hot chocolate (1 cocoa + 1 milk, 900 Ft), existing ownership, busy/output/focus and checkout. Ten-serving ingredient orders: tea 1200 Ft, cocoa 1000 Ft; existing milk 1500 Ft. No new modal/hotkey or shop service.
- Files/invariants: catalog append-only ids, recipe configuration and pure machine state; existing ingredient package/delivery/refill, machine/PC display, stock-aware orders, shortage checks, prices and reputation availability. No duplicate debit, consumption, output or lost inventory on blocked output; old SKUs/coffee behavior unchanged. User files preserved.
- Checks: new hot-drinks state/integration suite; coffee-world, coffee-display, refactor-coffee, ingredient-pickup, ingredient-packaging, stock-aware-orders, reputation-integration, checkout-revision and ledger. One combined checkout profile when stable; native Game view and actual input, local WebGL hot-drinks and coffee regression at 720p/1080p.
- Done: acceptance criteria, relevant inventory/money checks, real interaction and reviewed images passed, PR merged, issue reconciled. Pages requires separate request.

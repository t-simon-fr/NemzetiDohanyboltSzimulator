# Workflow-bevezetés ellenőrzése — 2026-11-03

Kapcsolódó feladat: #9. A játékfunkciók előző commitja: dee3417932351619f0adf61acdb455432dfcfa53. A workflow munkapéldányban készült, ezért a tesztjelentés ezt HEAD-ként és dirty=true értékkel tartalmazza; a vizsgált tartalmat a sourceFingerprint azonosítja.

## Eszközök

`Tools/Checks/WorkflowRunnerChecks.ps1`: 7 sikeres ellenőrzés. A transporthiba, a külső success=true mögötti fordítási és futási hiba, valamint a hiányzó státusz elutasítása; sikeres eredmény megőrzése; PowerShell-szintaxis és suite-regiszter.

`Tools/Verify-Project.ps1 -Profile checkout`: friss fordítás + LedgerChecks.Run 12, CheckoutRevisionChecks.Run 20, BasketDay 41, RecoveryAndCancellation 9 állítás. Összesen 82 sikeres állítás. A végső futás azonosítója és sourceFingerprint lent kerül rögzítésre. A jelentés az adott tartalomra vonatkozik; nem a korábbi kiadási teszt újracímkézése.

## Képrögzítés és képellenőrzés

Reviewer: Codex, 2026-11-03. Unity Editor Game view, tényleges képméret: 1920×888. Mindkét PNG megnyitva és vizuálisan átnézve.

- [Főmenü](Evidence/2026-11-03-main-menu.png): a cím és a három gomb a rögzített képen látszik, nem vágódik le.
- [Beállítások](Evidence/2026-11-03-settings.png): a billentyűk, az alapértékek és a vissza gomb megjelenik; ez igazolja, hogy a capture az IMGUI felületet is rögzíti.
- Megfigyelés: az áttetsző panelen át a világ erősen látszik. A további olvashatósági és kisebb felbontású vizsgálat a #7 feladata marad.
- Hatókör: az új capture eszköz és képek ellenőrzése sikeres. Ez nem igazolja a teljes játékosvezérlés, a billentyű-átállítás vagy az összes új funkció vizuális/interakciós elfogadási feltételeit.

WebGL-build és Pages-publikálás nem történt; ehhez a dokumentációs/eszköz workflow-módosításhoz nem szükséges. A játékfunkciók saját WebGL-próbája továbbra is pending.

## Végső futás
Run: 20261103-192015-489; sourceFingerprint: f21ec34313e25653e3c82a7fe85e966ed3a061dad2c5bacf3548b983c77d0276.
Állapot: passed; friss fordítás, 82 sikeres állítás, 0 hiba.
Teljes gépi jelentés: [checkout-report.json](Evidence/checkout-report.json).

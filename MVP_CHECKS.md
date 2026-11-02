# Ellenőrzött állapot – 2026. november 2.

A kompakt bolt jelenete és a forráskód el van mentve. Unity 6000.6.7f1, URP. A szkriptfordítás sikeres, fordítási hiba nélkül.

## Automatikus ellenőrzések

| Ellenőrzés | Sikeres állítás |
|---|---:|
| `LedgerChecks.Run` – pénzügyi alapok | 12 |
| `ProductInteractionChecks.Run` – felvétel, fix hely, visszarakás, ütközés | 24 |
| `CompactShopChecks.CashModel` – címletek, darabszám, váltás, függő vásárlói pénz | 12 |
| `CompactShopChecks.BaseDay` – teljes nyolcvásárlós cigarettás nap | 107 |
| `CompactShopChecks.ExpandedDay` – bővítés, utánrendelés, kézi feltöltés, négy önkiszolgáló vásárló | 134 |
| `CompactShopChecks.EdgeCases` – megszakítás, visszatett áru, korai zárás, fizetésképtelenség | 17 |
| `CompactShopChecks.DeviceControls` – monitor- és kasszagombok a fókuszált kamera képén, helyes egérsugár-találat | 68 |
| `CompactShopChecks.PlayerRoute` – tényleges CharacterController mozgás a pult mellett és az új polcokhoz | 12 |
| **Összesen** | **386** |

A próbák Play módban, a tényleges jeleneten futottak. A vezérlőfüggvényeket és a fizikai célzás sugarát ellenőrzik; nem a felhasználó billentyűzetét/egerét mozgatják. A karakterek végigjárják a polc és a pult közötti útvonalat, de a fejlett karakterelkerülést és a játékmenet szubjektív élményét ezek nem igazolják.

Eredményminták:

- Kezdőbolt: 8 eladás, 18 200 Ft árbevétel, 5300 Ft nyereség, 23 900 Ft zárókassza.
- Bővített bolt: 8 eladás, 4 önkiszolgáló vevő, 10 800 Ft árbevétel, 3350 Ft nyereség; 2000 Ft beruházás és 1590 Ft beszerzés után 12 910 Ft zárókassza.
- A következő nap megőrzi a pénz darabszámait, készletet és bővítést; az új játék visszaállítja a kezdőállapotot.

A gépi eredmény helyben: `Temp/CompactChecks/check-results.json`.

## Futtatás

A futó Unity helyi Pipeline kapcsolatával:

```powershell
& Tools/Invoke-Pipeline.ps1 -Command 'script run' -Parameters '{"file":"Tools/Checks/CompactShopChecks.cs","entry":"CompactShopChecks.BaseDay"}'
```

A többi belépési pont a fenti táblázatban szerepel. A napi próbák végén a játék visszaáll a kezdőállapotba. A kapcsolat hitelesítési adatát a segéd nem jeleníti meg, nem tartalmazza a forráskód, és nem kerül Gitbe.

## Látvány és Windows-változat

A Game nézetből készült képeken ellenőriztük a monitort, a kasszát és a cigarettapolcot. A betűméretet, a mélységhelyes takarást és a pénztálca elrendezését korrigáltuk. A képek helyben a `Temp/CompactChecks` könyvtárban vannak.

A végső **StandaloneWindows64 build sikeres**, nulla hibával és nulla figyelmeztetéssel. Időtartama 22,562 másodperc, mérete 104 984 429 bájt. A teljes futtatható mappa: `Builds/MVP`; indító: `NemzetiDohanyboltSzimulator.exe`.

Az önálló alkalmazás grafika nélküli indulási próbájában a folyamat futva maradt, a naplóban nem volt futásidejű kivétel/hiba. Ez a próba az indulást igazolja; a megjelenítést a Unity Game nézetében ellenőriztük. A végső kasszakép: `Temp/CompactChecks/cash-verified.png`.

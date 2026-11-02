# Ellenőrzött állapot – 2026. november 2.

A kompakt bolt jelenete és a forráskód el van mentve. Unity 6000.6.7f1, URP. A szkriptfordítás sikeres, fordítási hiba nélkül.

## Visszajáró-kijelzés javítása

A korábbi VISSZA sor a kivett címletek összegét mutatta. Most külön VISSZAJÁRÓ sor mutatja a kapott pénz és az ár különbségét, és KIVÉVE sor a játékos által már kiválasztott pénzt. A futó jelenet tényleges TextMesh kijelzőjén ellenőrizve: 2200 Ft ár / 5000 Ft kapott pénz mellett 0 Ft és 500 Ft kivételekor is 2800 Ft marad a járó visszajáró. Mindkét állapot helyes. A négy sor elfér a kijelzőn; kamera-renderen is ellenőrizve.

Az alábbi 488 állítás a raktár és hátsó udvar elkészítése után újra lefuttatott teljes ellenőrzéssor eredménye. A visszajáró-kijelzéshez a fenti célzott próbák is sikerültek.

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
| `RegisterFixtureChecks.Run` – kisebb eszközök, kihúzható fiók, mozgatott/forgatott pult és kamerák | 16 |
| `RestockingChecks.Run` – valódi játékosi kivétel, készletjelzés, kézben láthatóság, polcperemre célzás, foglalt/rossz hely, visszarakás | 25 |
| `WarehouseChecks.InventoryAndTrash` – márkadobozok, oda-vissza árumozgás, üres karton, új szállítmány, papírgyűjtő, készletmegőrzés | 40 |
| `WarehouseChecks.WalkingRoute` – tényleges mozgás a boltból a raktáron át a kukához és vissza, kartonnal | 21 |
| **Összesen** | **488** |

A próbák Play módban, a tényleges jeleneten futottak. A vezérlőfüggvényeket és a fizikai célzás sugarát ellenőrzik; nem a felhasználó billentyűzetét/egerét mozgatják. A karakterek végigjárják a polc és a pult közötti útvonalat, de a fejlett karakterelkerülést és a játékmenet szubjektív élményét ezek nem igazolják.

Eredményminták:

- Kezdőbolt: 8 eladás, 18 200 Ft árbevétel, 5300 Ft nyereség, 23 900 Ft zárókassza.
- Bővített bolt: 8 eladás, 4 önkiszolgáló vevő, 10 800 Ft árbevétel, 3350 Ft nyereség; 2000 Ft beruházás és 1590 Ft beszerzés után 12 910 Ft zárókassza.
- A következő nap megőrzi a pénz darabszámait, készletet és bővítést; az új játék visszaállítja a kezdőállapotot.

A mostani gépi eredmény helyben: `Temp/Warehouse/check-results.json`. A kézzel szerkesztett pult és pultlap helye megmaradt; a vásárlók a pulthoz kötött kiszolgálási ponthoz érkeznek.

## Futtatás

A futó Unity helyi Pipeline kapcsolatával:

```powershell
& Tools/Invoke-Pipeline.ps1 -Command 'script run' -Parameters '{"file":"Tools/Checks/CompactShopChecks.cs","entry":"CompactShopChecks.BaseDay"}'
```

A többi belépési pont a fenti táblázatban szerepel. A napi próbák végén a játék visszaáll a kezdőállapotba. A kapcsolat hitelesítési adatát a segéd nem jeleníti meg, nem tartalmazza a forráskód, és nem kerül Gitbe.

## Látvány és Windows-változat

A Game nézetből készült képeken ellenőriztük a monitort, a kasszát és a cigarettapolcot. A betűméretet, a mélységhelyes takarást és a pénztálca elrendezését korrigáltuk. A mostani képek helyben a `Temp/CounterUpgrade` könyvtárban vannak. A kisebb monitor és az új házzal, billentyűzettel, nyugtanyomtatóval és kihúzható pénzfiókkal rendelkező kassza is ellenőrizve lett.

A kasszaoldali átjárót, a hat márkadoboz raktári elrendezését és a hátsó papírgyűjtőt kameraképen ellenőriztük. A raktár 1,3 méteres ajtónyílásain a CharacterController kartont hordozva is átjut, a padlók folyamatosak. Képek: `Temp/Warehouse/warehouse-room-final.png`, `rear-yard.png`, `warehouse-entrance.png`.

A friss StandaloneWindows64 build sikeres: 0 hiba, 0 figyelmeztetés, 27,309 másodperc, 105 103 197 bájt. Indító: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`. Az önálló alkalmazás grafika nélküli indulási próbája is sikerült: futva maradt, a naplóban nem volt futásidejű kivétel vagy hiba.

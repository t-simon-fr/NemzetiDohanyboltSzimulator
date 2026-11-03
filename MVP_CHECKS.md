# Ellenőrzött állapot – 2026. november 2.

A kompakt bolt jelenete és a forráskód el van mentve. Unity 6000.6.7f1, URP. A szkriptfordítás sikeres, fordítási hiba nélkül.

## Billentyűzet, italok és szabad polcelhelyezés – 2026. november 3.

**25 ellenőrzéscsoport, 3690 sikeres állítás**, 0 sikertelen. A teljes korábbi regressziós csomag mellett a monitor és memóriajáték billentyűzetes kezelése, három teljes műszak, mindkét új ital eladása és 21 Enterrel lezárt fizetés is sikeres. A kilenc csomagolt termék és a kávé mind a 71 polchelyen ténylegesen el lett helyezve; a vegyes polcokról vásárlás és visszavonás is ellenőrzött. A végleges WebGL-build 0 hibával és 0 figyelmeztetéssel készült el; a helyi Chromium-futás 15 képet és 0 böngészős hibát adott. A tényleges billentyűzetes kártyakijelölés, alkalmazásváltás és memóriafordítás képen is ellenőrizve. Jelentés: `Builds/KeyboardAlcoholRelease/checks.json`. Részletek: [KeyboardAlcoholRelease.md](Docs/KeyboardAlcoholRelease.md). Az alábbi eredmények korábbi mérési pontok.

## Teljes feature-csomag – 2026. november 3.

**21 ellenőrzéscsoport, 1105 sikeres állítás**, 0 sikertelen. A teljes listát és a bizonyítékok helyét a [FeatureRelease.md](Docs/FeatureRelease.md) rögzíti. A futó jelenetből készült eredmények: `Builds/FeatureRelease/checks.json`. A mostani WebGL-build 0 hibával és 0 figyelmeztetéssel készült el, a helyi Chromium-próba 11 képpel és 0 böngészős hibával zárult; a kártyák tényleges kattintásra fordultak fel. Build: `Builds/FeatureRelease/build-status.json`, böngészőnapló: `Builds/Verification/local-console.json`. A két új, vegyes műszak külön ellenőrzi az ütővel megállított, illetve elmenekülő tolvajt; mindkettőben kávé, pultra tett önkiszolgáló áru, érmes és hagyományos fizetés is szerepel. A fizikai érmecélzás, felmosócélzás, kamerák, minijátékok, eldobott tárgyak, napváltás és kasszaegyezőség is sikeres. Az alábbi szakaszok korábbi mérési pontok.

## Vészgomb – 2026. november 3.

A megvásárolható vészgomb elkészült. **15 ellenőrzéscsoportban 675 sikeres állítás**, ebből 45 a `PanicButtonChecks.Run` célzott ellenőrzése: vásárlás és pénzügyi egyezőség, ismételt vétel tiltása, a fizikai gomb elérése, mindkét irányban megálló és feloldás után továbbhaladó vásárlók, kézben tartott áru és karton, lezárás alatt várakozó tényleges vevő, zárás és napváltás. A korábbi műszak-, eszköz-, feltöltési, raktári és TobaccOS-próbák is sikeresek. A régi feltöltési teszt víztermékét név szerint azonosítjuk a korábbi rögzített termékindex helyett.

Összesített eredmény: `Builds/PanicButtonPreview/checks.json`. Vizuálisan ellenőrzött képek ugyanitt: `shop.png`, `button.png`, `locked.png`. Fordítási hiba, konzolhiba és figyelmeztetés: 0. A próbák a Unity Play módjában futottak, az alábbi Windows- és WebGL-build-adatok korábbi állapotokra vonatkoznak.

Az alábbi ellenőrzési táblázat korábbi mérési pontot dokumentál.

## Visszajáró-kijelzés javítása

A korábbi VISSZA sor a kivett címletek összegét mutatta. Most külön VISSZAJÁRÓ sor mutatja a kapott pénz és az ár különbségét, és KIVÉVE sor a játékos által már kiválasztott pénzt. A futó jelenet tényleges TextMesh kijelzőjén ellenőrizve: 2200 Ft ár / 5000 Ft kapott pénz mellett 0 Ft és 500 Ft kivételekor is 2800 Ft marad a járó visszajáró. Mindkét állapot helyes. A négy sor elfér a kijelzőn; kamera-renderen is ellenőrizve.

Az alábbi 537 állítás a raktári polcok, dobozból történő feltöltés és számítógépes alkalmazások elkészítése után újra lefuttatott teljes ellenőrzéssor eredménye. A visszajáró-kijelzéshez a fenti célzott próbák is sikerültek.

## Automatikus ellenőrzések

| Ellenőrzés | Sikeres állítás |
|---|---:|
| `LedgerChecks.Run` – pénzügyi alapok | 12 |
| `ProductInteractionChecks.Run` – felvétel, fix hely, visszarakás, ütközés | 24 |
| `CompactShopChecks.CashModel` – címletek, darabszám, váltás, függő vásárlói pénz | 12 |
| `CompactShopChecks.BaseDay` – teljes nyolcvásárlós cigarettás nap | 107 |
| `CompactShopChecks.ExpandedDay` – bővítés, utánrendelés, kézi feltöltés, négy önkiszolgáló vásárló | 134 |
| `CompactShopChecks.EdgeCases` – megszakítás, visszatett áru, korai zárás, fizetésképtelenség | 17 |
| `CompactShopChecks.DeviceControls` – monitor- és kasszagombok a fókuszált kamera képén, helyes egérsugár-találat mindhárom számítógépes oldalon | 82 |
| `CompactShopChecks.PlayerRoute` – tényleges CharacterController mozgás a pult mellett és az új polcokhoz | 12 |
| `RegisterFixtureChecks.Run` – kisebb eszközök, kihúzható fiók, mozgatott/forgatott pult és kamerák | 16 |
| `RestockingChecks.Run` – teli doboz felvétele, egyenként kipakolás, láthatóság, zöld keret, foglalt/rossz hely, visszarakás | 25 |
| `WarehouseChecks.InventoryAndTrash` – márkadobozok, oda-vissza árumozgás, üres karton, új szállítmány, papírgyűjtő, készletmegőrzés | 36 |
| `WarehouseChecks.WalkingRoute` – tényleges mozgás a boltból a raktáron át a kukához és vissza, kartonnal | 19 |
| `ComputerWorkflowChecks.Run` – asztal és alkalmazások kattintása, berendezésvásárlás, új polc feltöltése, napváltás, hordozás közbeni új szállítmány | 41 |
| **Összesen** | **537** |

A próbák Play módban, a tényleges jeleneten futottak. A vezérlőfüggvényeket és a fizikai célzás sugarát ellenőrzik; nem a felhasználó billentyűzetét/egerét mozgatják. A karakterek végigjárják a polc és a pult közötti útvonalat, de a fejlett karakterelkerülést és a játékmenet szubjektív élményét ezek nem igazolják.

Eredményminták:

- Kezdőbolt: 8 eladás, 18 200 Ft árbevétel, 5300 Ft nyereség, 23 900 Ft zárókassza.
- Bővített bolt: 8 eladás, 4 önkiszolgáló vevő, 10 800 Ft árbevétel, 3350 Ft nyereség; 2000 Ft beruházás és 1590 Ft beszerzés után 12 910 Ft zárókassza.
- A következő nap megőrzi a pénz darabszámait, készletet és bővítést; az új játék visszaállítja a kezdőállapotot.

A mostani gépi eredmény helyben: `Temp/CartonWorkflow/check-results.json`. A kézzel szerkesztett pult és pultlap helye megmaradt; a vásárlók a pulthoz kötött kiszolgálási ponthoz érkeznek.

## Futtatás

A futó Unity helyi Pipeline kapcsolatával:

```powershell
& Tools/Invoke-Pipeline.ps1 -Command 'script run' -Parameters '{"file":"Tools/Checks/CompactShopChecks.cs","entry":"CompactShopChecks.BaseDay"}'
```

A többi belépési pont a fenti táblázatban szerepel. A napi próbák végén a játék visszaáll a kezdőállapotba. A kapcsolat hitelesítési adatát a segéd nem jeleníti meg, nem tartalmazza a forráskód, és nem kerül Gitbe.

## Látvány és Windows-változat

A Game nézetből készült képeken ellenőriztük a monitort, a kasszát és a cigarettapolcot. A betűméretet, a mélységhelyes takarást és a pénztálca elrendezését korrigáltuk. A mostani képek helyben a `Temp/CounterUpgrade` könyvtárban vannak. A kisebb monitor és az új házzal, billentyűzettel, nyugtanyomtatóval és kihúzható pénzfiókkal rendelkező kassza is ellenőrizve lett.

A kasszaoldali átjárót, a hat márkadoboz raktári elrendezését és a hátsó papírgyűjtőt kameraképen ellenőriztük. A raktár 1,3 méteres ajtónyílásain a CharacterController kartont hordozva is átjut, a padlók folyamatosak. Képek: `Temp/Warehouse/warehouse-room-final.png`, `rear-yard.png`, `warehouse-entrance.png`.

A friss StandaloneWindows64 build sikeres: 0 hiba, 0 figyelmeztetés, 18,793 másodperc, 105 136 109 bájt. Indító: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`. Az önálló alkalmazás grafika nélküli indulási próbája is sikerült: futva maradt, a naplóban nem volt futásidejű kivétel vagy hiba.

A legújabb képek a `Temp/CartonWorkflow/` mappában: raktári fémpolcok márkafeliratokkal, doboz hordozása, új cigarettapolc, számítógépes asztal és alkalmazások. A teljes doboz kézben marad, E egy darabot pakol ki. Az alkalmazáspróba valódi kamerából küldött egérsugárral ellenőrzi a külön oldalak gombjait; nem szimulál tényleges billentyűzet- vagy egérbemenetet.

Alkalmazásváltáskor a dinamikus betűkészlet statikus feliratai késleltetve újraépülnek; a Rendelések és Berendezés oldalon az ASZTAL, KILÉPÉS és TRAFIK OS feliratok a végső kameraképeken is láthatók. E javítás után a 41 alkalmazás- és 82 eszközellenőrzés újra sikerült.

Új kiadás: 29 csoport / 3796 állítás. `Builds/NextFeatureRelease/checks.json`; `CardAndSummaryChecks.Run` ellenőrzi a kártyás fizetést és a napvégi továbblépést. Böngésző: `node Tools/Verify-WebGL.cjs --features --dayend`.

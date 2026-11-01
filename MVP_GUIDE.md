# Nemzeti Dohánybolt Szimulátor – MVP

A prototípus egy teljes, nyolcvásárlós műszakot tartalmaz: felkészülés, nyitás, rendelésösszekészítés, készpénzes fizetés, utánrendelés és kézi polcfeltöltés, majd zárás és elszámolás.

## Indítás

Unityben nyisd meg az `Assets/Game/Scenes/ShopPrototype.unity` jelenetet, és nyomj Play-t. A már elmentett jelenet tartalmazza az MVP rendszereit. Új graybox jelenet létrehozásakor automatikusan bekerülnek; meglévő jelenethez a **Nemzeti Dohánybolt > Enable Complete MVP** menüpont használható.

A külön Windows-változat a `Builds/MVP/NemzetiDohanyboltSzimulator.exe` fájlból indul. A teljes MVP könyvtárra szükség van az exe mellett lévő adatfájlokkal együtt.

## Vezérlés

| Gomb | Művelet |
|---|---|
| WASD, egér | Mozgás, körülnézés |
| E | Felvétel, lerakás fix helyre, kassza/doboz/ajtótábla használata |
| F | Fizetési panel a kassza közelében |
| B | Beszerzés |
| Q | A hátsó dobozból kiválasztott terméktípus váltása, amikor a dobozra nézel |
| O | Nyitás, majd zárás kérése |
| X | Az aktuális vásárló udvarias elküldése |
| H | Súgó |
| Escape | Panel bezárása vagy kurzor elengedése |
| Bal kattintás a játékba | Kurzor ismételt rögzítése |

A panelek használata közben a vásárlók és a műszak ideje szünetel. A megrendelt szállítmány közben is megérkezhet. A feloldott kurzor mellett a játékmenet megáll, amíg visszakattintasz.

## Egy műszak

1. Kezdetben 6000 Ft és termékenként két darab áru áll rendelkezésre.
2. Nyitás után automatikusan érkeznek a vásárlók. Legfeljebb hárman állnak sorban. Négy visszatérő személyiség, eltérő színű próbafigura és rövid párbeszédek adják a változatosságot.
3. A kék áru ásványvíz, a zöld rágó, az aranyszínű sör. A kért terméket tedd a pult KÉK helyére.
4. A megfelelő áru megnyitja a fizetést. A kapott pénzt címletenként látod; a visszajárót a címletgombokkal állítod össze. A hibás összeget javíthatod. A készlet és a kassza csak a helyes fizetéskor változik.
5. B-vel háromdarabos csomagokat rendelhetsz. A költség azonnal levonódik, a szállítás nyolc másodperc. Az áru a hátsó barna dobozba érkezik, onnan E-vel vehető ki és kézzel rakható a polcra. Q váltja a kiválasztott típust.
6. A kis kezdőkészlet miatt ásványvízből pótolni kell. Ha egy rendelést nem tudsz teljesíteni, X-szel elküldheted a vásárlót.
7. Nyolc érkezés után, a sor rendezésével automatikusan zárás következik. O-val korábbi zárást is kérhetsz. A napvégi képernyőn elkülönül a nyereség és a kasszapénz.
8. A következő nap átveszi a készletet, a doboz tartalmát és a kasszát. Az új játék gomb az eredeti állapotot állítja vissza.

## Pénzügyi szabályok

| Áru | Eladási ár | Darab beszerzési ára | Háromdarabos csomag |
|---|---:|---:|---:|
| Buborék ásványvíz | 350 Ft | 150 Ft | 450 Ft |
| Menta rágó | 300 Ft | 120 Ft | 360 Ft |
| Pirkadat sör | 550 Ft | 260 Ft | 780 Ft |

A napi működési költség 300 Ft. A nyereség az árbevétel mínusz az eladott áruk beszerzési értéke és a napi költség. A kassza a nyitópénzhez hozzáadott bevételből, az utánrendelések levonásával és a napi költségből áll. A megmaradt, már megvett áru nem kerül még egyszer levonásra a következő napon.

## Ellenőrzések

- `Tools/Checks/LedgerChecks.cs`: pénzügyi számítások, visszajáró, helytelen tranzakciók, zárás.
- `Tools/Checks/ProductInteractionChecks.cs`: fix helyek, foglaltság, méret, ütközések, megszakított felvétel.
- `Tools/Checks/FullMvpChecks.cs`: a valódi bolt teljes nyolcvásárlós napja, sor, téves áru javítása, beszerzés, kézi feltöltés, fizetés és elszámolás. A `CheckNextDayAndReset` a napváltást, újrakezdést és korai zárást ellenőrzi.

Az ellenőrzések a Unity Pipeline `script run` parancsával futnak. A teljes napteszt Play módot igényel, és a végén az összesítő képernyőn marad; a napváltási ellenőrzés visszaállítja a kezdőállapotot.

## Az MVP határai

A látvány és a vásárlók mozgása szürkedobozos: előre megadott útvonalak és egyszerű figurák. A napok közötti állapot a futó játékon belül marad meg; bezárás után új játék kezdődik, tartós mentés még nincs. Egy vásárló egy terméket kér. A hangok, végleges modellek és animációk későbbi kidolgozást kapnak.

Az ellenőr, a hitelkérés, a készpénzes trükkök és a figyelemeltereléses lopás a `../GAME_VISION.md` szerinti későbbi rendszerek.

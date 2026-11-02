# Folytatás innen

Mentési pont: 2026. november 2.

## Projekt

- Unity **6000.6.7f1**, URP, Input System.
- Projektgyökér: `D:\Projects\nemzeti-dohanybolt-sim\NemzetiDohanyboltSzimulator`.
- Jelenet: `Assets/Game/Scenes/ShopPrototype.unity`.
- Windows-változat: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`.
- Játékosi használat: `MVP_GUIDE.md`. Ellenőrzések: `MVP_CHECKS.md`.
- Iránydokumentum: `Docs/GAME_VISION.md`.

## Legutóbbi javítás – doboz, kézben tartott áru és polcfeltöltés

A szállítódoboz készletlista és a kiválasztott típus/darabszám megjelenítését kapta; az üres doboz és az úton lévő rendelés külön üzenetet ad. A kezdő raktár továbbra is üres, a monitoron kell rendelni. Ha a kiválasztott típusból nincs, de másikból van megérkezett áru, a kivétel automatikusan elérhető típust választ.

A játékos akkor is kézbe veheti az árut, ha a megfelelő polc tele van; a dobozra nézve E-vel visszarakható. A kézben tartott termék magasabban, a vezérlősáv felett látszik, névvel együtt. A megfelelő üres helyeket zöld LineRenderer keret mutatja. A peremre célzás közeli polchelyre illesztést is enged, láthatóságellenőrzéssel. A rossz terméktípus és a foglalt hely továbbra sem fogad árut.

A `PlayerInteractor.RefreshTarget` és `TryPlaceHeld` ugyanazt az interakciót szolgálja ki a billentyűzetből és a valódi jeleneten futó `RestockingChecks.Run` próbából. A dobozba visszarakás darabszámot és helyfoglalást is helyesen rendez. Nem került be ingyenes vagy végtelen készlet.

## Előző javítás – kézzel áthelyezett pult, kisebb eszközök, kasszafiók

A felhasználó szerkesztőben, még nem mentett állapotban mozgatta a pultot és a pultlapot. Ezeket a módosításokat a frissítés előtt elmentettük; helyi biztonsági másolat: `Temp/CounterUpgrade/BeforeFixtures.unity`. A bolt nem lett újragenerálva.

A pult pozíciója közelítőleg `(0.742, .550, -1.404)`, a pultlapé `(0.731, 1.130, -1.453)`; a pultlap 3,4 méteres szélessége is megmaradt. A felszerelés a pultlapból számolt magasságra került.

A **Counter Assembly** csoportot kell mozgatni a teljes munkaállomás átrendezéséhez. A `CounterLayout` kiszolgálási pontja és a vásárlói sor ebből követi az új helyet/forgatást. A `ShopLayout` bejárati/kijárati/átjáró jelölőket használ; a polc megközelítése a termékhely irányából számolódik.

A monitor 62 cm széles, fekvő képarányú, kétoszlopos rendelési képernyővel. A kassza 58 cm széles házat, ferde billentyűzetet, kis kijelzőt és nyugtanyomtatót kapott. A tizenkét pénzrekeszt tartalmazó fiók 44 cm-t csúszik ki 0,35 másodperc alatt; fizetéskor nyílik, kilépéskor csukódik. Zárt vagy még nyíló fiók pénzei nem fogják el a kattintást.

A `CashRegisterDrawer` kezeli a mozgást. A ház ferde felületét egyszerű, döntött BoxCollider követi; a túl nagy dobozütköző eltakarhatná a billentyűket. A Player az Ignore Raycast rétegen van, hogy a fókuszált kasszára célzást ne takarja a saját CharacterController. Az eredeti, változatlan játékos-kezdőhely a pult mögé lett igazítva.

A **Nemzeti Dohánybolt > Align Counter Equipment** helyben frissíti a berendezéseket a pultlaphoz, a bolt többi részének megtartásával. A meglévő kézi berendezés-igazításokat alaphelyzetbe rendezi. A **Build Compact Shop** továbbra is teljes újragenerálás.

Az ellenőrzések aktuális eredménye a `MVP_CHECKS.md` fájlban van; az új `RegisterFixtureChecks.Run` a fiók köztes/nyitott/zárt állapotait, a méreteket és a mozgó/forgatott pultot ellenőrzi.

## A korábbi jegyzetekből elkészült

1. A pult bal oldalán játékbeli beszerzési monitor: hat terméktípus, készlet/raktár/úton levő áru, rendelés és bővítés.
2. A fizetési modal helyett fizikai kassza: mind a tizenkét magyar címletérték, véges darabszám, darabonkénti kivétel, pénztálca, visszatétel, váltópénz és átadás. A grafika még próbamodell.
3. Kisebb, 5 × 6,3 méteres kezdőbolt; Pannon Piros, Duna Kék és Alföld Arany cigarettából 8-8 doboz.
4. Egy 2000 Ft-os, nyitás előtt megvehető, 4 × 6,3 méteres bővítés. A pultnak a jelenetben beállított helye bővítéskor állandó. A pult mellett a játékos is elfér.
5. A bővítés új polcairól a vásárlók önállóan veszik fel a vizet, rágót vagy sört, majd ugyanahhoz a pulthoz mennek fizetni. A cigarettát az eladó adja ki.

A monitor és kassza közelről használható, kamerafókusszal és kattintható térbeli kezelőszervekkel. Az eszközhasználat szünetelteti a vásárlókat. A nyitóképernyő, súgó és napi összesítés továbbra is külön képernyő.

Megmaradt az összefüggő nyolcvásárlós műszak, a humoros rövid párbeszéd, a sor, az utánrendelés és kézi feltöltés, a korai/automatikus zárás és a napváltás. A kassza, a készlet és a bővítés a futó alkalmazáson belül megmarad a következő napra.

Az önkiszolgáló vásárlás megszakításakor az áru az eredeti, addig lefoglalt polchelyére kerül vissza. A kapott vásárlói pénz a fizetésig külön van kezelve, visszajáróhoz felváltható, elküldéskor visszaadható. A könyvelés a beruházást külön kezeli. Elfogyott kasszapénznél a napvégi tartozás után új játék szükséges.

## Következő közös próba

- Játékosként végigjátszani a kis boltot, majd bővíteni és feltölteni az új polcokat.
- Kasszakamera, olvashatóság, rekeszek kattintása és visszajáró tálcájának elrendezése.
- A polc, pult és szállítódoboz közötti mozgás kényelme.
- Gazdasági arányok: az árak, a 24 dobozos kezdőkészlet, a bővítés ára még prototípus-beállítások.
- A jelenlegi egytermékes rendelések és a figurák mozgásának finomítása.

## Még tervezett tartalom

Tartós játékmentés; kész bolti modellek és magyar pénzgrafika; több bővítési fokozat; összetettebb kosarak és tömegelkerülés; ellenőr és igazolvány; hitelkérés; félreszámolt pénz és készpénzes trükkök; figyelemeltereléses lopás; reggeli törzsvendégek és további humoros párbeszédek. Ezek nem részei a mostani implementációnak.

## Ellenőrzés és fejlesztés

A teljes ellenőrzési eredmény és a build állapota: `MVP_CHECKS.md`. A `Tools/Checks/CompactShopChecks.cs` tartalmazza a két teljes nap, a pénz, az összes eszközgomb, a megszakítások és a tényleges játékosi útvonal próbáit. A régi víz/rágó/sör MVP-hez kötött napteszteket ezek váltották fel.

- A jelenetet a **Nemzeti Dohánybolt > Build Compact Shop** újraépíti. A régi Build Graybox menü is erre vezet; ne a korábbi nagytermes generátort állítsuk vissza.
- Fő kód: `MvpShopController`, `ShopDevices`, `ShopCashDrawer`, `ShopLayout`, `MvpCustomerActor`. Generátor: `Editor/CompactShopSetup.cs`; helyben frissítés: `Editor/CounterFixtureSetup.cs`.
- A régi egyvásárlós `CustomerOrderController` a kompatibilitási forrásban maradt; a kompakt jelenetben nincs aktív példánya.
- A Pipeline kapcsolat leírója: `Library/Pipeline/.unity-pipeline-port`. Hitelesítési adatot tartalmaz, ne írassuk ki. A `Tools/Invoke-Pipeline.ps1` kezeli anélkül, hogy megjelenítené.
- Play módban ne mentsük a jelenetet: a futás során létrejött készlet és vásárlók nem szerkesztett jelenetadatok.
- `Library`, `Temp`, `Logs` és `Builds` nem kerül Gitbe.
- Az eredeti C:-s munkakönyvtár már nem létezik. Minden parancshoz a fenti D:-s munkakönyvtár kell; a sandboxon kívüli írást az eszköz engedélyezett emelt futtatása kezeli.

# Folytatás innen

Mentési pont: 2026. november 3.

## Projekt

- Unity **6000.6.7f1**, URP, Input System.
- Projektgyökér: `D:\Projects\nemzeti-dohanybolt-sim\NemzetiDohanyboltSzimulator`.
- Jelenet: `Assets/Game/Scenes/ShopPrototype.unity`.
- Windows-változat: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`.
- Játékosi használat: `MVP_GUIDE.md`. Ellenőrzések: `MVP_CHECKS.md`.
- Iránydokumentum: `Docs/GAME_VISION.md`.

## Legfrissebb: billentyűzet, italok és szabad polcelhelyezés

Az #1 issue három új feladata is elkészült. Kassza: Enter, Backspace, Delete, címletbillentyűk. Monitor: Tab / nyilak / Enter, Backspace, alkalmazás- és termékgyorsbillentyűk. Kevert 50 ml és Kőfejtői 500 ml saját modellel, öt italpolccal és kilenc raktári márkahellyel. Bármely termék és a kávé bármely szabad bolti polchelyen elhelyezhető; a magas palackokhoz igazított kezdőpolc, tartalomkövető árcímkék és a tényleges helyről vásárló NPC-k is elkészültek. **25 csoport, 3690 sikeres állítás**. Részletek: `Docs/KeyboardAlcoholRelease.md`; jelentés: `Builds/KeyboardAlcoholRelease/checks.json`.

## Teljes GitHub feature-csomag

Az eredeti issue mind a kilenc kérése megvalósult, a vészgombbal és az egybenyitott üzlettel együtt. Aktuális funkciók, kezelés, árak és ellenőrzés: **Docs/FeatureRelease.md**. Fő új források: `ShopActivities`, `ShopMiniGames`, `ShopHandTool`, `MvpShopFeatures`; próbák: `FeatureReleaseChecks`. Az előző teljes ellenőrzés **21 csoportban 1105 sikeres állítás** volt. A későbbi szakaszok korábbi mérési és fejlesztési pontokat dokumentálnak.

A TobaccOS új alkalmazásai: Kávé, Kamerák, Játékok. Bővítés 24 000 Ft, extra polc 9000 Ft, kávégép 16 000 Ft; a vészgomb 7500 Ft. G-vel minden hordozható tárgy eldobható. A kamerák és minijátékok közben a bolt működik. A kávé a `ShopCatalog.Coffee` termékazonosító; a `ShopCatalog.Count` a kilenc polcos/raktári terméket jelenti, a kávéalapanyag külön rendszerben készül és rendelhető.

## Egybenyitott üzlet bővítés után

A megvásárolt önkiszolgáló bővítés az első válaszfal mellett a pult melletti hosszabb `Staff side wall` falszakaszt is elrejti, a hozzá tartozó burkolattal és ütközővel együtt. A bolt egy nagy helyiséggé válik. Napváltáskor nyitva marad a tér; új játéknál mindkét falszakasz visszaáll. A meglévő jelenetben és az újragenerált boltban ugyanaz a `ShopLayout.SetExpanded` működés érvényes.

## Megvásárolható vészgomb

A TobaccOS Üzletfejlesztés alkalmazásából nyitás előtt, egyszer, 7500 Ft-ért vehető meg. E a pult mögötti piros gombon lezárja vagy feloldja a bejárati redőnyt; kézben tartott termékkel és kartonnal is használható. A vásárlók mindkét irányban megállnak előtte, a bentiek kiszolgálása folytatható. Záráskor automatikusan feloldódik, a megvásárolt fejlesztés napváltáskor megmarad. A berendezés futáskor épül fel, a jelenet újragenerálása nélkül. Részletek és ellenőrzés: `Docs/PanicButton.md`, `Tools/Checks/PanicButtonChecks.cs`. Az összes további GitHub-feature megvalósítását a fenti FeatureRelease dokumentum rögzíti.

## TobaccOS – aktuális számítógép

A számítógép új TobaccOS asztalt, képes **Árurendelés** és **Üzletfejlesztés** alkalmazást kapott. A termékekből 1–12 darab választható; az összeg a kiválasztott mennyiség beszerzési ára. Kezdőboltban kizárólag a négy cigarettamárka jelenik meg. A ReturAqua, Loca Cola és Hamster Energy neve, képe és kattintási célpontja csak a megvásárolt önkiszolgáló bővítés után látható. Az áru nyolc másodperc után a megfelelő raktári kartonba érkezik.

A meglévő bővítés és extra cigarettapolc megmaradt. Új bútorbolt, bútor-elhelyezés és személyre szabás későbbi feladat; ezeket most nem implementáltuk. Aktuális működés, továbbfejlesztési határok és ellenőrzési parancsok: `Docs/TobaccOS.md`. A korábbi szakaszokban szereplő hattermékes katalógus és régi alkalmazásnevek történeti előzmények.

Unity-próbák: **585 sikeres ellenőrzés**, benne az új `TobaccOSChecks.Run` 83 ellenőrzésével. Jelentés: `Builds/TobaccOSPreview/checks.json`. Az új WebGL-build és a publikált változat böngészős ellenőrzését a `Tools/Verify-WebGL.cjs --tobaccos` munkafolyamat támogatja. A publikus repó kizárólag a lefordított játékfájlokat tartalmazza.

## Legutóbbi fejlesztés – raktári polcok, dobozból feltöltés, számítógépes alkalmazások

Az előző raktári asztalok helyén hat, többszintes fémpolc áll. A pult és az ajtók megmaradtak. A frissítés helyben történt, a korábbi jelenet: `Temp/CartonWorkflow/BeforeChanges.unity`. Szerkesztői menü: **Upgrade Carton And Computer Workflow**; külön **Upgrade Warehouse Shelves** is elérhető. A teljes generátor is hozzáadja az új berendezést.

**E a dobozon a teljes kartont veszi fel, akár teli is. E a megfelelő üres bolti polchelyen egy darabot pakol ki, a doboz kézben marad.** A `WarehouseCarton` saját mennyiséget tárol; a vezérlő raktári tömbje ezek összege. A `WarehouseArea.SyncBoxes` a különbséget egyezteti, a pozitív szállítás a dokkolt kartonba kerül, vagy új kartont hoz létre. Kézben levő dobozba nem érkezik új áru. A fogyást a `StockFromCarton` könyveli a tényleges sikeres lerakás után. A legacy `TryCreateSupply` a régi napi ellenőrzéseket szolgálja ki, játékban a márkaállomások teljes dobozt adnak.

A cartonhoz a `ProductSlot.CanStock` csak márkához kötött, üres, méretben megfelelő, fizikailag szabad polchelyet enged; a kassza átadóhelyét nem. A zöld keretek és a ray-célzás dobozzal is működnek. A félig teli doboz visszavihető. Foglalt dokknál két szabad polcszint valamelyikére kerül, majd telítettség esetén a külön kartongyűjtő helyre. Csak üres karton dobható a kukába.

**B a pult közeléből megnyitja az asztalt**, legfeljebb 3,5 méterre a monitor képernyőjétől. E a teljes monitoron működik, mert az assembly gyökerén is van `ShopStation`. A számítógépen `ShopComputerApp.Desktop`, `Orders`, `Furnishing` oldalak váltják egymást, az inaktív oldal nem foghat kattintást. `WorldControlKind.OpenApp`, `Desktop`, `ExtraShelf` kezeli a navigációt. Minden megnyitás az asztalra vezet.

A **Berendezés** alkalmazásban a 2000 Ft-os bővítés mellett egy 1200 Ft-os extra cigarettapolc is megvehető nyitás előtt, egyszer. A jobb oldali személyzeti falnál 9 márkához kötött helyet ad, ingyenáru nélkül. `ShopLayout` tárolja a hivatkozását és tulajdonlását; napváltás megtartja, ResetDemo elrejti. A bolt többi része nem mozdul. Szabad játékbeli bútor-elhelyezés még nincs.

Ellenőrzések és friss build: `MVP_CHECKS.md`. Az új alkalmazás- és készletpróbák: `ComputerWorkflowChecks.Run`; a `RestockingChecks` és `WarehouseChecks` már az új, dobozból egyenként kipakoló működést kérik számon. Képek: `Temp/CartonWorkflow/`.

## Előző fejlesztés – kasszaoldali raktár, márkadobozok és hátsó udvar

Az alábbi előzmények a korábbi állapotot írják le; a jelenlegi dobozos feltöltéshez a fenti rész és a `MVP_GUIDE.md` az irányadó.

A bolt megtartásával hozzáépült egy 3,5 × 4 méteres raktár a kassza felőli, világkoordináta szerinti bal oldalfal mögött. A régi `Left wall` inaktív maradt meg, a résfalak 1,3 méteres nyílást hagynak. A raktárból hátul kis, kerített udvarra lehet kimenni a nagy zöld papírgyűjtőhöz. A pult, a kézi pultlap-igazítások és a meglévő polcok nem lettek újraépítve. Biztonsági másolat a hozzáépítés előtti jelenetről: `Temp/Warehouse/BeforeWarehouse.unity`.

Mind a hat terméktípus saját, feliratozott `Brand Dock` helyet kapott. A megérkezett készlet a saját márka kartonjában jelenik meg; a típust most a konkrét doboz választja ki, nem Q. Egy márka raktári darabszámát egy doboz fogja össze, a plafon 24 darab. Az első megérkező áru vagy visszatett termék létrehozza a dobozt. A víz/rágó/sör beszerzése továbbra is boltbővítéshez kötött.

E a dobozon terméket vesz ki; kézben levő termékkel a megfelelő dobozon E visszarakja. A rossz márka nem fogadja el. A kiürült karton E-vel kézbe vehető és a hátsó kukánál E-vel kidobható. Teli karton és eladható áru nem dobható ki. Ha az üres karton hordozása közben új szállítmány érkezik, az új doboz külön jön létre, a régi kidobása nem töröl készletet.

Fő kód: `WarehouseArea` (fizikai dobozok és dokkok), `WarehouseCarton` (üres csomagolás és megjelenített mennyiség), `WarehouseBin` (papírgyűjtő), a márkához köthető `ShopStation` és a kibővített `PlayerInteractor`. A készlet továbbra is a műszakvezérlőben van, a kartonok ezt jelenítik meg. Napváltás megőrzi a raktárt és a csomagolást, új játék törli az új dobozokat és a kukaszámlálót.

Szerkesztői hozzáadás: **Nemzeti Dohánybolt > Add Warehouse And Rear Yard**; meglévő raktár esetén nem generálja újra. A `Build Compact Shop` is hozzáadja az új alapelrendezéshez. A `Warehouse And Rear Yard` csoport a jelenetben átrendezhető; a dobozok a hivatkozott dokkokhoz igazodnak.

Próbák: `WarehouseChecks.InventoryAndTrash`, `WarehouseChecks.WalkingRoute`. Az aktuális összesített eredmény a `MVP_CHECKS.md` fájlban van.

## Előző javítás – doboz, kézben tartott áru és polcfeltöltés

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

2026-11-03: az új hét feature, bankkártya és a napvégi gombok javítása elkészült. Részletek: [NextFeatureRelease](NextFeatureRelease.md). Ellenőrzés: 3796 állítás / 29 csoport, Builds/NextFeatureRelease. A csempész és dinamikus árképzés külön jövőbeli feladat.

2026-11-03: az esti maradás/csempész és az állítható ár/kereslet elkészült, 3854 ellenőrzés sikeres. [Kezelés és ellenőrzés](EveningPricingRelease.md). A felhasználó kérte, hogy ezt a változatot ne publikáljuk Pagesre; az éles kiadás 43f88b0 marad.

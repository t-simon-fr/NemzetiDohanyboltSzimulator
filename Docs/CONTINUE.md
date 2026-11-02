# Folytatás innen

Mentési pont: 2026. november 2.

## Projekt

- Unity **6000.6.7f1**, URP, Input System.
- Projektgyökér: `D:\Projects\nemzeti-dohanybolt-sim\NemzetiDohanyboltSzimulator`.
- Jelenet: `Assets/Game/Scenes/ShopPrototype.unity`.
- Windows-változat: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`.
- Játékosi használat: `MVP_GUIDE.md`. Ellenőrzések: `MVP_CHECKS.md`.
- Iránydokumentum: `Docs/GAME_VISION.md`.

## A tegnapi jegyzetekből elkészült

1. A pult bal oldalán játékbeli beszerzési monitor: hat terméktípus, készlet/raktár/úton levő áru, rendelés és bővítés.
2. A fizetési modal helyett fizikai kassza: mind a tizenkét magyar címletérték, véges darabszám, darabonkénti kivétel, pénztálca, visszatétel, váltópénz és átadás. A grafika még próbamodell.
3. Kisebb, 5 × 6,3 méteres kezdőbolt; Pannon Piros, Duna Kék és Alföld Arany cigarettából 8-8 doboz.
4. Egy 2000 Ft-os, nyitás előtt megvehető, 4 × 6,3 méteres bővítés. A pult `(0, .55, -2.05)` pozíciója állandó. A pult mellett a játékos is elfér.
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
- Fő kód: `MvpShopController`, `ShopDevices`, `ShopCashDrawer`, `ShopLayout`, `MvpCustomerActor`. Generátor: `Editor/CompactShopSetup.cs`.
- A régi egyvásárlós `CustomerOrderController` a kompatibilitási forrásban maradt; a kompakt jelenetben nincs aktív példánya.
- A Pipeline kapcsolat leírója: `Library/Pipeline/.unity-pipeline-port`. Hitelesítési adatot tartalmaz, ne írassuk ki. A `Tools/Invoke-Pipeline.ps1` kezeli anélkül, hogy megjelenítené.
- Play módban ne mentsük a jelenetet: a futás során létrejött készlet és vásárlók nem szerkesztett jelenetadatok.
- `Library`, `Temp`, `Logs` és `Builds` nem kerül Gitbe.
- Az eredeti C:-s munkakönyvtár már nem létezik. Minden parancshoz a fenti D:-s munkakönyvtár kell; a sandboxon kívüli írást az eszköz engedélyezett emelt futtatása kezeli.

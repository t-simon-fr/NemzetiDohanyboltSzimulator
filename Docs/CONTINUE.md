# Folytatás innen

Mentési pont: 2026. november 1.

## Projekt

- Unity: **6000.6.7f1**, URP.
- Projektgyökér: `D:\Projects\nemzeti-dohanybolt-sim\NemzetiDohanyboltSzimulator`.
- Jelenet: `Assets/Game/Scenes/ShopPrototype.unity`.
- Windows-változat: `Builds/MVP/NemzetiDohanyboltSzimulator.exe`.
- Használat: `MVP_GUIDE.md`.
- Ellenőrzések: `MVP_CHECKS.md`.
- Játékirány: `Docs/GAME_VISION.md` (a projekt fölötti iránydokumentum mostani, verziókezelt másolata).

## Elkészült állapot

A teljes, összefüggő MVP működik: nyitás előtti felkészülés, nyolcvásárlós nap négy karakterrel, háromfős sor, három termék, fix polc- és pulthelyek, rendelésösszekészítés, készpénzes fizetés és címletes visszajáró, utánrendelés, szállítódoboz és kézi feltöltés, automatikus vagy korai zárás, napi elszámolás és következő nap. A humor és az alap vezérlés magyar nyelvű.

A teljes nap ellenőrzött mintája: nyolc kiszolgált vásárló, 3100 Ft árbevétel, 1360 Ft eladottáru-költség, 300 Ft működés, 1440 Ft nyereség, 450 Ft utánrendelés, 8350 Ft zárókassza.

134 automatikus ellenőrzés sikerült. A Windows-build sikeres, nulla hibával és figyelmeztetéssel. A külön alkalmazás grafika nélküli indulási próbája is sikerült. A felületeket az Editor Game nézetéből készült képeken ellenőriztük.

## Következő alkalom – a felhasználó konkrét jegyzetei

Az alábbiak új, megállapodott fejlesztési irányok. Még tervezettek, nem részei a most elmentett MVP-nek.

1. **Rendeléskezelő monitor a pult bal oldalán.** Kerüljön oda egy játékbeli monitor, amelyen kezelhetők a rendelések. Ebből induljunk ki a jelenlegi rendelési/beszerzési panel átdolgozásánál.
2. **Fizikai pénztárgép magyar pénzzel.** A fizetés a játékbeli pénztárgépen történjen a lebegő fizetési modal helyett. A játékos a kasszából vegyen ki valódi magyar forintcímleteknek megfelelő bankjegyeket és érméket a visszajáróhoz. A konkrét kezelés és megjelenés még kidolgozandó.
3. **Kisebb, cigarettákkal feltöltött kezdőbolt.** A kezdő helyiség legyen kompaktabb, és a kezdő választékban jelenjenek meg a cigaretták. A mostani víz/rágó/sör tesztválaszték és nagy graybox terem ehhez igazítandó.
4. **Későbbi területbővítés, helyben maradó pulttal.** A bolt területe legyen növelhető, miközben a pult ugyanabban a pozícióban marad. A bővítési fokozatok és árak még nyitottak.
5. **Vásárlói önkiszolgálás az új polcoknál.** A bővített terület új polcairól a vásárlók önállóan gyűjtsék össze a vásárlásukat, majd ugyanahhoz a pulthoz menjenek fizetni. Az útvonalak és polccélpontok kövessék a bővítéseket.

A következő fejlesztési tervet ezekre építsük; a működő MVP műszak- és pénzügyi alapjait felhasználhatjuk hozzá. A teljes műszak további játékosi próbája és a tempó, gazdaság, kezelés, humor finomítása továbbra is hasznos.

A látvány és a figurák még szürkedobozosak. A tartós játékmentés nincs implementálva: a napváltás a futó alkalmazáson belül őrzi meg az állapotot. A játék bezárásakor a pénz/készlet nem kerül lemezre.

Az ellenőr, hitelkérés, készpénzes trükkök, figyelemeltereléses lopás és a reggeli törzsvendégek további történetei kötelezően tervezett későbbi tartalmak.

## Fontos technikai tudnivalók

- Az MVP-t a `Scripts/Mvp/MvpShopController.cs` és a `MvpShopHud.cs` vezérli. A régi `CustomerOrderController` csak az inaktív figuramintán maradt meg.
- Meglévő jelenetet a **Nemzeti Dohánybolt > Enable Complete MVP** kapcsol be, újragenerálás a **Build Graybox Shop Scene** menüponttal lehetséges. A jelenet már be van kötve és el van mentve.
- A Unity Pipeline csomag telepítve van. A futó Editor helyi kapcsolatának leírója a `Library/Pipeline/.unity-pipeline-port` fájlban van; hitelesítési adatot tartalmaz, ezért a tartalmát ne jelenítsük meg vagy verziókezeljük.
- A `Library`, `Temp`, `Logs` és `Builds` könyvtárakat a Git figyelmen kívül hagyja. A Windows-build helyben megmarad, de nem része a forráskód-commitnak.
- A munkamenet eredeti C:-s mappáját a felhasználó áthelyezte. Minden fájlművelethez a fenti D:-s munkakönyvtárat használjuk; a korábbi sandbox miatt a D:-s írásokhoz az eszközben engedélyezett emelt hozzáférés kellett.

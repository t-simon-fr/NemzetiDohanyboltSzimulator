# #124 – Ellenőrzési jelentés

[Használat és modulhatárok](../../Architecture/Partners/README.md). Három külön 20 000 Ft-os csomag; hét új változat, két azonos méretű márkás hűtő és egy hat termékhelyes pult feletti tartó. Az issue „nyolc termék” sora számlálási elírás: kilenc terméket sorol fel, ebből kettő már létezett; a meglévő Kevert szintén a saját családjának kedvezményét kapja.

## Natív ellenőrzés

[Teljes baseline](unity.json): **1295 sikeres állítás / 51 suite, 38,30 s**. A három új fenntartott suite: partner-pricing (20), partner-integration (79), partner-targeting (34). Valódi készlet- és pénzútvonalon vizsgálják a külön feloldásokat, a védett lottópénzt, a közös bútorvásárlás/mentés/megszakítás folyamatát, dupla inputot, családhoz kötött helyeket, szállítást, NPC-rendelést, kézi feltöltést, régi/új egységköltséget, kártyás kasszázást, napváltást és új játékot.

A végső diff-review a nap végi listán egy megmaradt, kilenctermékes ciklust talált. A lista most a teljes aktív SupplyTypes kínálatot mutatja; a beszerzési kerekítés külön soron látszik. Ehhez [friss célzott natív kör](unity-final.json): **114 állítás / 3 suite, 9,64 s** (partner-integration, ui-presentation, entrance-day). Nem új teljes körként számoljuk el. A baseline óta a játékosi működésben kizárólag MvpShopHud.cs változott. A böngészős futtató új napvégi inputlépéseket és egy hiányzó helyi kattintási segédfüggvényt kapott.

A WebGL-próba később az előkészítő kamera hibáját is kimutatta: a harmadik kisüvegnél a karakter a pultba került, a másik irányú általános korrekció pedig a cigarettafalba. A kamera most a kettő közti szabad személyzeti folyosóról céloz. A fenntartott natív célzási próba előbb mozgatja/ülepíti a CharacterControllert, utána az elvárt konkrét polchelyet is ellenőrzi. [Friss végső célzás](unity-targeting-final.json): **34 állítás**, a végső builddel egyező forráslenyomattal. Ez a későbbi módosítás csak az Editor/acceptance előkészítőt és tesztjét érinti; a partner- és pénzlogika nem változott.

[Források és megfigyelések](native-review.json), 1918×879, GUI-val: [hűtők](native-fridges.png), [pult feletti tartó](native-counter.png), [partnerségi oldal](native-partners.png), [napvégi kerekítés](native-summary-top.png), [görgetett teljes készlet](native-summary-stock.png). Reviewer: Codex saját képi review. A jelenetek és a görgetés API-val előkészítettek; nem natív egérinterakcióként számoljuk el őket. A tesztfinanszírozás miatt a képek mesterséges nagy bevételt mutathatnak.

Az első képi körben a termékfeliratok túlnyúltak a csomagoláson, a pulti tartó pedig az ütőbe ért. A végleges modelleken a feliratok szélessége korlátozott; a tartó feljebb került és az ütővel való távolsága ellenőrzött. A három csomag és összes gombja kényelmesen elfér a TobaccOS oldalon; a napvégi új sorok görgetve olvashatók.

## WebGL

[Végső WebGL-build](build.json): **0 hiba / 3 figyelmeztetés, 250,67 s**. A figyelmeztetések a Unity/Emscripten WebGPU/JS-könyvtárából származnak. A friss build időbélyegét az aktuális indítás UTC-idejéhez ellenőriztük. Az első sikeres build után a HUD-készletlista javítása, a második után az elfogadási kamerák korrigálása indokolt új buildet; ezek nem egyetlen sikeres buildként vannak elszámolva. A végső játéklogika ezt követően nem változott.

[Partnerségek](browser.json): **45**, [beszerzési regresszió](browser-cart.json): **6**, [készletes átrendezés](browser-arrangement.json): **15**, [végső napvégi görgetés](browser-summary.json): **4 sikeres ellenőrzés**, mind 0 konzolhibával. Az indítási és napváltási állítások átfednek; nem 70 különböző követelményt jelentenek. Mind a négy jelentés azonos build-asset SHA256 értékeket tartalmaz.

A teljes kör valódi egér/billentyű bemenettel végzi a csomagelhelyezés megszakítását, a két hűtő mentéskori fizetését, az egyszeri pulti vásárlást és ismételt kattintást, az összes új termék megrendelését, hét külön karton és a régi Hamster kézi feltöltését (E és bal klikk), az NPC új termékhez vezető útját, a 650 Ft-os terminálfizetést és a 270 Ft-os tényleges beszerzési költséget, majd a táblás zárást, ajtós naplezárást és következő napot. Az előkészítő a kezdőállapotot, kamerát és szimulációs időt kezeli, nem a vizsgált vásárlási/feltöltési/fizetési inputot helyettesíti.

A teljes kör után kizárólag a böngészős runner napvégi képrögzítése változott: a kerék kis elmozdulása helyett a valódi görgetősávot húzza alulra. Ezt a külön, négyállításos partner-summary kör ellenőrizte azonos playeren; ez már API-val előkészített, megvett/feltöltött partnerállapotból indul, majd valódi terminál-, tábla-, ajtó-, görgetősáv- és Enter-bemenetet használ. A további runner-diff csak behúzás; Node-parse ellenőrizve. [Végső eszközforrás](test-source.json).

Codex saját [képi review](visual-review.json): [ajánlatok 720p](1280x720-partner-offers-720.png) / [1080p](1920x1080-partner-offers-1080.png), [megvett csomagok](1280x720-owned-partners-720.png), [kedvezményes kosár 720p](1280x720-discounted-cart-720.png) / [1080p](1920x1080-discounted-cart-1080.png), [feltöltött hűtők 720p](1280x720-stocked-fridges-720.png) / [1080p](1920x1080-stocked-fridges-1080.png), [Műszakkezdő 720p](1280x720-stocked-counter-display-720.png) / [1080p](1920x1080-stocked-counter-display-1080.png), [napvégi kerekítés](1280x720-partner-summary-top-720.png), [teljes új készlet 720p](1280x720-partner-summary-stock-720.png) / [1080p](1920x1080-partner-summary-stock-1080.png). A feliratok elférnek, a fő műveletek olvashatók; a görgetett összesítő mind a hét változatot mutatja. Hét változatlan kép a korábban átnézett rögzítéssel byte-azonos; a többi végső képet külön megnyitottuk.

A megszakadt natív transportpróbák és a korábbi sikertelen böngészős előkészítések nem számítanak sikeres ellenőrzésnek. A tömör JSON-ok minden állítást, hibát, buildhash-t és az utolsó állapotot megtartják; a nyers állapotminták a megnevezett helyi Builds könyvtárakban maradnak.

## Forrás és hatókör

[Baseline-forrás](baseline-source.json), [végső build-forrás](source.json). A HEAD a korábbi main; a feladat változtatásai az azonosított munkafában voltak. A megmaradó idegen anyag-/projektbeállítások és felhasználói mappák nincsenek a feladat commitjában. A runtime-azonosítók 0–15 változatlanok, az új változatok 16–22. A régi készlet a saját beszerzési költségét viszi tovább; nincs visszamenőleges átárazás. A kedvezmény utáni egységköltség egész forintos, csak a végső beszállítói fizetés kerekedik 5 Ft-ra, külön UI- és ledger-eltéréssel. Ez a felhasználónak jelzett implementációs alapfeltevés.

Saját diff- és vizuális review, nem független agent. Általános lemezes játékmentés és Pages-publikálás nem része a feladatnak.

# #22 – Egységes trafik UI

## Megvalósított működés

- Törtfehér papír, sötétzöld alap és bordó kiemelés; közös `ShopUi` tipográfia, panel- és gombstílusok. A fő overlay-ek 1280×720 virtuális vásznon, arányosan skálázódnak. Az IMGUI és a fizikai monitor meglévő működésére épít, új UI-függőség vagy jelenet-újragenerálás nélkül.
- Kompakt nap/fázis/kassza/műszakidő sáv. Az aktuális fázis teendője megmarad; az általános mozgássúgó az első mozgás után eltűnik. A célpontnál csak az aktuális művelet és szükséges tárgykezelés jelenik meg, az átállított billentyűk szerint.
- Egy vevőpanel: név, a tényleges párbeszéd teljes mondata, csoportosított rendelés, átadott/kért mennyiségek, KÉSZ/HIÁNY jelzés és releváns válaszok. A már meglévő abszurd köszönések bekerülnek a rendelési mondatba. A téves áruval kapcsolatos megszólalást sem írja felül állandó generikus szöveg. Nincs új párbeszédmodal vagy automatikus világszünet.
- A darabszám a valódi átadott tárgyakat követi, felvételkor csökken, megszakításkor visszaáll. Sikeres fizetés után az elköszönés alatt is kész marad a rendelés, bár az eladott tárgyakat a játék eltávolította.
- A rövid értesítések hat másodperc után eltűnnek; az ismételt művelet újra láthatóvá teszi ugyanazt a visszajelzést. Az aktuális rendelés, fizetési hiba és fázisteendő továbbra is helyben elérhető. A megjelenítés nem módosít pénzt, készletet vagy műszakállapotot.
- Közös megjelenés a menükön, görgethető beállításokon, súgón, kártyás terminálon, készpénz-/érmefizetésen, kávéválasztón, TobaccOS-on, árakon, esti kereskedőn és az átrendezésen. A fizikai monitor sötétzöld/bordó színei összeillenek a képernyőpanelekkel. A 3D-kassza és kéz modellje nem cserélődik.
- Nyugtaszerű elszámolás: a nyereség, zárókassza és fő eredmények görgetés nélkül látszanak; a részletes készlet görgethető, a Következő nap/Új játék/Kilépés gombok rögzített területen maradnak. Negatív kasszánál az újrakezdés elérhető.

## Ellenőrzés

- Unity checkout: **234 sikeres állítás**, 12 suite, benne a 20 állításos `UiPresentationChecks`. Végleges jelentés: `Builds/Verification/20261104-115720-691/report.json`, tartós másolat: `unity-report.json`.
- Forráscommit: `8ddca88`; forráslenyomat: `f3ed4b6d5a29669c5c39dbcc3cf85d3dfcffd26748649ec65b64125a89df63e5`. A Unity-jelentés a commit előtt készült azonos tartalomról; a jelentés előtti HEAD `5ec9629`, a munkafa dirty volt. A meglévő, idegen lokális módosításokat megőriztük.
- A view-model próbái a valódi részleges átadást, a termék visszavételét/visszaállítását, készlethiányt, a készre fizetett rendelést, az értesítés lejáratát/ismétlését, a billentyűkiosztást és a WebGL fixture által használt JSON-átadást ellenőrzik. A korábbi pénz-, kassza-, készlet-, ajtó- és átrendezési próbák is sikeresek.

- Teljes helyi WebGL-regresszió: **73 sikeres ellenőrzés, 0 konzolhiba**, `browser-full.json`; eredeti helyi jelentés `Builds/Verification/acceptance-1793793211952/report.json`. Build: 0 hiba, 0 figyelmeztetés, 21 076 704 bájt, befejezve 2026-11-04 11:52:58 UTC (`webgl-full-build.json`). A buildfájlok SHA-256 lenyomata a böngészős JSON-ban van.
- A teljes futás forrása `5ec9629`, lenyomata `405c7b30348e33d5ad5185259142b3b870fa3e8cff9a021bc02da697c1f3b617`. Ezt követően kizárólag a `DrawRecipeControls` három kávéválasztó gombja kapta meg a közös stílust és az Aktív feliratot. A gombok helye, fizikai/műveleti kapcsolata és játékszabályai változatlanok. Ez az érintett kávéfolyamat külön, friss buildből célzott újrapróbát kap; a többi folyamat forrása megegyezik a teljes futáséval.
- Reviewer: Codex, 2026-11-04. Átnézett nézetek: 1280×720 és 1920×1080. A hosszú magyar megszólalás, a rendelés és a válaszok nem fedik egymást; a részleges átadás láthatóan 1 / 2-re vált; a terminál hibaüzenete olvasható; a nyereség/zárókassza és a Következő nap gomb görgetés nélkül látszik. Az átkötött R gomb megmarad újratöltés után, majd az Alapértékek tényleges kattintással visszaállítja az E-t. A menük, fizetés és napváltás valódi böngészős bemenettel végigpróbálva.
- Vizuálisan javítva: a feliratok hover állapotban is megtartják a kontrasztot; a WebGL gammaeltérését a panel és gomb közös textúraalapú színkezelése megszünteti. A képek a javított forrásból készültek.

- Végleges kávégombok: **9 sikeres célzott böngészős ellenőrzés, 0 konzolhiba**, `browser-coffee.json`, helyi jelentés `Builds/Verification/acceptance-1793793758424/report.json`. Friss build: 0 hiba, 0 figyelmeztetés, 21 078 989 bájt (`webgl-build.json`), befejezve 2026-11-04 12:01:43 UTC. Mindhárom recept tényleges egérkattintással kiválasztható; a kész ital és alapanyaghiány billentyűs próbája is sikeres. Az átnézett `1280x720-coffee-monitor.png` képen a közös gombstílus és az Aktív jelzés olvasható.
- Vizuális/interakciós eredmény: **passed**. A tartós PNG-k a jelentés mellett találhatók; a JSON-ok megőrzik minden ellenőrzés eredményét, a helyi eredeti jelentés és a buildfájlok SHA-256 lenyomatát. A részletes diagnosztikai állapotsorozat csak a helyi jelentésben marad.

## Kiadás és függőség

Ág: `feature/22-trafik-ui`, a #19 (`feature/15-shop-layout`) nem mergelt forrására épül. Külön, egymásra épülő PR készül; nem duplázzuk meg a #15–#21 változtatások review-ját. Mainre merge és Pages-publikálás nem része ennek a kérésnek.

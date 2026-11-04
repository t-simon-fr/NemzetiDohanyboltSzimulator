# #31 – Tartalomfüggő polccímkék

A polcra nézéskor megjelenő raktári készletablak megszűnt. Az üres raktári polc nem mutat felvételi panelt; tényleges doboznál csak a használati utasítás marad. A raktári, bolti és pult alatti címkék üres tartalomnál üresek, feltöltve a valóban ott lévő árut jelzik. A bolti címke nevet és aktuális árat mutat; vegyes tartalomnál a termékneveket közös ár nélkül. Kézben tartott áru vagy üres karton nem számít polckészletnek. A szállítás, felvétel/visszarakás, kézi feltöltés, kiürítés, helycsere és új játék frissíti a feliratot, legfeljebb 0,25 másodpercen belül.

- Forrás: `57d0292`; lenyomat: `2a0e8578f6d26bab0beeccdd8ad171480f20f59d3c5cb05242fa3f25d9467d76`.
- Unity 6000.6.7f1 checkout: **342 sikeres állítás, 16 suite**, ebből 32 célzott címkeellenőrzés. [Jelentés](unity-report.json). Helyi eredeti: `Builds/Verification/20261104-165933-508/report.json`; a commit előtt, azonos forrástartalommal készült. Az idegen lokális beállításokat és fájlokat megőriztük.
- A célzott ellenőrzések a tényleges doboz- és termékkezelést, készlet/pénz megőrzését, más és vegyes árut, aktuális árat, fizikailag áthelyezett dobozt, üres kartont és új játékot is vizsgálják.
- WebGL: **11 sikeres célzott ellenőrzés, 0 konzolhiba**; valódi E-bemenet: érkező szállítmány, doboz felvétele, polcfeltöltés, visszarakás, termék felvétele/visszatétele és készlet/pénz megőrzése. [Böngészős jelentés](browser.json), [build](webgl-build.json): 0 hiba/figyelmeztetés, 21 096 852 bájt, 250,43 másodperc. A buildassetek SHA256-lenyomata a JSON-ban szerepel
- Vizuális/interakciós review: Codex, 2026-11-04, 1280×720 és 1920×1080; **passed**. Az üres címkék valóban üresek, a raktári készletablak nem jelenik meg; a megfelelő polcon jelenik meg a terméknév. A bolti név és ár elfér a meglévő keskeny címkesávon. A tényleges interakciókat a böngészős próba végigjárta.

Képek: [üres raktár](1280x720-shelf-warehouse-empty.png), [megérkezett doboz](1280x720-shelf-warehouse-delivered.png), [visszarakott doboz](1920x1080-shelf-warehouse-returned-large.png), [feltöltött cigarettapolc](1280x720-shelf-retail-stocked.png), [nagy nézet](1920x1080-shelf-retail-stocked-large.png).

Állapot: implementálva és ellenőrizve; a feladat lezárásához a kész PR merge-je is hozzátartozik. Pages-publikálás nem része a kérésnek. A korábbi játékrendszerekhez a Unity checkout regressziós profil futott; WebGL-ben az érintett polcfolyamat célzott próbája szerepel, nem új teljes regressziós futás.


A kezdeti böngészős próba hibás kamera-előkészítést talált: az állványba helyezett játékos kicsúszott a raktárból. A tesztkamera helye javítva és a folyosó talaja/célzása külön Unity-ellenőrzést kapott; az új buildből a teljes érintett folyamat sikeres. A játék működése eközben nem változott.

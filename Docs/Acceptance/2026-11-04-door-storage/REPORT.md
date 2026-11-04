# #20–#21 – Ajtótábla és pult alatti készlet

## Működés

- A nyitvatartási tábla az ajtó üvegezett részén van, az ajtószárnnyal együtt mozog. A korábbi fali példány nem marad a falon.
- A kétoldalas tábla kifelé zárt üzletnél ZÁRVA, nyitott üzletnél NYITVA feliratot mutat. Bentről a fizikai tábla másik oldala látszik. A feliratok nem tartalmaznak az ellenkező oldalról félrevezető E-s műveletnevet; az aktuális teendőt az interakciós sáv jelzi.
- E a táblán: nyitás/zárás. E az ajtó alsó részén: hazamenetel, csak üres, bezárt boltban. Az NPC-k továbbra is nyitják/csukják az ajtót; a táblát a járhatósági rács nem tekinti állandó akadálynak.
- A pult személyzeti oldalán öt csomagos, nyitott tárolórekesz van. A pult középső része valóban üreges, nincs előtte rejtett tömör collider. A vásárlói oldala zárt marad, a pult külső mérete és a közlekedési útvonal változatlan.
- A rekesz kizárólag Dnyipro csomagokat fogad. Kézzel feltölthető, E-vel kivehető és visszatehető; az áru a normál átadóhelyen/kasszán eladható. A rekesz nem önkiszolgáló polc, a vevők nem innen veszik le a terméket.
- A maradék áru megmarad a következő napra; új játék kiüríti a rekeszt. Nincs automatikus ajándékkészlet vagy új beszerzési szabály: az esti kereskedő továbbra is ötös csomagot ad a meglévő áron.

## Unity

- **214 sikeres állítás**, 11 suite; `Tools/Verify-Project.ps1 -Profile checkout`.
- Végleges forrás: `Builds/Verification/20261104-110319-922/report.json`; tartós másolat `unity-report.json`.
- Forráslenyomat: `7c94c75c30e44cff47ede377bf40989ee94fa7880f63ed0c8ead96291873a9b8`.
- Az új `DoorStorageChecks` 22 állítást ellenőriz: kifelé forduló felirat, ajtóval együtt mozgó tábla, szigorú készlettípus, kézi felvétel/visszarakás, valódi vevő és kasszázás, egyszeri eladás, másnapi megőrzés és új játék.
- Külön Game view-próba: a tényleges interaktor sugarával elérhető a rekesz, a csomag felvehető és visszatehető. Az utcáról is a táblát találja el a célzás, és a nyitott felirat helyesen olvasható.

## WebGL / vizuális próba

- Helyi acceptance build: **0 hiba, 0 figyelmeztetés**, 21 067 847 bájt; befejezve 2026-11-04 10:58:25 UTC (`webgl-build.json`).
- Célzott böngészős próba: **7 sikeres ellenőrzés, 0 konzolhiba**; `browser-door-storage.json`. Valódi E-bemenet veszi fel és teszi vissza a csomagot, megfordítja a táblát, majd visszazárja a boltot naplezárás nélkül.
- Reviewer: Codex, 2026-11-04. Az 1280×720 képeket átnéztem: a rekesz öt csomagja látható, nincs előttük tömör pult; az utcai nézetben a két állapot helyesen olvasható; bentről a kétoldalas tábla túloldala látszik. Tartós képek: `private-stock.png`, `door-outside-closed.png`, `door-outside-open.png`, `door-inside.png`.
- A build forráslenyomata `28854315a5338102512c309fb03ca9a4e99add6944d62164383d54328110443a` (`unity-build-source.json`, szintén 214 sikeres állítás). Ezután kizárólag a Node-próba két elvárása változott: a meglévő tárgykezelés felvételkor a forráshelyet lerakásig lefoglalja, ezért öt foglalt helyből egy kézben van; az ajtóhoz közelebb sétáló tesztjátékosnak újra az ajtó alsó részére kell céloznia a naplezáráshoz. A játékkód és a build változatlan maradt; a végleges tesztfuttatóhoz új Unity-jelentés készült. A böngészős JSON-ok tartalmazzák a buildfájlok SHA-256 lenyomatát.

- Teljes regresszió: **64 sikeres böngészős ellenőrzés, 0 konzolhiba**, `browser-full.json`; teljes helyi jelentés: `Builds/Verification/acceptance-1793790189171/report.json`. A vevő kinyitja/csukja az ajtót, a játékos a határon belül marad, zárás után az ajtó külön célzásával lezárható a nap és elindítható a következő. Az átrendezés, kassza, kávé, készlet, billentyűbeállítások és menü regressziós próbái is sikeresek.
- Vizuális és interakciós eredmény: **passed**. A teljes futás `1280x720-entrance-after-hours.png` képén a tábla alatt az ajtóra célozva a „Hazamegyek: nap lezárása” prompt látszik; a következő tényleges E eléri az elszámolást. A kisebb és nagyobb nézetben a meglévő ajtó/napváltás és átrendezés továbbra is működik. A képfájlok és JSON-ok tartós másolatai a célzott próba képeivel együtt találhatók itt.

Az acceptance fixture csak a helyzeteket készíti elő. A táblát és a rekeszt a böngészős próba valódi E-billentyűvel használja. A külső kamera kizárólag tesztpozíció: a játékos a normál játékmenetben továbbra sem sétálhat ki a nyilvános ajtón.

## Kiadás

A változtatások a #19 PR-ben, a `feature/15-shop-layout` ágon készülnek. Mainre merge és Pages-kiadás nem történt ehhez a csomaghoz.

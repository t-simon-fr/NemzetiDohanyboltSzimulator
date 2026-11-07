# #134 – Ellenőrzési jelentés

[Használat és végleges méretek](README.md). A felhasználó a pontosításnál 120×120 cm-es sarokelemet választott: mindkét fal mentén egy normál polcnyi szélesség, két 50 cm mély csatlakozó oldal, öt azonos szintű/vastagságú polclap. A valódi mesh és a rendező kontúrja közös profilból származtatott arányokat használ. Ár és kapacitás változatlan.

## Natív ellenőrzés

[Végső teljes kör](unity.json): **1162 sikeres állítás / 48 suite**, 63,19 s. A corner-wall suite 46 állítása ellenőrzi a 120×120-as befoglalót, két 50 cm-es valódi mesh-élt, polcszinteket/vastagságot, két egymásra merőleges normál polc fal-/szomszédillesztését, a forgatott profilt, pontos 18500 Ft-os közös terhelést, készletazonosságot, valódi NPC-termékmegközelítést mindhárom bútoron, átlós ütközési távolságot, napváltást, eladást és resetet. A többi suite az általános pénz/készlet/átrendezés/útvonal/UI regresszió.

A korábbi célzott kör 207 sikeres állítást adott. Az első teljes futás az Editor vezérlőkapcsolatának időkorlátja miatt nem lett sikeres; az Editor nem válaszolt a szabályos Play-leállításra sem. A saját ellenőrző folyamat újraindult, a lemezen lévő fájlok és idegen módosítások megmaradtak. A runner a Play-futásban engedélyezi az Application.runInBackground kapcsolót; ez nem PlayerSettings-módosítás. A végső friss teljes kör ezután sikeres, a tooling 15 önellenőrzése is sikeres. A korábbi megszakadt kör nincs passként beszámítva.

[Natív Game view](native-joined-shelves.png), [review-metaadat](native-review.json): 1918×879, GUI-val együtt. A két normál polc és a sarokelem csatlakozó lapjai hézagmentesek és azonos szintűek; a két fal menti hátfal folytonos. Ez API-val előkészített modell-review, nem natív egérinterakció. Reviewer: Codex saját képi review.

## WebGL

[Végső WebGL-build](build.json): **0 hiba / 3 figyelmeztetés, 503,15 s**. Egy korábbi fordítás eredmény nélkül félbeszakadt; ezt nem számítjuk sikeres buildnek. A végső eredmény friss időbélyegét az új indítás UTC-időpontjához ellenőriztük. A build előtti forráslenyomat egyezik a sikeres natív körével.

[Sarok és két csatlakozó polcsor](browser.json): **15 sikeres ellenőrzés**, [készletes átrendezési regresszió](browser-arrangement.json): **15 sikeres ellenőrzés**, mindkettő 0 konzolhibával. Az indítási ellenőrzés átfed. Valódi egérrel/R-forgatással történt a sarokelem és mindkét normál polc lerakása, a közös 18500 Ft-os terv mentése, 3250 Ft-os sarokeladás tervezése/megszakítása/mentése. A második kör tényleges készletes mozgatást, elvetést, hűtőmozgatást és ajtós napváltást ellenőrzött. A fixture a kezdőállapotot és a célpontok vetítését készíti elő, a vizsgált műveleteket a játékosbemenet végzi.

Codex saját képi review: [összeillesztett előnézet 720p](1280x720-joined-corner-720.png) és [1080p](1920x1080-joined-corner-1080.png). Mindkét 50 cm-es csatlakozás folytonos a normál polcokkal, a 120×120-as sarokformát és a befelé mutató nyilakat helyesen rajzolja. A fő műveletek és az összesítés mindkét nézetben elérhetők/olvashatók; 720p-ben a katalógus görgethető. A JSON-ok minden checket/hibát/buildhash-t megtartanak, az ismételt állapotokból csak az utolsót; a teljes nyers minták a megnevezett helyi Builds könyvtárban maradnak.

## Forrás és hatókör

[Build forráslenyomat](source.json). A HEAD az előző main, a feladat módosításai az azonosított munkafában voltak. A négyszögű bútorok foglalása változatlan. A sarokelem navigációja a levágott sarok síkját is figyelembe veszi, azonos 29 cm-es biztonsági távolsággal; a lerakási foglalás továbbra is teljes befoglaló. Saját diff- és képi review, nem független agent. Pages-kiadás nincs.

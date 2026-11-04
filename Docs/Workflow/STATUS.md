# Aktuális állapot

A GitHub issue-k az elsődleges feladatlista. A `Docs/CONTINUE.md` régebbi részei történetiek.

- #2–#8: implementáció, Unity-integrációs és helyi WebGL-bemeneti ellenőrzés kész. Részletek: [elfogadási jelentés](../Acceptance/2026-11-03/REPORT.md).
- Aktuális checkout profil: **469 sikeres állítás**, 19 csoport. #37: 9 kézinterakciós + 12 aprópénzes + 15 készpénzes WebGL-próba, 0 konzolhiba; [jelentés](../Acceptance/2026-11-04-customer-hand/REPORT.md). #35: 15 készpénzes + 12 aprópénzes WebGL-ellenőrzés sikeres, 0 konzolhiba; [jelentés](../Acceptance/2026-11-04-forint/REPORT.md). #33: 15 készpénzes + 17 kártyaterminál WebGL-ellenőrzés sikeres, 0 konzolhiba; 1280×720 és 1920×1080 képek átnézve. A korábbi csomagok eredményei a saját jelentéseikben szerepelnek.
- Javítva az elkészült kávé téves hiányjelzése és a HUD-on ismétlődő vevői mondat.
- #11: Misi és Nóra saját karakterkoncepciója, abszurd személyisége és dialógusterve elkészült a `docs/11-character-concept` ágon. A 3D-modellek és játékbeli dialógus-integráció még nincs implementálva.
- A kassza/kéz grafikai átdolgozását a felhasználó félretette. Az ajtó/tábla alapú napváltás kiadva (#13); a pult alatti csempészáru (#20) és az ajtóra tett kifelé olvasható tábla (#21) implementálva és ellenőrizve, kiadás nélkül.
- Az előző kiadás Pagesre került: forrás `ef5da37`, publish `29af415`. A #13 változtatás azóta kiadva: forrás `6c39f37`, publish `bfd4519`. A helyi acceptance WebGL-build külön tesztplayer; a shipping buildben nincs tesztprobe. Publikálás csak külön kérésre.
- 2026-11-04: #19 és #26–#29 függőségi sorrendben mainre mergelve, végső merge: `5e4f21c`. A main fájltartalma megegyezett az ellenőrzött feature-ágéval. Állandó szabály: a kész, ellenőrzött PR-eket külön rákérdezés nélkül mergeljük; Pages külön kérésre.

## Parancsok

```powershell
pwsh -File Tools/Checks/WorkflowRunnerChecks.ps1
pwsh -File Tools/Verify-Project.ps1 -Profile checkout
# Sikeres helyi acceptance build után:
node Tools/Verify-Acceptance.cjs
```

Workflow: [WORKFLOW.md](../WORKFLOW.md). Böngészős előfeltételek: [Acceptance/README.md](../Acceptance/README.md). Minden új futás saját jelentést készít; az itt hivatkozott eredmények csak a jelentésben azonosított tartalomra érvényesek.

## #13 – Ajtó és műszakkezelés

- Implementálva a feature/13-door-day-flow ágon: NPC-k által mozgatott fizikai ajtó, játékost blokkoló bejárat; E-s nyitvatartási tábla; kézi naplezárás az ajtónál.
- Üdvözlő és külön vevői párbeszédmodal eltávolítva; T: helyettesítés, X: elutasítás a HUD mellett.
- Az üres zárt bolt automatikusan esti állapotba kerül, könyvelési zárás csak távozáskor történik.
- Unity: 145 sikeres állítás, nyolc suite. Helyi WebGL: 14 sikeres ellenőrzés, 0 konzolhiba. Vizuális próba: passed, 1280×720 és 1920×1080. [Tartós jelentés és képek](../Acceptance/2026-11-04/REPORT.md).
- Mainre mergelve és Pagesre publikálva: `6c39f37` / `bfd4519`; élő indulás ellenőrizve.

## #15 – Alaprajz és átrendezés

- Felhasználó által jóváhagyott implementáció; kapcsolt csomagok: #16, #17, #18.
- A feature/15-shop-layout ágon elkészült a bal oldali pult, a kezdő területen belül maradó bejárat, a kartonos személyzeti útvonal, a berendezések adatmodellje és az A* útkeresés.
- A TobaccOS-ból felülnézeti átrendezés indulhat; polc/hűtő, forgatás, előnézet, közlekedési ellenőrzés, mentés/megszakítás és következő napi megőrzés.
- Unity: 192 sikeres állítás. Teljes helyi WebGL: 58 sikeres ellenőrzés, 0 konzolhiba; külön 15 ellenőrzéses átrendezési próba a kisebb nézetben. Vizuális próba: passed, 1280×720 és 1920×1080. [Jelentés](../Acceptance/2026-11-04-layout/REPORT.md).
- Mainre mergelve a #19 PR-rel. Pagesre még nincs kiadva.

## #20–#21 – Pult alatti készlet és ajtótábla

- A #19 PR-ben, a `feature/15-shop-layout` ágon. A tábla az üvegezett ajtóval együtt mozog, E-re megfordul; kifelé NYITVA/ZÁRVA a tényleges nyitvatartásnak megfelelően. E az ajtó alsó részén külön naplezárás.
- Öt személyzeti, kizárólag Dnyipro csomagot fogadó hely a pult valóban üreges közepén; kézi feltöltés, felvétel/visszarakás, normál kiszolgálás, másnapi megőrzés és új játék ellenőrizve.
- Unity: 214 sikeres állítás, 11 suite. Teljes helyi WebGL: 64 sikeres ellenőrzés, 0 konzolhiba. Külön célzott próba: 7 sikeres ellenőrzés. Vizuális és interakciós eredmény: passed, az új elemek 1280×720 nézetben átnézve. [Jelentés és képek](../Acceptance/2026-11-04-door-storage/REPORT.md).
- Mainre mergelve a #19 PR-rel. Pagesre még nincs kiadva.

## #22 – Egységes trafik UI

- `feature/22-trafik-ui`, a nem mergelt #19 ágára építve. Közös törtfehér/sötétzöld/bordó stílus, kompakt műszaksáv, valós átadást követő vevőpanel, teljes megszólalás és releváns válaszok, lejáró értesítések, követett billentyűkiosztás.
- Menük, terminál, TobaccOS, kávé, árak, esti kereskedő és átrendezés egységesítve. Nyugtaszerű elszámolás, fix Következő nap gomb.
- Unity: 234 sikeres állítás. WebGL: 73 teljes + 9 célzott ellenőrzés, 0 konzolhiba. Vizuális/interakciós eredmény: passed, 1280×720 és 1920×1080. [Jelentés és képek](../Acceptance/2026-11-04-ui/REPORT.md).
- Mainre mergelve a #26 PR-rel. Pagesre még nincs kiadva. A #23 sprint/ugrás, #24 fizikai terminál, #25 kávégép-kijelző részletei alább.

## #23–#25 – Mozgás és fizikai eszközök

- Implementálva külön, egymásra épülő ágakon a #26 UI-PR fölött: `feature/23-player-movement` → `feature/24-physical-card-terminal` → `feature/25-coffee-machine-display`.
- Mainre mergelve: [#27 mozgás](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/27), [#28 terminál](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/28), [#29 kávégép](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/29).
- Konfigurálható Shift-sprint és Space-ugrás; valódi POS-számgombok és kijelző; a kávégépen három recept, főzés/hiány/kész ital állapot és tényleges csészeátvétel. Nincs kettős könyvelés, duplafőzés vagy kattintás átadása a világba.
- Unity: 310 sikeres állítás. Natív Game view-k és sugaras vezérlési próbák kész; helyi WebGL: 113 teljes + 19 célzott sikeres ellenőrzés, 0 konzolhiba. Vizuális és interakciós próba: passed, 1280×720 és 1920×1080. A közben talált szünet/kurzorfókusz miatti megálló főzés javítva és újrapróbálva. [Jelentés](../Acceptance/2026-11-04-device-controls/REPORT.md).
- Mainre mergelve. Pagesre még nincs kiadva.

## #31 – Polccímkék készletablak nélkül

- A ránézésre felugró raktári készletlista eltávolítva; az üres polc címkéje és felvételi panelje üres. A bolti, raktári és pult alatti címke a tényleges tartalmat követi: érkezés, felvétel/visszarakás, feltöltés, más/vegyes áru, aktuális ár és kiürítés.
- Unity: 342 sikeres állítás, ebből 32 célzott ellenőrzés. WebGL: 11 sikeres célzott próba, 0 konzolhiba; vizuális/interakciós eredmény passed mindkét felbontásban. [Jelentés és képek](../Acceptance/2026-11-04-shelf-labels/REPORT.md). Pagesre nem került ki.

## #33 – Saját kasszamodell és közeli fizetési nézet

- Eredeti Blender-modell, külön mozgó pénzfiók, nagy döntött kijelző, működő parancsgombok és magyar SDF-feliratok. A kamera a modellhez és nyitott fiókhoz igazodik; kilépéskor és fizetéskor a hely/forgás/FOV visszaáll.
- Unity: 389 sikeres állítás, ebből 47 új kasszaellenőrzés. Helyi WebGL: 15 készpénzes + 17 terminálpróba, 0 konzolhiba; vizuális és interakciós review passed mindkét felbontásban. [Jelentés, képek és modellforrás](../Acceptance/2026-11-04-modern-register/REPORT.md). Pagesre nem került ki.

## #35 — Hivatalos magyar forint

- Az MNB változatlan képei mind a 12 címlethez, kétoldalas bankjegyekkel és pénzdarabokkal; közös megjelenítés a fiókban, a visszajáró tálcáján és a vásárló kezében. Forrásmegjelölés a H műszaksegédben; eredeti források és SHA-256 dokumentálva.
- Unity: 448 sikeres állítás, ebből 59 célzott forintpróba. Helyi WebGL: 15 készpénzes + 12 aprópénzes próba, 0 konzolhiba. Vizuális és tényleges interakciós review passed 1280×720 és 1920×1080 felbontáson. [Jelentés és képek](../Acceptance/2026-11-04-forint/REPORT.md). Pagesre nem került ki.

## #37 — Játékos felőli kéznézet és közvetlen interakció

- Természetes, játékos felőli kamera világ szerinti felfelé iránnyal, a játékos felé olvasható pénzdarabokkal. Tenyér/ujjak/pénz célzására E vagy bal kattintás nyitja az aprópénzválogatást; a nyitó kattintás nem választ ki pénzt. Teljes kameravisszaállítás, hatótáv/foglalt kéz/eltűnő kéz védelme ellenőrizve.
- Unity: 469 sikeres állítás; helyi WebGL: 9 kézinterakciós + 12 aprópénzes + 15 készpénzes ellenőrzés, 0 konzolhiba. Vizuális és tényleges interakciós próba passed mindkét felbontáson. [Jelentés és képek](../Acceptance/2026-11-04-customer-hand/REPORT.md). Pagesre nem került ki.

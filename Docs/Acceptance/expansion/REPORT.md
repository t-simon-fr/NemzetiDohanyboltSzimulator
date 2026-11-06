# #123 – Üzlet- és raktárbővítés

Használat, méretek, árak, modulhatárok és kompatibilitás: [architektúra és alaprajz](../../Architecture/Expansion/README.md).

## Megvalósítás

Négy üzlet- és négy független raktárszint, összesen hat vásárlási lépcső. A bolti terület 5/7/9/11 × 6,3 m; a raktár 3,5 × 4, 3,5 × 5, 5 × 5 és 5 × 6 m. Az első bolti lépcső nyitja az önkiszolgálót/hűtőt. Új oldalt kapott a TobaccOS fejlesztési nézete a raktárnak és a négy külön vásárolható modulnak. A bővítések megőrzik a meglévő pultot, bejáratot, hátsó ajtót és készletet.

A régi hosszú polc eredeti slotjai három, külön rendezhető egységbe kerülnek. Az új bolti, keskeny, kétoldalas és raktári polcok a megvett helyiségben rendezhetők; a berendezési nézet mindkét helyiséget mutatja. Rács: 5 cm, térképjel: P / H / R, teljes név és kijelölés az oldalsávban. Az ütközés, polchozzáférés és pult–raktár–hátsó ajtó útvonal ellenőrzött. Nincs új termékcsalád vagy reputáció ebben a csomagban; Pagesre nincs kiadva.

## Natív ellenőrzés és forrás

- Teljes stabil alap: [1043 sikeres állítás / 45 suite](unity.json), 2026-11-06. Forrás `fcd09d5`, [baseline forrás](source-baseline.json).
- Az első [helyi WebGL-kör](browser-baseline.json) 32 ellenőrzése sikeres, 0 konzolhiba; [baseline build](build-baseline.json). A képeken a keskeny tervmezőkben tördelődött a teljes bútornév. Saját diff-review megmutatta, hogy az induláskor inaktív kezdő polcok címkéi és a kétoldalas polc második oldali címkéi hiányozhatnak.
- A címke-/nézetjavítás `386f31f`, a végső elhelyezési javítás `733dc1a`. Rövid tervjelölések, inaktív polcok címkéinek felépítése, oldalanként csoportosított árcímke és jobb vizuális fixture-nézet. A pontosan érintkező polcoknál 1 mm számítási tolerancia védi a 3 × 1,2 = 3,6 m elhelyezést; valódi átfedést továbbra is tiltunk. [173 friss célzott állítás](unity-final.json): expansion 98, arrangement 27, shelf-labels 32, floor-plan 16. A változatlan többi suite a fenti teljes baseline-ból megtartott; nem állítjuk, hogy a teljes 1043-as kör a végső javításon újra lefutott.
- [Végső buildhez rögzített forrás](source.json). Az ezt követő változás a böngészős futtatóban az új polcmérethez igazított célzás és a raktár mentési forgatókönyve; a player C#-tartalma nem változott. Ezek a JS-próbák a helyi player mellett futnak, a player újrabuildelését nem igénylik.

A natív `expansion` suite mind a 16 szintpárban ellenőrzi a méretet és alap útvonalakat; a szintenkénti alternatívákat, kezdő keskeny polcot, árakat/fedezetet, felső szinthatárt, négy modult, tényleges karton dokkolását, címkéket, megszakítást, korábbi bútorpozíciók megőrzését és az új játékot. A meglévő arrangement/floor-plan próbák valódi termékazonosságot, NPC-hozzáférést, kartonos CharacterController-utat és napváltási megőrzést is ellenőriznek.

Néhány első runnerkérés a sikeres domain reload után megszakadt, teszt nélkül; a hivatkozott sikeres ismétlések frissek. A felhasználó külön anyag-/projektbeállításait és a Recovery/Data fájlokat megőriztük. Ezek a helyi forráslenyomat környezetének részei, nem kerülnek a PR-be.

## Vizuális / WebGL készültség

Végső böngészős kör: **36 sikeres expansion és 15 sikeres arrangement ellenőrzés, 0 konzolhiba**. [Bővítési próba](browser.json), [készletes átrendezés és napváltás](browser-arrangement.json), [sikeres build](build.json) (0 hiba / 3 figyelmeztetés). A két kör indítási ellenőrzése közös, az eredményeket nem állítjuk 51 különböző esetnek. Az átrendezési futtató régi polc-/hűtőkoordinátáit korrigáltuk; a javítások után a teljes célzott kör frissen sikeres. A player C#-kódja e futtatójavításoktól nem változott. Reviewer: Codex saját ellenőrzés; független review nem futott. A fixture csak állapotot és célzást készít elő; a vásárlás, szerkesztőnyitás, kijelölés, tiltott elhelyezés, mozgatás, mentés és kilépés valódi egér-/billentyűeseményeket használ. A 16 méretkombináció áttekintői külön állapot-előkészítések, nem vásárlást helyettesítő elfogadási bizonyítékok.

Az alaprajzi SVG a szabad elrendezést szemlélteti. A kezdő boltban a B alternatíva külön megvásárolt keskeny polcot mutat; a nagyobb szintek alternatívái a natív elhelyezésvalidátoron is átmentek. A folyosók 1,2/1,5 m-es szélessége tervezési cél; az engedélyezés a tényleges ütközőkkel/hozzáféréssel dolgozik, nem kötelező folyosósablonnal.


Vizuálisan átnézett végső nézetek: [kezdő alaprajz](1280x720-plan-0-0.png), [7 m-es bolt / hosszabb raktár](1280x720-plan-1-1.png), [9 m / széles raktár](1920x1080-plan-2-2-large.png), [11 m / nagy raktár](1920x1080-plan-3-3-large.png), [raktári állvány mentése](1280x720-warehouse-rack-saved.png), [bútorvásárlás](1920x1080-upgrades-large.png), [készletes polc mentése](1280x720-arrangement-saved.png), [hűtő és polc átrendezve](1920x1080-arrangement-large.png). 720p/1080p nézetekben minden fő vezérlő elérhető, a teljes nevek az oldalsávban olvashatók, a rövid tervjelek nem tördelődnek. A négy szint játékosnézete is átnézve, a végső validálási tolerancia nem változtatott a látványon. Eredmény: **passed**. Pages-publikálás külön kérésre.

A tartós böngészős JSON-bizonyítékok minden ellenőrzést, hibát, időpontot, buildhash-t és képlistát megtartanak; az ismétlődő állapotpillanatképekből csak az utolsót tartalmazzák. A teljes nyers futások helyi Builds/Verification artifactként megmaradnak.

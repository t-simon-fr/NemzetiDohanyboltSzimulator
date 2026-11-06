# #128 – Bútorvásárlás és átrendező

## Eredmény

[Viselkedés és határok](README.md): Fejlesztések/Bútorok fülek, közvetlen átrendező-katalógus, ismételt elhelyezés, tervből törlés, megszakítás, egyszeri mentéskori fizetés, közös 5 cm-es rácsigazítás. A külön +9 cigarettapolc-vásárlás kikerült a játékosi katalógusból; a történeti tárgy/API megmarad.

## Ellenőrzés

**Teljes Unity checkout: 1080 sikeres állítás / 46 suite**, [friss jelentés](unity.json). A pénzügyi esetek: ismételt elhelyezés, törlés/megszakítás, lottótartalék miatti teljes visszautasítás részvásárlás nélkül, pontos egyszeri költség és a fizikai pénztár egyezése. A raktári dokkok csak fizetés után regisztrálódnak; a hűtő egyszer vásárolható. Négy forgatás ugyanarra a rácsra igazodik. Meglévő készlet, útvonalak, méretbővítés és új játék is ellenőrzött.

A kezdeti teljes futást egy korábbi teszt fixture-izolációs hibája állította meg. Az entrance-day suite után törlésre váró, már inaktív Dnyipro-doboz a szinkron Editor-környezetben még nem semmisült meg; ezért a feature-batch új vásárlása tévesen előző szállítmányt látott. A [célzott diagnózis](fixture-diagnosis.json) igazolja: beszélgetés nyitva, elegendő pénz, régi doboz inaktív. A feature-batch előkészítése most befejezi ennek a kizárólag tesztbeli, már elrendelt törlésnek a végrehajtását. A javítás után a teljes 46 suite egy futásban sikeres. Player-kód ehhez nem változott.

**Végső WebGL-build: 0 hiba / 3 figyelmeztetés, 260,85 s.** [Build](build.json), [build előtti forrásazonosító](source.json). Két végső böngészős kör: [17 bútorvásárlási ellenőrzés](browser.json) és [15 készletes átrendezési ellenőrzés](browser-arrangement.json), 0 konzolhiba. A két kör közös indítási elemeket tartalmaz; nem állítjuk, hogy 32 különböző eset.

A fixture csak pénzt/kiinduló állapotot és célpontokat készít elő. Tényleges egér-/billentyűbemenet vásárolja a területet, nyitja a katalógust, forgatja és rakja le a polcokat, törli a terv tételét, szakítja meg és menti az elrendezést, görget a hűtőhöz, zárja a napot. Ellenőrzött a rácshoz igazodó előnézet, tiltott bejárat, ismételt elhelyezés, pontos vételár, újranyitáskor tulajdon, valamint a készletes polc és a hűtő napváltás utáni helye.

## Vizuális review

Reviewer: Codex saját review, nem független agent review. Az első képi körben a túl erős rácsot és a 720p oldalsáv katalógus/visszajelzés átfedését javítottuk. A végső képeken mindkettő rendezett, a fő műveletek elérhetők:

- [Fejlesztések](1280x720-developments.png), [Bútorok](1280x720-furniture-catalog.png).
- [Vásárlási terv 720p](1280x720-purchase-plan-720.png), [1080p](1920x1080-purchase-plan-1080.png).
- [Görgetett katalógus 720p](1280x720-catalog-scrolled-720.png): az utolsó hűtőkártya is kattintható. [Teljes katalógus 1080p](1920x1080-catalog-complete-1080.png).
- [Mentett polc játékosnézetben](1280x720-arrangement-saved.png), [mentett készletes polc és hűtő alaprajza](1920x1080-arrangement-large.png).

A natív ScreenCapture két kísérletben nem adott képet a jelenlegi Editor-környezetben; ez nem szerepel sikeres natív vizuális próbának. A fenti képek és valós interakciók a WebGL-célplatformról származnak. A böngészőjelentések minden ellenőrzést/hibát/buildhash-t megtartanak, az ismétlődő állapotmintákból csak a végsőt. A teljes nyers jelentések helyben, a bennük megnevezett Builds/Verification könyvtárakban maradnak.

A forrásazonosítók az adott futás előtti munkapéldányra vonatkoznak: a HEAD még a korábbi main, a módosított kód a munkafában volt. A végső build után csak tesztfixture, böngészős görgetési szekvencia és dokumentáció változott; a player C#-kódja azonos. A teljes Tools-t is tartalmazó natív lenyomat ezért eltérhet a build előttitől. Idegen helyi módosításokat nem állítottunk vissza és nem commitoltunk.

Pagesre nincs kiadva; publikálás külön kérésre.

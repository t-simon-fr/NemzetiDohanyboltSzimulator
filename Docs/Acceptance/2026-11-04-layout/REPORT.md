# #15 – Bal oldali pult és üzletátrendezés

Kapcsolt munkacsomagok: #16 alaprajz, #17 berendezések/útkeresés, #18 kezelőfelület.

## Működés

- A pult és a hozzá tartozó teljes kassza/monitor/átadóhely a bal fal mentén, jobbra néző kiszolgálással áll. A cigaretták a játékos mögötti bal falra kerülnek.
- A nyilvános ajtó jobbra tolódott, de teljes szélességében a kezdő üzlettéren belül marad. A raktár felőli pultvégnél a játékos kartonnal is kijuthat a vásárlótérbe.
- A jobb oldali bővítés nem mozdítja el a pultot vagy ajtót, és nem telepít új L-pultot. A kávéállomás a hátsó személyzeti részen kap helyet.
- TobaccOS → Üzletfejlesztés → Üzlet átrendezése. Nyitás előtt vagy üres boltban, zárás után használható; tárgyat előbb le kell tenni, futó főzést meg kell várni.
- Felülnézeti előnézet, 0,25 m rács, egérrel elhelyezés, Q/E-vel 90 fokos forgatás. Zöld/piros visszajelzés konkrét hibával; bal kattintás rögzíti az előnézetet, jobb kattintás/Esc elveti az aktuális előnézetet. A külön Mentés alkalmazza a teljes tervet, a Mégse eldobja az összes nem mentett változtatást.
- Az önkiszolgáló polc és a megvásárolt hűtő mozgatható. A pult, cigarettapolc, ajtók és személyzeti útvonal fix; ezt a felület jelzi.
- A világ és minden időzítő szünetel szerkesztés közben. Az előnézet nem változtatja meg a valódi bútorokat, készletet vagy pénzt. A mentés együtt mozgatja a tárolóhelyeket és az azokon lévő termékeket.
- A vevők ugyanazzal a járhatósági ráccsal és A* útkereséssel közlekednek, amellyel az elrendezés ellenőrzése is történik. A raktár/személyzeti sáv nem NPC-rövidítés.
- A következő nap megőrzi az elrendezést és a készletet. Új játék visszaállítja az alaphelyzetet.

## Technikai határok

Az alaprajz a meglévő, szerkesztett jelenetet futáskor igazítja át; nem generáljuk újra és nem írjuk felül a jelenetfájlt. A berendezések stabil azonosítót és külön hely/forgatás adatot kapnak. A kávégép korábbi képkockánkénti visszapozicionálása megszűnt. A magasban lévő látványgeometria csak az átrendezési nézetben rejtőzik el, a kamera és a rendererek kilépéskor visszaállnak.

A mentés itt a futó játékon belüli elrendezést jelenti. Böngészőfrissítésen vagy alkalmazás-újraindításon átívelő teljes játékmentés nem része a csomagnak. Új berendezéstípusokat és szabad pultmozgatást sem vezetünk be.

## Unity-ellenőrzés

- `Tools/Verify-Project.ps1 -Profile checkout`: **192 sikeres állítás**, tíz suite.
- Végleges jelentés: `Builds/Verification/20261104-102102-025/report.json`, tartós másolata `unity-report.json`.
- Forráslenyomat: `5413a726b27fedfa9bab0927b59658c6e368092482af1c65b841a625fa2236db`.
- `FloorPlanChecks`: 16 állítás, valódi CharacterControllerrel és kézben tartott kartonnal végigjárt személyzeti/feltöltési útvonal.
- `ArrangementChecks`: 31 állítás, A* kerülőút/elválasztott tér, tiltott elhelyezések, foglalt gép/nyitott bolt/kézben tartott áru, teljes megszakítás, polccal együtt mozgó termékazonosság és következő napi megőrzés. Egy valódi NPC megkeresi az elforgatott polcot, felveszi az árut, eljut a kasszához, majd a megszakított vásárlás visszaadja az árut ugyanarra a polchelyre.

## Böngészős és vizuális ellenőrzés

- Sikeres helyi acceptance WebGL-build: **0 hiba, 0 figyelmeztetés**, 21 064 430 bájt (`webgl-build.json`).
- Teljes regresszió: **58 sikeres böngészős ellenőrzés, 0 konzolhiba**. Tartós eredmény: `browser-full.json`; teljes helyi napló: `Builds/Verification/acceptance-1793787985286/report.json`.
- Külön átrendezési próba a kisebb nézetben: **15 sikeres ellenőrzés, 0 konzolhiba** (`browser-arrangement.json`). Ezek a teljes csomaghoz képest ismételt esetek a másik felbontáson, nem 15 új különálló funkció.
- Reviewer: Codex, 2026-11-04. Vizuális és interakciós eredmény: **passed**. A TobaccOS gombja, a berendezésválasztás, Q/E forgatás, piros előnézet, mentés és megszakítás valódi egér/billentyűbemenettel próbálva. A felület és gombok olvashatók/elérhetők 1280×720 és 1920×1080 méretben. A kisebb nézetben az alaprajz is elfér; a polc rövid címkéje nem tördelődik olvashatatlanul.
- Átnézett, tartós képek: `layout.png`, `invalid.png`, `draft.png`, `layout-large.png`, `starter-entrance.png`. Az előnézet a berendezések alaprajzi helyigényét jelzi; a tényleges 3D-bútor csak a Mentés után kerül az új helyre.
- A végleges build után a forráslenyomat egyezik a fenti Unity-jelentéssel. A build fájljainak SHA-256 lenyomata a böngészős eredményekben szerepel. A tartós JSON-ok elhagyják a részletes diagnosztikai állapotsorozatot, de megőrzik az eredeti helyi jelentés lenyomatát és minden ellenőrzés eredményét.

A külön acceptance player fixture-je készíti elő a helyzeteket és olvassa az állapotot; a kezelőfelületet valódi Chromium-egér- és billentyűesemények vezérlik. Az átrendezés a tényleges TobaccOS gombjáról indul. A böngészős jelentés a build fájljainak SHA-256 lenyomatát is tartalmazza, és konzolhiba esetén hibás eredményt ad.

Javított teszt-előkészítés: az új nyitvatartási táblát a bejárat mellett, a vevő útvonalán kívülről, átlósan kell megközelíteni; a régi tesztpozíció a bal oldali pult belsejébe került. A valódi játékos számára a tábla a vásárlótérből elérhető marad.

## Kiadás

Forráság: `feature/15-shop-layout`. A csomag nincs mainre mergelve vagy Pagesre publikálva. A korábban kiadott #13 ajtós verzió forrása `6c39f37`, Pages-commitja `bfd4519`.

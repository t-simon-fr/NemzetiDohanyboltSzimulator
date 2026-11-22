# Aktuális állapot

A GitHub issue-k az elsődleges feladatlista. A `Docs/CONTINUE.md` régebbi részei történetiek.

- #183 (Marika): MPFB-alapú női test + Mixamo-rig + 4 Blender-átvétel (séta/tétlen/bemutató/panasz), Karen-bob és szigorú szemöldök, testre festett időtlen ruha (bordó blúz, szénszürke nadrág), hangulat-szájak; nevek és játéklogika változatlan. Teljes `checkout` profil **60 suite / 1722 állítás, 0 hiba**; **30 célzott `customer-marika` állítás**; WebGL `marika-handover` zoom+panel PASS, E-átadás játék-szintű WebGL-hiba miatt bukik (**#184**, a régi modellre is). 720p/1080p képek átnézve. [Jelentés](../Acceptance/marika-mpfb/REPORT.md). Józsi/Laci a következő szeletekben. Pagesre nincs kiadva.

- #181 pilot (Pisti): Blender-szkripttel generált sztereotip vásárlómodell (34 part, rig, séta/tolvaj-animációk, hangulat-szájak), változatlan játéklogikával. Teljes `checkout` profil **59 suite / 1692 állítás, 0 hiba**; **25 célzott `customer-pisti` állítás**; **4 `pisti-handover` + 8 dialógus + 10 UI WebGL-ellenőrzés**, 0 konzolhiba; 720p/1080p képek átnézve. [Jelentés](../Acceptance/pisti-pilot/REPORT.md). Marika/Józsi/Laci a következő szeletekben. Pagesre nincs kiadva.

- #175: RPG dialóguspanel kamerazoommal + összesítő-felújítás — FOV-lerp (60→48), monogram-chip + hangulatcímke, gépelős mondat E-befejezéssel (akciófogyasztás nélkül), megújult összesítő. Teljes `checkout` profil **58 suite / 1665 állítás, 0 hiba**; **34 célzott `dialogue-zoom` állítás**; **8 dialógus + 10 UI WebGL-ellenőrzés**, 0 konzolhiba; 720p/1080p képek átnézve. [Jelentés](../Acceptance/dialogue-zoom/REPORT.md). Pagesre nincs kiadva.

- #174: változatos kérések és mondatok — személyiség-súlyozott extra sorok, 3-as mondatpoolok, determinisztikus seed; az extrák csak publikusan levehető áruk lehetnek. Teljes `checkout` profil **57 suite / 1633 állítás, 0 hiba**. Pagesre publikálva; lásd az aktuális kiadást.

- #169: Dnyipro kipakolása dobozból egyenként — egy E egy csomagot tesz szabad pult alatti helyre, a doboz maradéka és címkéje frissül; üres doboz eldobható. Teljes `checkout` profil **57 suite / 1637 állítás, 0 hiba**. Pagesre publikálva; lásd az aktuális kiadást.

- #168: kosz csak ott gyűlik, ahol járnak — a foltforrás az aktív vevők közeléből szemez, üres boltban a tartalékhelyekkel. Teljes `checkout` profil **57 suite / 1637 állítás, 0 hiba**. Pagesre publikálva; lásd az aktuális kiadást.

- #166: NPC útvonal simítás — a négy-szomszédos A* lépcsős cikkcakkja helyett greedy line-of-sight rövidítés a meglévő járhatósági predikátummal, azonos clearance-szel; a validáció nyers rácson marad. Teljes `checkout` profil **57 suite / 1633 állítás, 0 hiba**. Pagesre publikálva; lásd az aktuális kiadást.

- #164: 17:00 után nem jön új vendég — az automatikus érkezés leáll az üzlet órája szerint (1 valódi másodperc = 1 kijelzett perc), a bolt nyitva marad a bent lévők kiszolgálására, egyszeri üzenet jelzi a váltást. Teljes `checkout` profil **57 suite / 1629 állítás, 0 hiba**. Pagesre publikálva; lásd az aktuális kiadást.

- #162: LaVazze Barista kávégép — a runtime primitív gép helyett Blender-szkripttel generált modell (zöld test, krém/bordó előlap, iker-kifolyó, tálca, oldaltartály, dísz tekerő/gombok), azonos befoglalómérettel és interakciós pontokkal; feliratok átírva. **244 célzott natív állítás; 35 WebGL-ellenőrzés**, 0 konzolhiba, 720p/1080p képek átnézve. [Jelentés](../Acceptance/coffee-machine/REPORT.md). Pagesre publikálva; lásd az aktuális kiadást.

- #152/#154/#156: ellenőrzési hurok megerősítése és a #112 utóhatásainak rendezése — a runner múlékony transport-hibánál újrapróbálkozik és `transportRetries`-t rögzít, a checkek explicit out-változót használnak (`CS0165` ellen), a #112 után elavult `partner-integration` és `corner-wall` suite a katalógusból származtatja a várt értéket, a reputáció választék-metrikája pedig csak a ténylegesen hordozott jegy-/jégkrémkategóriát számolja. `WorkflowRunnerChecks`: 24 checks; teljes `checkout` profil **57 suite / 1626 állítás, 0 hiba**. Részletek a #152/#154/#156 issue-kban. A runtime-javítás (#156) publikálva; lásd az aktuális kiadást. A tooling (#152, #154, #160) csak helyi.

- #160: lefedettség — a `smoke` profil 16 suite-ra bővült (jegy, jégkrém, partner, sarokpolc, bútorplanning/editor, árszerkesztő, reputáció), hogy a katalógus- és bútorkind-eltolódás ne maradhasson észrevétlen; **16 suite / 516 állítás**. A #156 WebGL-bizonyítéka elkészült: **0 hiba / 3 figyelmeztetés, 286,5 s** build, **17 böngészős ellenőrzés, 0 konzolhiba**, a kezdő bolt választéka 100/100. [Jelentés](../Acceptance/reputation-choice/REPORT.md). Pagesre nincs kiadva.

- #112: fagyasztóláda és három saját márkás csomagolt jégkrém (Hókristály csoki, Jeges Pötty eper, Sarki Fény vanília) pult melletti ládakészletből, kézi átadással/leolvasóval, vegyes kosárral és készpénz/kártya fizetéssel; új Jégkrém árkategória. **9 suite / 244 sikeres célzott natív állítás; 11 WebGL-ellenőrzés**, 0 konzolhiba. Natív logika és 720p/1080p képek átnézve (a ládafelirat tükrözése képről jött, javítva). [Jelentés](../Acceptance/ice-cream/REPORT.md); [használat](../Architecture/IceCream/README.md). Pagesre publikálva; lásd az aktuális kiadást.

- #109: vonaljegy és 10 utazásos gyűjtőjegy pult mögötti készletből, kézi átadással/leolvasóval, vegyes kosárral és készpénz/kártya fizetéssel; új Jegyek árkategória. **6 suite / 146 sikeres célzott natív állítás; 10 WebGL-ellenőrzés**, 0 konzolhiba. Natív és 720p/1080p képek átnézve (az árkategória-sor javítása is képről jött). [Jelentés](../Acceptance/tickets/REPORT.md); [használat](../Architecture/Tickets/README.md). Pagesre publikálva; lásd az aktuális kiadást.

- #107: telefonfeltöltés a második hétfőn automatikusan és ingyen; Üreskom, Notyet tel, Two, készpénz/kártya, bizonylat, 5% jutalék és elkülönített szolgáltatói elszámolás. **57 suite / 1597 állításos baseline, 116 érkezési és 78 végső célzott állítás; 38 mobilos, 18 terminálos és 29 lottós WebGL-ellenőrzés**, 0 konzolhiba. A futások hatóköre és a 720p/1080p képek a [jelentésben](../Acceptance/mobile-topup/REPORT.md); [használat](../Architecture/MobileTopUp/README.md). A teljes heti/tutorial rendszer #139 marad. Pagesre nincs kiadva.

- #111: tea és forró csoki a meglévő kávégépen; Levélke tea/Kakaóka kakaópor külön beszerzéssel, szállítással és kézi feltöltéssel, közös tejkészlettel, öt fizikai receptgombbal és meglévő pohárátadással/fizetéssel. **55 natív suite lefedve: 1162 érintetlen baseline + 376 friss érintett állítás; 35 melegitalos, 19 kávégépes és 26 közös kiszolgálási WebGL-ellenőrzés**, 0 konzolhiba. A részleges profil és tesztadapter-javítások hatóköre a [jelentésben](../Acceptance/hot-drinks/REPORT.md); natív és 720p/1080p képek átnézve. [Használat](../Architecture/HotDrinks/README.md). Pagesre nincs kiadva.

- #122: négytényezős üzletreputáció, egyszeri és legfeljebb ±4 pontos napzárási változás, TobaccOS Ma/Lezárt nap nézet, napi összesítő, korlátozott érkezés-/kosárhatások és meglévő törzsvendégek kedvenccel/saját abszurd mondattal. **53 natív suite lefedve: 1261 érintetlen baseline + 99 friss érintett állítás; 15 reputációs, 45 partnerségi és 26 forgalmi/kiszolgálási WebGL-ellenőrzés**, 0 konzolhiba. A részleges profil és a csak acceptance kamerát érintő fix hatóköre a [jelentésben](../Acceptance/reputation/REPORT.md); natív és 720p/1080p képek átnézve. [Használat](../Architecture/Reputation/README.md). Pagesre nincs kiadva.

- #124: Hamster Energy, Loca Cola és Kevert / Műszakkezdő partnercsomagok, hét új termékváltozat, két egyező méretű márkahűtő és egy pult feletti tartó; csomagonként 20 000 Ft és 10% jövőbeli beszerzési kedvezmény, megőrzött régi egységköltségekkel. **1295 natív állítás / 51 suite baseline, 114 friss HUD-/napváltási és 34 végső célzási állítás; 45 + 6 + 15 + 4 WebGL-ellenőrzés**, 0 konzolhiba. Natív és 720p/1080p képek átnézve. [Jelentés](../Acceptance/partners/REPORT.md), [használat](../Architecture/Partners/README.md). Pagesre nincs kiadva.

- #134: 120×120×250 cm-es sarokelem, mindkét oldalon 50 cm mély csatlakozással a normál polcsorokhoz; egyező öt polcszint, közös mesh-/kontúrarány, átlós NPC-foglalás biztonsági távolsággal. **1162 natív állítás / 48 suite, 15 sarokcsatlakozási és 15 készletes átrendezési WebGL-ellenőrzés**, 0 konzolhiba; natív és 720p/1080p képek átnézve. [Jelentés](../Acceptance/corner-connector/REPORT.md). Pagesre nincs kiadva.

- #132: tényleges falfelülethez igazított rendező és illesztés, vásárlói iránynyilak és konkrét megközelítési hibák, ismételt lerakás megtartott forgatással; 6500 Ft-os, tíz készlethelyes Sarokpolc. **1148 natív állítás / 48 suite baseline, 12 friss UI-állítás a kontúr finomítása után; 12 sarok/fal, 16 bútoros és 15 készletes átrendezési WebGL-ellenőrzés**, 0 konzolhiba. Natív kép és 720p/1080p WebGL-képek átnézve. [Jelentés](../Acceptance/corner-wall/REPORT.md). Pagesre nincs kiadva.

- #130: a kezdőpolccal azonos megvehető normál/keskeny polc, több önálló hűtő, üres bútorok eladása 50%-os jóváírással, fal-/bútorillesztés és látható 10 cm-es rács, valódi modell-/méretelőnézet. **1112 natív állítás / 47 suite baseline, 159 végső célzott állítás; 16 új bútoros és 15 készletes átrendezési WebGL-ellenőrzés**, 0 konzolhiba, 720p/1080p képek átnézve. [Jelentés](../Acceptance/furniture-editor/REPORT.md). Pagesre nincs kiadva.

- #128: Fejlesztések/Bútorok menü, átrendezőbe épített katalógus és ismételt tervezés, mentéskori egyszeri fizetés, átdolgozott rács és közös 5 cm-es illesztés. **1080 natív állítás / 46 suite**, 17 vásárlási és 15 készletes átrendezési WebGL-ellenőrzés, 0 konzolhiba. A 720p/1080p felület átnézve. [Jelentés](../Acceptance/furniture-planning/REPORT.md). Pagesre publikálva; lásd az aktuális kiadást.

- #123: négy üzlet- és négy független raktárszint, vásárolható moduláris polcok és raktári állvány, közös átrendező. **1043 állításos / 45 suite baseline, 173 végső célzott állítás; 36 bővítési és 15 készletes átrendezési WebGL-ellenőrzés**, 0 konzolhiba. 720p/1080p képek átnézve. [Jelentés](../Acceptance/expansion/REPORT.md), [méretek és alaprajz](../Architecture/Expansion/README.md). Pagesre publikálva; lásd az aktuális kiadást.

- #125: Dnyipro csomagolás és kötözött karton frissítve. 49 célzott Unity-állítás és 5 helyi WebGL-ellenőrzés, 0 konzolhiba; 720p/1080p képek átnézve. [Jelentés](../Acceptance/dnyipro/REPORT.md). Pagesre publikálva; lásd az aktuális kiadást.

- #106: **Szerecsenjáték Zrt. Lottópont** elkészült: kaparósok, Ötöske, kézi kasszázás, későbbi NPC-beváltás és elkülönített üzemeltetői elszámolás. **953 natív állítás / 44 csoport**, 29 baseline és 8 végső célzott WebGL-ellenőrzés, 0 konzolhiba; 720p/1080p képek átnézve. [Közös jelentés](../Acceptance/lottery-point/REPORT.md), [használat és architektúra](../Architecture/LOTTERY_POINT.md). Pagesre publikálva; lásd az aktuális kiadást.

- #115/#116: Albro tej- és La Vazze kávéalapanyag saját modellel, olvasható csomagolással és játékos felőli kéznézettel; a gép E-vel és bal kattintással is feltölthető. **165 célzott Unity-állítás, 14 helyi WebGL-ellenőrzés, 0 konzolhiba**, 720p/1080p képek átnézve. [Közös jelentés](../Acceptance/ingredient-packaging/REPORT.md). Pagesre publikálva; lásd az aktuális kiadást.

**Aktuális Pages-kiadás:** a 8fe066e óta elkészült összes main-fejlesztés publikálva: #109 jegyek, #112 jégkrémek, #156 választék-metrika, #162 kávégép, #164 17:00 érkezésstop, #166 útvonalsimítás, #168 forgalom-kosz, #169 Dnyipro-kipakolás, #174 változatos kérések, párbeszéd-terv (#172/#173). Forrás `d9ee73f`, publish `99507dc`. A pontos commit Pages-státusza **built**. [Élő játék](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=d9ee73f), [kiadási jelentés](../Acceptance/2026-11-15-pages/REPORT.md). A korábbi kiadási megjegyzések történetiek.

**Előző Pages-kiadás:** a #128 bútorvásárlás, #123 üzlet-/raktárbővítés, #125 Dnyipro, #106 lottópont, #115/#116 Albro/La Vazze és minden korábbi main-fejlesztés publikálva. Forrás `8fe066e`, publish `0617fa1`. A pontos commit Pages-státusza **built**. [Élő játék](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=8fe066e), [kiadási jelentés](../Acceptance/furniture-pages/REPORT.md). A korábbi kiadási megjegyzések történetiek.

- #69–#74: a hat moduláris refaktor elkészült: beszerzés, készlet/szállítás, checkout, kávégép, nap/fókusz, bootstrap/erőforrás-élettartam. **852 natív állítás / 40 csoport**, közös WebGL-build, **123 különböző sikeres böngészős ellenőrzés**, 0 konzolhiba. [Közös jelentés](../Acceptance/modular-refactor/REPORT.md), [modulhatárok](../Architecture/REFACTOR_BATCH.md), PR #100–#105. A futtató három régi UI-célzási hibája javítva, a sikeres részeket megtartottuk. Pagesre publikálva; lásd a fenti kiadási jelentést.

- #98: az öngyújtók és a tartó is eltávolítva. **13 célzott natív és 4 helyi WebGL-ellenőrzés**, 0 konzolhiba; két felbontás képei átnézve. [Jelentés](../Acceptance/remove-lighter-display/REPORT.md), PR #99. Pagesre publikálva; lásd a fenti kiadási jelentést.

- #94 és #96: szabad raktárnyílás, az öngyújtós vitrin üvege eltávolítva. **13 célzott natív és 4 helyi WebGL-ellenőrzés**, 0 konzolhiba; két felbontás képei átnézve. [Közös jelentés](../Acceptance/stockroom-and-display-fixes/REPORT.md), PR #95/#97. Pagesre publikálva; lásd a fenti kiadási jelentést.

- #92: TobaccOS hover-kontraszt javítva: világos felületen sötétbarna, sötét felületen krém felirat minden állapotban. **12 célzott natív és 4 helyi WebGL-ellenőrzés**, 0 konzolhiba; két felbontás képei átnézve. [Jelentés](../Acceptance/hover-contrast/REPORT.md), PR #93. Pagesre még nincs kiadva.

- #87–#89: falusi trafik külső/belső arculat és minimalista TobaccOS hat alkalmazással; kategóriás, gépelhető ármező, két −/+ gomb, Enter/Esc, Shift és üreskategória-védelem. **766 állításos natív baseline, 98 végső érintett és 44 friss inputellenőrzés; 31 WebGL-ellenőrzés, 0 konzolhiba**. PR #90/#91, [közös jelentés és képek](../Acceptance/2026-11-05-trafik-refresh/REPORT.md), [jóváhagyott referenciák](../Design/TrafikRefresh/README.md). Pagesre még nincs kiadva.

- #77–#81: folyamatos véletlenszerű vevőérkezés, cigaretta csak a leolvasóra, három külön italátadóhely, sarokba támasztott és visszarakható seprű/ütő, lámpatestekhez kötött világítás és kijárható bejárat. **717 Unity-állítás / 31 csoport, 26 helyi WebGL-ellenőrzés, 0 böngészőhiba**; képek és tényleges interakció átnézve. [Közös jelentés](../Acceptance/2026-11-05-request-followups/REPORT.md), PR #82–#86. Pagesre még nincs kiadva.

- #60 és #61: [architektúra-felmérés és refaktorterv](../Architecture/MODULARIZATION.md), illetve [bevezetett AI-workflow és pilot](AI_WORKFLOW.md). A #69–#74 follow-up refaktorok azóta elkészültek; lásd a fenti közös jelentést. A workflow-hoz célzott suite-választás, Unity nélküli tervkimenet, futásidő-mérés és helyi dokumentációs linkellenőrző készült. 15 eszköz-önellenőrzés, 49 célzott Unity-állítás és 4 dokumentum linkellenőrzése sikeres; [bizonyíték](Evidence/2026-11-05-ai-pilot.json). A játék runtime-ja és a Pages-kiadás nem változott.

- #53–#59: az új feature-csomag implementálva, Unityben, helyi WebGL-ben és vizuálisan ellenőrizve. **650 Unity-állítás / 26 csoport, 30 közös és 6 végső rendelőellenőrzés, 0 böngészőhiba**. PR #62–#68 mainre mergelve, [közös jelentés és képek](../Acceptance/2026-11-05-feature-batch/REPORT.md). Pagesre publikálva; lásd a kiadási jelentést. A #60 és #61 spike nem része ennek a csomagnak.

**Aktuális Pages-kiadás (2026-11-05):** az összes új feature #53–#59 és a korábbi main-javítások kiadva. Forrás `d48500b`, publish `ec231b3`. [Élő játék](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=d48500b), [kiadási jelentés](../Acceptance/2026-11-05-feature-pages/REPORT.md). A GitHub Pages állapota built; a már sikeres helyi ellenőrzés után nem ismételtünk játékpróbát. A lenti korábbi kiadási megjegyzések történetiek.

- #47–#49: terminál automatikus bezárása sikeres fizetés után, zárt üres boltban minden fejlesztés vásárolható, raktári tej/kávé csomagfelvétel javítva. **621 sikeres Unity-állítás / 25 csoport**, tényleges natív E/egér/Enter és közös helyi WebGL-próba, 0 böngészőhiba; mindkét felbontás átnézve. [Közös jelentés](../Acceptance/2026-11-05-closing-fixes/REPORT.md), PR #50 / #51 / #52. Pagesre még nincs kiadva.
- #45: belépéskori tényleges kínálatból választott rendelések; nincs előzetes foglalás a sorban állóknak, így az utolsó darab sorban állás közben elfogyhat. Soha be nem szerzett vagy függő szállításban lévő áru nem kerül új rendelésbe. **570 sikeres Unity-állítás / 23 csoport**, 34 célzott eset, helyi WebGL és tényleges E-átadás, 0 böngészőhiba. [Jelentés](../Acceptance/2026-11-05-stock-orders/REPORT.md), [PR #46](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/46). Pagesre még nincs kiadva.
- #43: a kartonok szabadon áthelyezhetők a 24 üres fizikai raktári helyre; címke, felvétel, egyedi áru visszarakása a tényleges dobozt követi. Foglaltság/szállítás/megszakítás nem veszít vagy duplikál készletet. **536 sikeres Unity-állítás / 22 csoport**, tényleges E/egér és helyi WebGL ellenőrzés mindkét felbontásban, 0 böngészőhiba. [Jelentés](../Acceptance/2026-11-05-warehouse-storage/REPORT.md), [PR #44](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/44). Pagesre kiadva; lásd a fenti kiadási jelentést.
- #2–#8: implementáció, Unity-integrációs és helyi WebGL-bemeneti ellenőrzés kész. Részletek: [elfogadási jelentés](../Acceptance/2026-11-03/REPORT.md).
- Korábbi checkout profil (#37): **469 sikeres állítás**, 19 csoport. #37: 9 kézinterakciós + 12 aprópénzes + 15 készpénzes WebGL-próba, 0 konzolhiba; [jelentés](../Acceptance/2026-11-04-customer-hand/REPORT.md). #35: 15 készpénzes + 12 aprópénzes WebGL-ellenőrzés sikeres, 0 konzolhiba; [jelentés](../Acceptance/2026-11-04-forint/REPORT.md). #33: 15 készpénzes + 17 kártyaterminál WebGL-ellenőrzés sikeres, 0 konzolhiba; 1280×720 és 1920×1080 képek átnézve. A korábbi csomagok eredményei a saját jelentéseikben szerepelnek.
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

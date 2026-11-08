# #122 – Ellenőrzési jelentés

[Használat, mérés és hangolás](../../Architecture/Reputation/README.md). Pontosan a választék/készlet, tisztaság, árak és kosárhoz igazított kiszolgálási idő alapján, napzáráskor változó reputáció. Semleges 50-es indulás, ±4 pontos napi korlát, korlátozott forgalom-/kosár-/törzsvendég-hatás. Nincs közvetlen pénzjutalom vagy új feloldás.

## Natív ellenőrzés

A [közös profil első futásában](unity-baseline.json) **50 suite / 1261 állítás sikeres**, majd a korábbi partner-targeting előkészítése megállt. Ez a JSON összességében failed; nem teljes sikeres körként számoljuk el. A hiba oka a karakter talajra igazítása után megváltozott szemmagasságból, régi szögben futó tesztsugár volt. Kizárólag az acceptance kamera-előkészítő és annak fenntartott tesztje változott: a próba most valós karakterérintkezés után újracéloz, és külön megemelt padlón is ellenőrzi ezt. A reputáció játéklogikája és felülete a közös kör óta változatlan.

[Friss érintett kör](unity-final.json): **3 suite / 99 sikeres állítás**, 7,11 s; partner-targeting34, reputation-state27, reputation-integration38. Forráslenyomata egyezik a végső build előtti [forrással](source.json). A két kör együtt mind az 53 suite-ot lefedi; az érintetlen 50 suite eredményét explicit baseline-ként visszük tovább, nem ismételtük az egész profilt.

A reputációs próbák ellenőrzik az üres/kis mintás napot, négy tényezőt, egészséges alapárat és korlátozott olcsósítási előnyt, koszkor-integrálás lépésfüggetlenségét, egyszeri napzárást, értékhatárokat, hiány/ár/kiszolgálás egyszeri eseménybesorolását, elutasítással nem javítható átlagot, rögzített mintás kosárméreteket, valódi Unity-érkezési maggal több látogatót magas hírnévnél és tovább élő forgalmat alacsony hírnévnél. A háromfős sorkorlát és tényleges készletszabály megmarad.

Az integráció valódi készletet, függő szállítást, partnerfeloldást, kávé-/tejalapanyagot és lottót mintáz; a szünet/súgó, eszközhasználat és zárás külön időkezelését vizsgálja. Három tényleges NPC/nyugta/kártyás értékesítésből zár napot, megőrzi az értéket/utolsó jelentést, majd valódi ismert karakter kedvencével és abszurd mondatával ellenőrzi a visszatérést. A többi 50 suite a meglévő pénz/készlet/partnerek/lottó/kávé/nap/UI regresszió.

## Natív képi review

[Review-metaadatok](native-review.json): 1918×879, valódi Game view GUI-val. [Mai adatok](native-today.png), [összesítő](native-summary.png), [utolsó nap](native-last.png), [törzsvendég](native-regular.png). Reviewer: Codex saját képi review. A négy mutató, okok, átlagos várakozás és vezérlők elférnek; a tabok kiválasztása egyértelmű. Az összesítő fő művelete szabad, a háromtermékes abszurd rendelés olvasható.

Ez API-val előkészített natív ellenőrzés: valódi NPC/receipt/payment játékmeneti metódusokkal, nem natív egérinputként elszámolva. A későbbi acceptance kamera-fix csak a régi partnerpolc-célzást érinti; a fenti hírnév/UI képek és működés változatlanok. Az első vizuális kör után az aktív Ma/Lezárt nap tab jelzése és a mintaszám helyett mutatott átlagos várakozás pontosodott; a fenti képek már ezt a végleges nézetet mutatják.

## WebGL

[Végső közös WebGL-build](build.json): **0 hiba / 3 figyelmeztetés, 570,14 s**. A figyelmeztetések a Unity/Emscripten eszközkönyvtárából származnak. Egy végső build készült; a friss eredmény UTC-időbélyegét az indításhoz ellenőriztük. A végső natív kör, build előtti és böngészőpróbák utáni forráslenyomat egyezik; runtime vagy runner nem változott közben.

[Reputáció](browser.json): **15**, [partnerségi regresszió](browser-partners.json): **45**, [vevőforgalom és meglévő kiszolgálás](browser-arrivals.json): **26 sikeres böngészős ellenőrzés**, mindenhol **0 konzolhiba**, azonos player-asset SHA256 értékekkel. Az indítási és napváltási állítások átfednek.

A reputációs kör tényleges egérrel nyitja az új asztali alkalmazást, ellenőrzi a semleges/adat nélküli nézetet, billentyűzettel három fizikai nyugtához hagy jóvá 2200 Ft-os kártyás fizetést, majd a táblán/ajtón E-vel zár napot. Napközben az összérték50 marad, a lezáráskor54, a következő nap megtartja; a napi minták kiürülnek. A Ma/Lezárt nap tabok valódi kattintással olvassák vissza a megfelelő napot újabb jutalom nélkül. Az időmérés a terminál használata közben is fut.

A fixture a kezdőállapotot, NPC-belépést/várakozást és a meglévő készletből történő nyugta-előkészítést a rendes játékmeneti metódusokkal végzi; ezek nem valódi kézi árupickupként vannak elszámolva. A pénztári gombnyomás és a naplezárás valódi input. A végső törzsvendég-kép külön, explicit85-ös reputációval és ismert kedvenccel előkészített eset; nem állítjuk, hogy a tesztnap85 pontot adott. A seeded forgalom/kosárhatás és a kedvenchez kötött visszatérés a natív állapot-/integrációs körben is ellenőrzött.

A partnerségi kör ellenőrzi a csomagok egyszeri vásárlását, a hét új változat egér/E feltöltését, költségmegőrzést, NPC-útvonalat és tényleges terminálfizetést/napváltást. A requests-batch a véletlen folyamatos érkezést és zárás utáni leállást, scanner/kézi átadást, három kávét és pénz/készletegyezést is végigpróbálja.

Codex saját [képi review](visual-review.json), minden állapot mindkét felbontásban: [adat nélkül 720p](1280x720-reputation-empty-720.png) / [1080p](1920x1080-reputation-empty-1080.png), [mai megfigyelések720p](1280x720-reputation-today-720.png) / [1080p](1920x1080-reputation-today-1080.png), [napi összesítő720p](1280x720-reputation-summary-720.png) / [1080p](1920x1080-reputation-summary-1080.png), [lezárt nap720p](1280x720-reputation-last-720.png) / [1080p](1920x1080-reputation-last-1080.png), [törzsvendég720p](1280x720-reputation-regular-720.png) / [1080p](1920x1080-reputation-regular-1080.png). A feliratok és a hosszabb rendelés elférnek, a fő műveletek láthatók; új modal nincs.

A tömör JSON-ok minden ellenőrzést, hibát és buildhash-t megtartanak, az utolsó állapot releváns mezőivel; a reputációs jelentés a napzárási és következő napi állapotot külön is megőrzi. A teljes minták a megnevezett helyi Builds-könyvtárakban maradnak.

## Hatókör

A forrás HEAD-je a korábbi main, a fejlesztés a lenyomattal rögzített munkafában volt. A három új modul mellé vékony eseménykapcsolatok kerültek; nincs új mentési rendszer, karaktermodell vagy szolgáltatás. A helyi idegen anyag-/projektbeállítások és Recovery/Data mappák nincsenek a feladat commitjában. Saját diff-/képi review, nem független agent. Pages-kiadás nincs.

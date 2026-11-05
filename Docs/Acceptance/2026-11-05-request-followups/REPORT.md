# Új issue-pontosítások – 2026-11-05

Issue-k: #77–#81. PR-ek: #82–#86. Implementáció engedélyezve a #1 új pontjaihoz; Pages-kiadás nem része a csomagnak.

## Eredmény

- **Vevőérkezés (#77):** a nyitott boltban nincs fix napi limit. Az érkezések 12–28 másodperces véletlenszerű időközökkel folytatódnak; a sor továbbra is legfeljebb háromfős, a kínálatellenőrzés és az előzetes foglalás nélküli rendelés megmarad. Zárás megállítja az érkezést. Az AutomaticArrivals kapcsoló az explicit fixture-vevők elkülönítését teszi lehetővé.
- **Kézi lerakás (#78):** cigaretta csak a leolvasóra tehető, a vevői zónában nincs hozzá lerakókeret. A belső/NPC áthelyezés továbbra is működik. Az előre jelzett alapértelmezés szerint a kávé és a visszavett vevői ital közvetlenül a vevői zóna három külön helyére kerülhet, vonalkódolvasás nélkül. A kávéra vonatkozó kérdésre a megvalósításig nem érkezett külön válasz; ezt az alapértelmezést jeleztük a felhasználónak. A kibővített vevői alátét és a három hely a pult határain belül marad, külön a leolvasótól.
- **Eszközök (#79):** a seprű a raktári sarokba támasztva áll. A seprű és az ütő E-vel vagy bal kattintással a saját állandó helyére tehető vissza. A G-s eldobás és más helyről felvétel nem írja felül ezt az állandó helyet; a kéz egyszer ürül, nincs visszavételi kattintásból azonnali újrafelvétel.
- **Világítás (#80):** a korábbi cigarettapolc helyén maradt segédfény és a különálló régi területfények kikapcsolnak. A két fluoreszcens lámpatesthez saját, lefelé világító spot és shader-emission-variánstól független, világos csőfelület tartozik. A fények este is működnek. A ki/be kapcsolás csak tesztfixture-művelet a fényhatás megmutatására, nem új játékosi UI.
- **Bejárat (#81):** a fix láthatatlan fal helyett nem blokkoló interakciós trigger van. A játékos közeledése is nyitja a mozgó ajtót; ki és vissza lehet sétálni. A megvásárolt vészgomb valódi retesze megmaradt. A kisétálás nem zárja le a napot: zárt, üres boltban továbbra is explicit ajtóinterakció számol el egyszer.

## Ellenőrzés

- [Teljes natív regresszió](unity-full.json): **717 sikeres állítás / 31 suite**. Új területek: continuous-arrivals 6, tool-storage 20, fixture-lighting 12, walkable-entrance 11, scanner-placement 17; az alátét-próba 12 állításra bővült.
- A böngészős próba során a második csésze célzásánál kiválasztási trigger takarta a következő szabad helyet; a harmadik hely a kassza kijelzője mögé került. Javítottuk a célzást, a három hely a szabad pultfelületre került, és a fixture is a pult mögül céloz. Ezután [108 célzott állítás](unity-final-placement.json), új teljes natív kör és új WebGL-build készült. A teljes kör még nem commitolt végső tartalmat vizsgált, ezért a jelentések lenyomata az elsődleges azonosság, nem önmagában a HEAD mező.
- Végső runtime: `473b428`, [forrásazonosító](source.json). [Közös WebGL-build](webgl-build.json), majd [böngészős próba](browser.json): **26 sikeres ellenőrzés, 0 böngészőhiba**. Valódi CDP egér-, W/S/E- és összegbeviteli eseményekkel.
- A végső böngészős runner az eladásszám növekményét vizsgálja, mert a korábbi leolvasófixture már végrehajtott egy eladást. Ez csak Tools-változás; a WebGL runtime változatlan. Egy korábbi képmentési CDP-időtúllépés után friss böngészőben folytattuk.
- A böngészőben nyolcnál több látogató, valódi táblás zárás, tiltott vevői cigarettalerakás, sikeres szkennelés, seprű/ütő visszarakás, tényleges ki-/visszajárás és explicit naplezárás történt. Három külön receptű kávé három helyre került, majd egy 2500 Ft-os valós kártyajóváhagyás mindhárom fizikai csészét eladta; pénzfiók és készlet egyezett.
- A fizikát egy képkockában léptető ajtófixture a transzformációk szinkronizálását igényelte. A régi napciklusfixture-t biztos fedezettel és a mozgó ajtó próbáját ismert, távoli kezdőpozícióval tettük determinisztikussá. A visszavett ital régi tesztútvonala a leolvasó helyett a vevői zónába került. Fordítás után előfordult átmeneti pipeline-kapcsolatszakadás; ezekben a futásokban nem indult assertion, a későbbi sikeres jelentések szerepelnek itt.
- A scene-t nem generáltuk vagy mentettük újra, a felhasználói render-/projektbeállításokat és Recovery/Data fájlokat megőriztük. Az acceptance probe nem shipping kód; Pagesre nem publikáltunk.

## Vizuális és interakciós review

Reviewer: Codex, 1280×720 és 1920×1080. Natív Game view GUI-val rögzítve és átnézve; a végső böngészőképek átnézve, az interakciók tényleges bemenettel végrehajtva. A mennyezeti fények ki/be képe azonos kameraállásból mutatja a fal és pult megvilágításának különbségét; a seprű nem a közlekedőben áll. A cigarettás vevői felületen nincs zöld jelzés; a három ital külön helyet foglal és a leolvasó szabad marad.

- [Folyamatos érkezés](1280x720-continuous-arrivals.png).
- [Tiltott cigarettalerakás](1280x720-customer-zone-cigarette-blocked.png), [sikeres szkennelés](1920x1080-scanner-zone-separated-large.png).
- [Raktári sarok](1280x720-broom-corner.png), [visszarakási cél](1280x720-broom-return-target.png), [visszatett ütő](1280x720-bat-returned.png).
- [Fények nélkül](1280x720-fixture-lights-off.png), [lámpákkal](1280x720-fixture-lights-on.png), [nagy nézet](1920x1080-fixture-lights-on-large.png). [Natív világítás](native-fixtures-on.png), [natív összehasonlítás](native-fixtures-off.png).
- [Kint zárás után](1280x720-outside-after-closing.png), [explicit naplezárás](1280x720-explicit-day-end.png).
- [Három átadott kávé](1280x720-three-coffees-served.png), [nagy nézet](1920x1080-three-coffees-served-large.png).

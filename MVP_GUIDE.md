# Nemzeti Dohánybolt Szimulátor – játszható prototípus

Frissítve: 2026. november 2.

## Indítás

Nyisd meg az `Assets/Game/Scenes/ShopPrototype.unity` jelenetet, majd Play. A külön Windows-változat a `Builds/MVP/NemzetiDohanyboltSzimulator.exe` fájlból indul; az exe melletti adatkönyvtárak is szükségesek.

A jelenet már el van mentve. A **Nemzeti Dohánybolt > Build Compact Shop** menüpont újraépíti, előtte rákérdez a felülírásra. A korábbi **Build Graybox Shop Scene** menüpont is erre vezet.

## A kezdőbolt

A kezdő helyiség 5 × 6,3 méter. A pult és a pultlap a kézzel beállított új helyén maradt; a bővítés ezt a helyet őrzi meg. A hátad mögötti polcon három kitalált cigarettamárkából nyolc-nyolc doboz van. A pénztár 6000 Ft-tal indul. A bal és jobb oldal az eladó nézőpontjából értendő.

A pult bal oldalán a beszerzési monitor, jobb oldalán a pénztárgép áll. Középen a kék átadóhelyre kell tenni a vásárló által kért cigarettát. Az áru csak a sikeres fizetéskor fogy el a készletből.

## Vezérlés

| Gomb | Művelet |
|---|---|
| WASD + egér | Mozgás és körülnézés |
| E | Áru felvétele/lerakása, monitor, kassza, szállítódoboz vagy nyitástábla használata |
| B | A közeli rendelési monitor használata |
| F | A közeli pénztárgép használata fizetés közben |
| Q a doboznál | Kivett terméktípus váltása |
| O | Nyitás vagy korai zárás kérése |
| X | Az aktuális vásárló elküldése |
| H | Súgó |
| Escape | Kilépés az eszközből/súgóból, illetve kurzor elengedése |
| Bal kattintás | Eszköz gombja vagy címlet; szabad nézetben kurzor visszarögzítése |

A monitor és a kassza közelről, a játék világában használható. Használatukkor a kamera az eszközre áll, és a mozgás/vásárlói várakozás szünetel. A szállítmányok közben is megérkezhetnek. A kezdőképernyő, súgó és napi összesítés továbbra is külön képernyő.

## Fizikai kassza

Tedd a kért cigarettát a kék helyre, majd használd a pénztárgépet. Önkiszolgáló vásárlónál az áru már nála van. A kijelző külön mutatja a fizetendő összeget, a kapott pénzt, a kiszámított visszajárót és a már kivett pénz összegét. Például 2200 Ft-os árunál, 5000 Ft átadása után: VISSZAJÁRÓ 2800 Ft, KIVÉVE kezdetben 0 Ft.

A kassza házat, ferde billentyűzetet, kijelzőt és nyugtanyomtatót kapott. E/F használatakor a pénzrekeszes fiók 0,35 másodperc alatt kicsúszik az eladó felé; kilépéskor vagy sikeres fizetéskor visszacsukódik. A rekeszek teljesen nyitott fióknál kattinthatók.

Kattints egy rekesz bankjegyére, érméjére vagy hátlapjára: egy darabot veszel ki. A rekesz darabszáma csökken, a pénz a mellette levő tálcán jelenik meg. Üres rekeszből nem lehet kivenni.

- **VISSZA:** az utolsó kivett darab visszarakása.
- **TÖRLÉS:** az összes kivett pénz visszarakása.
- **VÁLTÁS:** egy nagyobb címlet felváltása kisebbekre, értékvesztés nélkül; a kiválasztott visszajárót előbb visszatölti.
- **ÁTADÁS:** a kiválasztott visszajáró ellenőrzése és a fizetés lezárása. Téves összegnél javíthatsz.

Címletek: 20 000, 10 000, 5000, 2000, 1000, 500 Ft-os bankjegyek; 200, 100, 50, 20, 10, 5 Ft-os érmék. A címletek és darabszámok működnek, a pénzgrafika színezett próbamodell, nem kész bankjegy- és érmetextúra.

A vásárlótól kapott pénz felváltható és használható a visszajáróhoz, de az eladás lezárásáig külön van nyilvántartva. Elküldéskor visszajár a vásárlónak; az önkiszolgáló áru is visszakerül az eredeti polchelyre. A kassza és a készlet csak sikeres fizetéskor könyvel el eladást.

## Monitor, rendelés és polcfeltöltés

A monitoron válaszd a termék háromdarabos csomagját. A költség azonnal levonódik, a szállítás nyolc másodperc. Az áru a jobb hátsó barna dobozba kerül. A dobozra nézve Q-val váltod a terméket, E-vel kiveszel egyet, majd a neki kijelölt üres polchelyre teszed.

A doboz kezdetben üres: a monitoron leadott rendelés nyolc másodperc után válik kivehetővé. A dobozra nézve készletlista mutatja a kiválasztott típust, a darabszámokat és az úton lévő rendeléseket. Ha a kiválasztott típus elfogyott, de másikból van áru, E a rendelkezésre álló terméket veszi kézbe.

A kézben tartott termék a látómező jobb oldalán, a vezérlősáv fölött látszik, a neve is megjelenik. A megfelelő szabad polc- és pulthelyeket zöld keret jelöli. Célozz a keretre vagy a közvetlenül előtte levő polcperemre, majd E-vel tedd le. Foglalt vagy más típushoz tartozó helyre nem lehet lerakni; az áru ilyenkor a kezedben marad.

Tele polc mellett is kiveheted az árut, és a dobozra nézve E-vel visszateheted. A doboz darabszáma csak sikeres kivételkor csökken, visszarakáskor nő.

A polchelyek típushoz kötöttek; a pult átadó- és ideiglenes tárolóhelye többféle árut fogad. A kezdőpolc tele van: az első utánrendelt cigarettákat az eladások után felszabaduló helyekre töltheted fel. A raktár típusonként legfeljebb 24 darabot fogad, a már megrendelt áruval együtt.

| Áru | Eladási ár | Darab beszerzése | 3 db rendelése |
|---|---:|---:|---:|
| Pannon Piros | 2200 Ft | 1500 Ft | 4500 Ft |
| Duna Kék | 2300 Ft | 1600 Ft | 4800 Ft |
| Alföld Arany | 2400 Ft | 1700 Ft | 5100 Ft |
| Buborék ásványvíz | 350 Ft | 150 Ft | 450 Ft |
| Menta rágó | 300 Ft | 120 Ft | 360 Ft |
| Pirkadat sör | 550 Ft | 260 Ft | 780 Ft |

Ezek a prototípus saját gazdasági beállításai.

## Boltbővítés és önkiszolgálás

Nyitás előtt a monitor **BOLTBŐVÍTÉS** gombja 2000 Ft-ért megnyitja a pult körül elérhető új, 4 × 6,3 méteres területet. A pult, a kassza és a monitor a helyén marad. A változás jelenleg azonnali, építkezési animáció nélkül.

Az új polcon három típusnak hat-hat helye van: víz, rágó, sör. A bővítés üres polcokat ad, az árut külön kell megrendelni és kézzel feltölteni. Ezután a nyolc látogatóból négy önkiszolgáló terméket keres: a polchoz megy, felvesz egyet, majd a változatlan helyű pénztárhoz jön. A cigarettát továbbra is az eladó adja ki.

Ha nincs készlet a keresett önkiszolgáló termékből, a vásárló a pulthoz jön a kérésével. Feltöltés után kézzel is kiszolgálhatod, vagy X-szel elküldheted. Egyelőre egy bővítési fokozat van; több fokozat és a kiegyensúlyozott árak későbbi feladatok.

## Műszak és elszámolás

Nyolc látogató érkezik, legfeljebb hárman várnak egyszerre. O-val korábban is kérhetsz zárást. A bent levőket még ki kell szolgálni vagy el kell küldeni; az utolsó távozás után elkészül az összesítés.

A napi működés 300 Ft. A nyereség az árbevétel mínusz az eladott áru költsége és a működés. A kasszapénzből a beszerzés és a beruházás is levonódik. A már kifizetett, úton levő áru záráskor a raktárba kerül. Ha a működés sem fizethető ki, az összesítés tartozást mutat és új játékot kér.

A következő nap megőrzi a kassza címleteit, a készletet és a bővítést a futó alkalmazáson belül. **Lemezes játékmentés még nincs**: bezáráskor a haladás elveszik. Az Új játék az eredeti kompakt boltot és kezdőkészletet állítja vissza.

## Az elrendezés kézi módosítása

Play módon kívül a Hierarchy **Counter Assembly** csoportját mozgasd vagy forgasd. A pult, pultlap, átadóhelyek, kisebb monitor, kassza és vásárlói megállási pont együtt követik. A kassza kb. 58 cm, a monitor kb. 62 cm széles.

A **Counter Fixtures** alatt a berendezések külön csoportban vannak: **Cash Register Assembly**, **Order Monitor Assembly**, **Change tray**. A kassza vagy monitor áthelyezéséhez az egész hozzá tartozó csoportot mozgasd, így a kijelző, fiók, kezelőszervek és kameranézet együtt maradnak.

A **Customer service point** a pultcsoporton belül a vásárló megállási helye; a sor ebből számolódik. Az **MVP Shop / Customer Routes** alatt a bejárat, kijárat és az önkiszolgáló átjáró pontjai is a Scene nézetben mozgathatók. Az útvonalpontok között továbbra is egyszerű egyenes szakaszokon mozognak a figurák: hagyj szabad helyet.

Ha külön a pultlapot rendezted át, a **Nemzeti Dohánybolt > Align Counter Equipment** újra a pultlapra helyezi és alaphelyzetbe rendezi a felszerelést, a falak és polcok újragenerálása nélkül. Ez a felszerelés saját kézi igazításait felülírja. A **Build Compact Shop** továbbra is az egész jelenetet újraépíti.

Ctrl+S-sel ments, majd Play-jel próbáld ki. A jelenet kézi változtatásai az önálló Windows-változatban új build után jelennek meg.

## Következő tartalmak

A figurák, termékek, pénzek és berendezések még próbamodellből állnak. Az ellenőr/személyi, készpénzes trükkök, hitelkérés, figyelemeltereléses lopás és a reggeli törzsvendégek további történetei későbbi fejlesztések. Az önkiszolgáló figurák rögzített útvonalpontokat követnek; fejlett tömegelkerülés még nincs.

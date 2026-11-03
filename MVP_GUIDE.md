# Nemzeti Dohánybolt Szimulátor – játszható prototípus

Frissítve: 2026. november 2.

## Indítás

Nyisd meg az `Assets/Game/Scenes/ShopPrototype.unity` jelenetet, majd Play. A külön Windows-változat a `Builds/MVP/NemzetiDohanyboltSzimulator.exe` fájlból indul; az exe melletti adatkönyvtárak is szükségesek.

A jelenet már el van mentve. A **Nemzeti Dohánybolt > Build Compact Shop** menüpont újraépíti, előtte rákérdez a felülírásra. A korábbi **Build Graybox Shop Scene** menüpont is erre vezet.

## A kezdőbolt

A kezdő helyiség 5 × 6,3 méter. A pult és a pultlap a kézzel beállított új helyén maradt; a bővítés ezt a helyet őrzi meg. A hátad mögötti polcon három kitalált cigarettamárkából nyolc-nyolc doboz van. A pénztár 6000 Ft-tal indul. A bal és jobb oldal az eladó nézőpontjából értendő.

A pult bal oldalán a beszerzési monitor, jobb oldalán a pénztárgép áll. A kassza felőli falon a RAKTÁR felirat jelzi az új átjárót. Középen a kék átadóhelyre kell tenni a vásárló által kért cigarettát. Az áru csak a sikeres fizetéskor fogy el a készletből.

## Vezérlés

| Gomb | Művelet |
|---|---|
| WASD + egér | Mozgás és körülnézés |
| E | Áru felvétele/lerakása, monitor, kassza, szállítódoboz vagy nyitástábla használata |
| B | A számítógép asztalának megnyitása a pult közelében, pontos célzás nélkül |
| F | A közeli pénztárgép használata fizetés közben |
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

## Monitor, raktár és polcfeltöltés

A monitor teljes háza, képernyője, talpa és állványa E-vel használható. **B** a pult közeléből, a kamerától legfeljebb 3,5 méterre levő monitor asztalát nyitja meg, célzás nélkül. Előbb tedd vissza a kézben levő árut vagy dobozt.

Az asztalon két kattintható alkalmazás van:

- **RENDELÉSEK:** hat márka, polckészlet, raktári összes darab és úton levő mennyiség. A háromdarabos csomag ára azonnal levonódik, nyolc másodperc alatt érkezik meg. A víz, rágó és sör boltbővítés után rendelhető.
- **BERENDEZÉS:** 24 000 Ft-os önkiszolgáló bővítés és 9000 Ft-os, kilenchelyes kiegészítő cigarettapolc vásárlása. Mindkettő egyszer, nyitás előtt vehető meg. A pult a helyén marad.

Az **ASZTAL** gomb visszalép az alkalmazásból. **KILÉPÉS / Escape** visszavisz a boltba. A vásárlók várakozása eszközhasználat közben szünetel, a szállítmányok megérkezhetnek.

Az áru a **kassza felőli oldalfalon nyíló raktárba** érkezik. A raktár az alapbolt része. Asztalok helyett márkánként feliratozott, többszintes fémpolcok vannak benne. Az első rendelés hozza létre az adott márka kartonját.

1. **E a raktári márkadobozon vagy a polcán:** a teljes kartont veszed fel, a benne levő mennyiséggel. Egyedi termék nem kerül a kezedbe.
2. Vidd a dobozt a megfelelő bolti polchoz. A zöld keretek mutatják az üres, azonos márkához tartozó helyeket.
3. **E egy kijelölt helyen:** pontosan egy termék kerül a fix helyére. A doboz a kezedben marad, a darabszáma eggyel csökken.
4. Nézz a következő üres helyre, majd ismét E. A foglalt, más márkához tartozó vagy fizikailag eltorlaszolt hely nem fogyaszt árut.
5. A teli, félig teli és üres doboz is visszavihető: **E a saját raktári márkapolcán**. A rossz márka polca nem fogadja el.

Ha dobozhordás közben ugyanabból a márkából új áru érkezik, külön dobozt kap a raktári polcon. A kezedben levő karton nem töltődik újra. Ha visszaviszed a régebbit, a szabad felső vagy alsó polcszintre kerül; ha ezek is foglaltak, a kartonok gyűjtőhelyére. A márkán belüli újabb rendelés a helyén maradó aktív szállítódobozhoz is hozzáadódhat. A raktári összesítés minden kartont, a kézben levőt is számolja; a típusonkénti korlát a függő rendeléssel együtt 24 darab.

A bolti polcról továbbra is egyedi terméket veszel fel E-vel a vásárló kiszolgálásához. Ezzel E a kék átadóhelyen lerakja az árut, vagy E a megfelelő raktári márkapolcon visszaviszi a készletbe. A pult átadóhelye nem dobozból való feltöltésre szolgál. A kezdő cigarettapolc tele van: előbb adj el róla, vagy vedd meg a kiegészítő polcot.

| Áru | Eladási ár | Darab beszerzése | 3 db rendelése |
|---|---:|---:|---:|
| Pannon Piros | 2200 Ft | 1500 Ft | 4500 Ft |
| Duna Kék | 2300 Ft | 1600 Ft | 4800 Ft |
| Alföld Arany | 2400 Ft | 1700 Ft | 5100 Ft |
| Buborék ásványvíz | 350 Ft | 150 Ft | 450 Ft |
| Menta rágó | 300 Ft | 120 Ft | 360 Ft |
| Pirkadat sör | 550 Ft | 260 Ft | 780 Ft |

Ezek a prototípus saját gazdasági beállításai.

## Üres kartonok és hátsó papírgyűjtő

Ha az utolsó terméket is kipakoltad, a karton **a kezedben marad**, és ÜRES KARTON feliratot kap. Visszaviheted a raktárba, vagy rögtön a hátsó kukához mehetsz vele.

A raktár hátsó nyílásán ki lehet menni a kis, körbekerített udvarra. A nagy zöld **PAPÍRGYŰJTŐ** kukára célozva E-vel kidobhatod az üres kartont. A kuka számolja az eltávolított dobozokat. Terméket vagy teli kartont nem enged kidobni.

Az üres karton visszavihető a saját márkájának raktári helyére. Ha közben új áru érkezett oda, a régi, üres csomagolás külön marad: a régi karton kidobása az új készletet nem érinti. A raktár és a kuka állapota napváltáskor megmarad, új játéknál visszaáll.

## Boltbővítés és önkiszolgálás

Nyitás előtt a monitor **BERENDEZÉS** alkalmazásának **ÖNKISZOLGÁLÓ BŐVÍTÉS** gombja 24 000 Ft-ért megnyitja a pult körül elérhető új, 4 × 6,3 méteres területet. A pult, a kassza és a monitor a helyén marad. A változás jelenleg azonnali, építkezési animáció nélkül.

Az új polcon három típusnak hat-hat helye van: víz, rágó, sör. A bővítés üres polcokat ad, az árut külön kell megrendelni és kézzel feltölteni. Ezután a nyolc látogatóból négy önkiszolgáló terméket keres: a polchoz megy, felvesz egyet, majd a változatlan helyű pénztárhoz jön. A cigarettát továbbra is az eladó adja ki.

Ha nincs készlet a keresett önkiszolgáló termékből, a vásárló a pulthoz jön a kérésével. Feltöltés után kézzel is kiszolgálhatod, vagy X-szel elküldheted. Egyelőre egy bővítési fokozat van; több fokozat és a kiegyensúlyozott árak későbbi feladatok.

A Berendezés alkalmazás külön 9000 Ft-os cigarettapolca a személyzeti rész jobb oldali falán jelenik meg, kilenc szabad hellyel, márkánként hárommal. Nem ad ingyenes termékeket. A feltöltött árut ugyanúgy felveheted a vásárló kiszolgálásához. A polc és a készlete napváltáskor megmarad, új játéknál visszaáll. A berendezés helye jelenleg rögzített; szabad játékbeli elhelyező mód még nincs.

## Műszak és elszámolás

Nyolc látogató érkezik, legfeljebb hárman várnak egyszerre. O-val korábban is kérhetsz zárást. A bent levőket még ki kell szolgálni vagy el kell küldeni; az utolsó távozás után elkészül az összesítés.

A napi működés 300 Ft. A nyereség az árbevétel mínusz az eladott áru költsége és a működés. A kasszapénzből a beszerzés és a beruházás is levonódik. A már kifizetett, úton levő áru záráskor a raktárba kerül. Ha a működés sem fizethető ki, az összesítés tartozást mutat és új játékot kér.

A következő nap megőrzi a kassza címleteit, a készletet, bővítést és megvásárolt polcot a futó alkalmazáson belül. **Lemezes játékmentés még nincs**: bezáráskor a haladás elveszik. Az Új játék az eredeti kompakt boltot és kezdőkészletet állítja vissza.

## Az elrendezés kézi módosítása

Play módon kívül a Hierarchy **Counter Assembly** csoportját mozgasd vagy forgasd. A pult, pultlap, átadóhelyek, kisebb monitor, kassza és vásárlói megállási pont együtt követik. A kassza kb. 58 cm, a monitor kb. 62 cm széles.

A **Counter Fixtures** alatt a berendezések külön csoportban vannak: **Cash Register Assembly**, **Order Monitor Assembly**, **Change tray**. A kassza vagy monitor áthelyezéséhez az egész hozzá tartozó csoportot mozgasd, így a kijelző, fiók, kezelőszervek és kameranézet együtt maradnak.

A **Customer service point** a pultcsoporton belül a vásárló megállási helye; a sor ebből számolódik. Az **MVP Shop / Customer Routes** alatt a bejárat, kijárat és az önkiszolgáló átjáró pontjai is a Scene nézetben mozgathatók. Az útvonalpontok között továbbra is egyszerű egyenes szakaszokon mozognak a figurák: hagyj szabad helyet.

Ha külön a pultlapot rendezted át, a **Nemzeti Dohánybolt > Align Counter Equipment** újra a pultlapra helyezi és alaphelyzetbe rendezi a felszerelést, a falak és polcok újragenerálása nélkül. Ez a felszerelés saját kézi igazításait felülírja. A **Build Compact Shop** továbbra is az egész jelenetet újraépíti.

Ctrl+S-sel ments, majd Play-jel próbáld ki. A jelenet kézi változtatásai az önálló Windows-változatban új build után jelennek meg.

## Vészgomb és bejárati retesz

Nyitás előtt a TobaccOS **Üzletfejlesztés** alkalmazásában **7500 Ft-ért** megveheted a vészgombot. A vásárlás után a pult személyzet felőli oldalán jelenik meg egy piros gomb. **E-vel reteszeled, újabb E-vel feloldod a bejáratot.** Áruval vagy kartonnal a kezedben is használható.

A zöld fény szabad, a piros reteszelt bejáratot jelez. Zárt redőnynél a vásárlók nem tudnak be- vagy kimenni; a bentieket továbbra is kiszolgálhatod. Záráskor a retesz automatikusan feloldódik. A megvásárolt fejlesztés a következő napra megmarad. Részletes leírás: [PanicButton.md](Docs/PanicButton.md).

## Új bolti feladatok és alkalmazások

A részletes funkciólista: [FeatureRelease.md](Docs/FeatureRelease.md).

- **G:** termék, teli vagy üres doboz, ütő, felmosó eldobása. Az eldobott tárgy megmarad és E-vel újra felvehető.
- **Felmosó:** E-vel felvehető a személyzeti részen. Nézz a koszfoltra, és tartsd nyomva a bal egérgombot kb. 1,5 másodpercig.
- **Baseballütő:** a pult mögött E-vel felvehető. Fizetés nélkül menekülő tolvaj közelében bal egérgomb: megállítás; a visszaszerzett áru a padlóról felvehető. A vészgomb lezárja előtte a bejáratot. A kijutó tolvaj készletveszteséget okoz.
- **Kávé:** az Üzletfejlesztésben 16 000 Ft-os kávégép vásárolható. A Kávé alkalmazásban 2000 Ft-ért tíz adag alapanyag rendelhető, nyolc másodperces szállítással. E a gépen: főzés, majd az elkészült pohár felvétele. A vevőnek a kék átadóhelyre tedd; ára 650 Ft.
- **Kamerák:** TobaccOS → Kamerák, majd a nézet kiválasztása. A bolt közben tovább működik.
- **Játékok:** Snake nyilakkal vagy WASD-vel, memóriajáték kattintással. Menü: játékválasztás; ASZTAL / Esc: visszatérés. A vásárlók közben érkeznek és várakoznak.
- **Aprópénz:** a tenyérből fizető vevőnél F a fizetés nézetére vált. Kattints az érmékre, majd a pontos összegnél az Átveszem gombra. A kiválasztás visszavonható.
- **Önkiszolgáló áru:** a vevő a külön szürke pulthelyre teszi. Tartsd szabadon ezt a helyet.

A bolt bővítése a teljes középső falat eltünteti, így egybenyílik a helyiség. A kávégép, a vészgomb, a polc, az alapanyag és a földre tett tárgyak napváltáskor megmaradnak. A játék bezárására továbbra is a fent jelzett mentési korlát érvényes.

## További tartalmak


A figurák, termékek, pénzek és berendezések még próbamodellből állnak. Az ellenőr/személyi, összetettebb készpénzes trükkök, hitelkérés és a reggeli törzsvendégek további történetei későbbi fejlesztések. Az önkiszolgáló figurák rögzített útvonalpontokat követnek; fejlett tömegelkerülés még nincs.

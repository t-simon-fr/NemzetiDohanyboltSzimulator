# Nemzeti Dohánybolt Szimulátor

## Jelenlegi MVP-állapot – 2026. november 1.

Az összefüggő MVP elkészült: nyitás előtti felkészülés, automatikus nyolcvásárlós műszak négy visszatérő karakterrel, sor, termékösszekészítés, címletes fizetés és visszajáró, rendelhető szállítmány, kézi polcfeltöltés, korai vagy automatikus zárás, elszámolás és következő nap.

A használat és a pontos szabályok a [MVP_GUIDE.md](../MVP_GUIDE.md) fájlban találhatók. A szürkedobozos látvány, a karaktermozgás, a tartós mentés és a későbbi ellenőri/hiteles/lopási helyzetek további fejlesztések. A következő lépés a teljes műszak játékosi kipróbálása és a tempó, gazdaság, kezelés és humor közös finomítása.

## A dokumentum célja

Ez a dokumentum a játék iránytűje. Új funkció, karakter, feladat vagy látványelem tervezésekor ellenőrizzük, hogy illik-e az itt leírt játékélményhez. A még ki nem próbált ötletek munkahipotézisek; a prototípus tapasztalatai alapján változhatnak.

## A játék röviden

**Műfaj:** egyjátékos, belső nézetű 3D boltos szimulátor, kisvárosi komédiaelemekkel.

**Alapötlet:** a játékos egy magyar kisváros trafikját vezeti. Készletet rendel, polcot tölt, vásárlókat szolgál ki, figyeli a pénzt és a bolt hírnevét. A visszatérő vendégeket idővel megismeri, a hétköznapi műszakok pedig sajátos, humoros történetekké állnak össze.

**A játékos élménye:** „Én tartom mozgásban ezt a kis boltot, és már tudom, ki mit fog kérni, mielőtt befejezné a mondatot.”

## Rögzített irányok

- A játék címe: **Nemzeti Dohánybolt Szimulátor**.
- A játék 3D-s szimulátor lesz.
- A humor fontos része a játék identitásának.
- A termékek fix polc- és pulthelyekre kerülnek, automatikus pozíció- és irányigazítással.
- A tempó alapvetően kezelhető és hétköznapi; időnként forgalmas, többfeladatos csúcsok gyorsítják fel.
- A boltos teendők és a vásárlókkal való találkozások egyaránt a játék középpontjában állnak.

## Következő fejlesztési irány – felhasználói jegyzet, 2026. november 1.

Az alábbi irányok a működő MVP következő átdolgozására vonatkoznak; még nincsenek megvalósítva.

- A pult bal oldalára játékbeli monitor kerül, amelyről kezelhetők a rendelések.
- A fizetési modal helyét a játékbeli pénztárgép veszi át. A játékos rendes magyar forintcímleteknek megfelelő bankjegyeket és érméket vesz ki a kasszából a visszajáróhoz.
- A kezdő bolt kisebb lesz, cigarettákkal feltöltött választékkal.
- Később a bolt területe növelhető, de a pult helye állandó marad.
- Az új polcokról a vásárlók önállóan gyűjtik össze vásárlásukat, és a változatlan helyű pulthoz mennek fizetni.

A felület átalakításánál a rendeléskezelő monitor és a fizikai kassza legyen a kiindulópont. A konkrét pénzkezelési interakció, a cigarettaválaszték és a bővítések mérete, sorrendje, ára a következő tervezési feladat.

## Munkahipotézisek, amelyeket később megerősíthetünk

- Első körben PC-re készül.
- Egyjátékos élmény.
- Belső nézetű, első személyű irányítás.
- Kitalált magyar kisvárosi környezet.
- Unity 6 és URP mint technikai alap.
- A termékek és márkák kezdetben fiktívek; a pontos választék még nyitott.

## Tervezési pillérek

### 1. Kézzel végzett bolti munka

A játékos érezze, hogy valóban a boltban dolgozik: odamegy a polchoz, feltölti az árut, a pultnál szolgál ki, és észreveszi, ha valami hiányzik. A rutin legyen könnyen érthető, az interakciók pedig gyorsak és kielégítőek.

### 2. Egyszerű, érdemi üzleti döntések

A készlet, a rendelés és a bolt fejlesztése hasson a következő műszakra. A játékosnak legyen oka mérlegelni, miből mennyit tartson, mire költsön, és hogyan rendezze be a kis üzletet. A gazdasági rendszer maradjon átlátható; ne váljon könyvelőprogrammá.

### 3. Ismerős, visszatérő vásárlók

A vásárlók ne csak véletlenszerű rendelésgenerátorok legyenek. Legyenek felismerhető szokásaik, beszédmódjuk és apró történeteik. A visszatérésük, a bizalmuk és a boltról alkotott véleményük adjon személyes folytonosságot a napoknak.

### 4. Nyugodt alap, időszakos pörgés

A műszak nagy része hagyjon időt a bolt működtetésére és a vásárlók megfigyelésére. Csúcsidőben több kérés, sor és bolti feladat kerülhet egymás mellé. A kihívás a prioritások megválasztása legyen, ne a folyamatos büntető időzítő.

### 5. Helyzetből fakadó humor

A humor a vásárlók sajátos szóhasználatából, félreértésekből, visszatérő poénokból, bolti hirdetésekből és a hétköznapi feladatok abszurditásából eredjen. A karakterek legyenek szerethetőek és emberiek, ne egydimenziós karikatúrák.

## Hangnem és témák

- Magyar kisvárosi mindennapok, apró helyi szokások és ismerős bolti helyzetek.
- Meleg, megfigyelő, néha száraz vagy abszurd humor.
- Visszatérő vásárlók és korábbi beszélgetésekre építő poénok.
- A komikum célpontja elsősorban a helyzet, a félreértés és a játékos döntése legyen; ne valós társadalmi csoportok vagy kiszolgáltatott emberek.
- A humor ne igényeljen aktuális internetes mémek ismeretét, hogy később is működjön.
- A dohánytermékek megjelenése a bolti szimuláció része; a játék ne romantizálja a függőséget, és ne adjon valós vásárlásra vagy megkerülésre használható útmutatást.

## Egy műszak alapköre

1. **Nyitás előtt:** készlet ellenőrzése, rendelés és polcok feltöltése.
2. **Műszak közben:** vásárlók fogadása, termékek megkeresése és értékesítése, sor kezelése, készlet figyelése.
3. **Záráskor:** bevétel és költségek áttekintése, eredmény megállapítása.
4. **Fejlődés:** a következő napra készülve új áruk, berendezés vagy bolti fejlesztés kiválasztása.

Az életkor-ellenőrzés egyszerű, egyértelmű játékmeneti interakció legyen ott, ahol a játékban szükség van rá; ne váljon a valódi szabályozás részletes szimulációjává.

## Későbbi, mindenképp tervezett játékelemek

Ezek fontos részei a teljes játéknak, de nem feltétlenül kerülnek bele az első prototípusba.

### Ellenőri látogatások és személyi igazolvány

Időnként ellenőr érkezik. A játékosnak el kell kérnie az igazolványt, amikor a játék szabályai szerint szükséges; ha ezt elmulasztja, az ellenőr bírságot szab ki. A szabály legyen előre megtanulható és a helyzetben egyértelmű, ne kiszámíthatatlan büntetés.

### Reggeli törzsvendégek

Legyenek visszatérő vásárlók, akik szinte minden reggel betérnek a legolcsóbb sörért vagy egy kisüveges, nagyjából 4–5 cl-es tömény italért, amit a szleng „kőműves Actimelként” is emleget. A visszatérő rendelés, a rövid párbeszédek és a köztük kialakuló ismerősség adja a humort és a karakterívet. A poén a sajátos rendelésből és a bolti rutinból jöjjön, ne önmagában a függőség kifigurázásából.

### Készpénz és visszajáró ellenőrzése

A játékosnak ellenőriznie kell, mennyit kapott, és helyesen kell visszaadnia. Némelyik vásárló siettetve vagy összezavaró módon adhatja át a pénzt; mások egyszerűen rosszul számolhatnak. A játékos figyelme és pontos kasszakezelése óvja a kasszát. Ez a viselkedés többféle karakterhez tartozhat, ne csak a reggeli törzsvendégekhez.

### Hitelt kérő vásárlók

Egyes vásárlók kérhetik, hogy a játékos írja fel a tartozásukat későbbi fizetésre. A hitel rövid távon segíthet megtartani egy törzsvendéget, de kockázatot jelent a kasszának. A visszafizetés és a bizalom kezelése későbbi műszakokhoz kapcsolódhat.

### Figyelemeltereléses lopási kísérlet

Vásárló kérhet még egy terméket, hogy a játékos elforduljon, majd fizetés nélkül távozhat. A játékosnak észre kell vennie a helyzetet, és össze kell vetnie a kiadott termékeket a fizetéssel. Később a bolt elrendezése vagy biztonsági fejlesztései segíthetnek az ilyen esetek megelőzésében.

Ezek a viselkedések ne egyetlen társadalmi csoporthoz kötődjenek. A vásárlók legyenek változatosak; a humor és a kihívás a konkrét bolti helyzetből, a játékos reakciójából és a karakterek egyediségéből fakadjon.

## Mi tartozik bele az első játszható változatba

- Egy kisebb, bejárható trafik.
- Első személyű mozgás és könnyen érthető tárgy-interakció.
- Néhány fiktív terméktípus, polc és készletfeltöltés.
- Vásárlók, akik rendelnek, várnak, fizetnek és reagálnak a kiszolgálásra.
- Alap kassza- és műszakvégi elszámolás.
- Néhány megkülönböztethető, visszatérő karakter és rövid párbeszédek.
- Legalább egy teljes, elejétől a végéig játszható műszak.

## Mi nem cél az első változatban

- Teljes, szabadon bejárható város.
- Többjátékos mód vagy online szolgáltatások.
- Összetett adózási, könyvelési vagy ellátásilánc-szimuláció.
- Folyamatos időnyomás és véletlenszerű katasztrófák.
- Részletes dolgozómenedzsment, mielőtt az egyjátékos bolti kör működik.
- Nagy mennyiségű tartalom a vásárlás–kiszolgálás–elszámolás alapjának kipróbálása előtt.

Ezek később újraértékelhetők, de csak akkor kerüljenek tervbe, ha erősítik a fő játékmenetet.

## Első prototípus sikerkritériuma

Az első prototípus akkor teljesíti a célját, ha a játékos egy rövid műszak alatt:

- megérti, hogyan mozog és hogyan lép kapcsolatba a bolttal;
- legalább néhány vásárlót kiszolgál;
- észreveszi a készlet és a kiszolgálás közötti kapcsolatot;
- kap egy-két természetes, emlékezetes humoros pillanatot;
- kíváncsi arra, milyen lesz a következő műszak.

## Döntési szűrő új ötletekhez

Minden új ötletnél kérdezzük meg:

1. Erősíti a bolti munkát, az üzleti döntéseket, a visszatérő vásárlókat vagy a helyzetkomikumot?
2. Hozzáad valami érdekeset egy műszakhoz, vagy csak több kezelendő rendszert jelent?
3. Megőrizhető mellette a nyugodt alaptempó és az olvasható humor?
4. Előbb ki tudjuk próbálni egy kis prototípusban?

Ha egy ötlet egyik pillért sem erősíti, vagy elvonja az időt az alap műszak kipróbálásától, egyelőre ne kerüljön be.

## Még nyitott tervezési kérdések

- Mennyire legyen realisztikus a látvány: stilizált, félrealisztikus vagy realisztikus?
- Pontosan mely cigarettatermékekkel és további árukkal töltsük fel a kisebb kezdőboltot?
- A következő verzió fizikai pénztárgépet és magyar címleteket használ; a konkrét kezelés és a vonalkódolvasás formája még nyitott.
- Milyen méretű és árú bővítési fokozatok épüljenek az állandó helyű pult köré?
- Milyen mértékben lehessen a párbeszédet befolyásolni?

Ezeket a prototípus és a későbbi közös tervezés során döntjük el.

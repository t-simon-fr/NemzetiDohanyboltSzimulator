# #13 – Ajtó, tábla és műszakváltás

## Játékos által látható működés

- A főmenü után a felkészülés azonnal játszható, üdvözlőmodal nélkül. A műszak órája áll.
- A bejárat melletti tábla E-re megfordul: ZÁRVA → NYITVA. Ez indítja a műszakot és a vendégek érkezését. A korábbi távoli O gyorsbillentyű megszűnt.
- A fizikai ajtószárny a közeledő NPC-kre nyílik, áthaladásuk után csukódik. A játékos útját külön, állandó collider zárja el, nyitott ajtónál is.
- A tábla ismételt használata bezár. Új vendég nem keletkezik, a meglévő vendégek kiszolgálhatók vagy elküldhetők. Nincs automatikus elszámolás az utolsó vendég után sem.
- Üres boltban esti állapot indul; a kereskedő külön „maradok” ablak nélkül elérhető. A könyvelés ekkor még nincs lezárva.
- A nap az ajtónál, üres kézzel, E-re zárható le. Csak ezután jelenik meg az elszámolás. A működési költséget egyszer vonja le; Enterrel a következő nap felugró üdvözlés nélkül indul.
- A külön vevői párbeszédmodal megszűnt. A rendelés mellett T jelzi a helyettesítő ajánlását, X az elutasítást. A súgó és a fizetési felület megmaradt.

## Unity

Reviewer: Codex, 2026-11-04.

- `Tools/Verify-Project.ps1 -Profile checkout`: **145 sikeres állítás**, nyolc suite.
- Jelentés: `Builds/Verification/20261104-082455-308/report.json` (tartós másolata: `unity-report.json`).
- Az új `EntranceDayChecks.Run` 15 feltételt ellenőriz, köztük a vendégek távozását, távoli/nyitott/bent tartott tárgyas naplezárás tiltását, esti árut és egyszeri költséget.
- `WorkflowRunnerChecks.ps1`: 7 sikeres ellenőrzés.

## WebGL és vizuális próba

Végleges böngészős jelentések:

- `entrance-browser.json`: **10 sikeres ellenőrzés**, 0 konzolhiba; nyitás, zárás, NPC-ajtó, bent maradás, mozgási korlát, E-s naplezárás, következő nap.
- `dialogue-browser.json`: **4 sikeres ellenőrzés**, 0 konzolhiba; T-válasz külön modal nélkül, elfogadott helyettesítés és elutasítás fizetés nélkül.
- `webgl-build.json`: sikeres build, 0 hiba, 0 figyelmeztetés, 21 019 504 bájt.
- Ellenőrzött forráslenyomat: `ce0d3b08cb1a016a156ce044d8ee0c56208ee624f2c5e9a2f2f5fb628d6b4244`; a végleges build után is egyezik a Unity-jelentéssel.

Vizuális eredmény: **passed**. Reviewer: Codex, 2026-11-04. A végigpróbált interakciók mellett átnézve az ajtó és a HUD 1280×720 és 1920×1080 méretben, az elszámolás és következő nap, valamint a rövid és többtermékes vevői rendelés. Az ajtó körvonala és kilincse látszik, a művelet az alsó sávban olvasható; az elszámolás gombjai látszanak; a háromtermékes szöveg és a T/X lehetőség nem lóg ki. Az esti ajtó sötét a meglévő éjszakai megvilágítás miatt. Tartós képek: `door.png`, `door-large.png`, `summary.png`, `dialogue.png`.

Helyi, külön `SHOP_ACCEPTANCE` build; a tesztfixture csak előkészíti a helyzetet és állapotot olvas. Az interakciókat valódi Chromium billentyű- és egéresemények végzik. Az NPC-ürítés a vendégek tényleges állapotgépét gyorsítva futtatja; a táblát és a kijáratot a teszt nem hívja közvetlenül.

Vizuális javítás: a közelről aránytalanul nagy világfeliratot eltávolítottuk az ajtóról. Az útmutatás az alsó interakciós sávban marad. A párbeszédteszt fixture-jében a korábbi kézi Tick mód helyett élő billentyűkezelés szükséges a T-válaszhoz.

## Kiadás

Ez a csomag forrásmódosítás és helyi ellenőrzés. Nem frissíti a GitHub Pages-oldalt. A korábbi, `ef5da37` forrású publikációt nem módosítja.

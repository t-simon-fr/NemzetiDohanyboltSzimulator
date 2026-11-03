# GitHub feature-csomag – 2026. november 3.

Az #1-es issue kilenc funkciója, a megvásárolható vészgomb és a bővítéskor eltűnő teljes középső válaszfal elkészült.

| Kérés | Megvalósított működés |
|---|---|
| Lopás és baseballütő | Az önkiszolgáló megnyitása után egyes vásárlók fizetés nélkül indulnak a kijárathoz. A pult mögötti ütőt E-vel lehet felvenni; közelről bal egérgomb állítja meg a menekülő tolvajt. A visszaszerzett áru a földre kerül és felvehető. A kijutó tolvaj készletvesztesége megjelenik a napi eredményben. |
| Számítógépes minijátékok | TobaccOS → Játékok: Snake nyilakkal vagy WASD-vel, illetve kattintható, nyolcpáros memóriajáték. A rekordok helyben megmaradnak. A bolt játék közben tovább működik. |
| Kosz és felmosás | Nyitvatartás alatt időnként kosz jelenik meg járható padlón, egyszerre legfeljebb négy folt. Felmosóval rá kell nézni, és a bal egérgombot kb. 1,5 másodpercig nyomva tartani. |
| Kávégép és értékesítés | Üzletfejlesztésben megvehető kávégép, külön Kávé alkalmazásban rendelhető tízadagos alapanyag. Nyolc másodperces szállítás, 3,5 másodperces főzés, ténylegesen hordozható pohár, pulton átadás és a meglévő kasszában fizetés. |
| Árhangolás | Önkiszolgáló bővítés: 24 000 Ft; extra cigarettapolc: 9000 Ft; kávégép: 16 000 Ft. A készletet továbbra is külön kell megvenni. A kávé 650 Ft, alapanyagköltsége 200 Ft/adag. |
| Kamerák | Két látható kamera a bejáratnál és az önkiszolgálónál. TobaccOS → Kamerák: váltás az élő képek között. A második nézet a bővítéssel nyílik meg. 480×270-es képek, legfeljebb 5 frissítés/mp; a bolt közben tovább működik. |
| Önkiszolgáló áru a pulton | A vásárló a sor elején a külön szürke pulthelyre teszi a valódi terméket. Fizetés eltávolítja az eladott darabot; megszakított vásárlás visszahelyezi a polcra, ha a helye szabad. |
| Érmék a tenyérből | Egyes kisebb értékű vásárlásoknál F / kassza a vásárló kinyújtott tenyerére fókuszál. Kattintással lehet kiválasztani és visszatenni az érméket, majd az Átveszem gombbal megerősíteni. Csak a pontos összeg fogadható el; a valóban átvett címletek kerülnek a kasszába. |
| G-vel eldobás | A termék, a teli vagy üres karton, az ütő és a felmosó is eldobható. Fizikával leesnek, újra felvehetők, és napváltáskor megmaradnak. A karton tartalma és a teljes készlet változatlan. |

## Két további kérés

A **7500 Ft-os vészgomb** egyszer megvehető az Üzletfejlesztésben. E a pult mögötti piros gombon lezárja vagy feloldja a bejáratot. Kézben tartott tárggyal is használható, a tolvajok sem tudnak áthaladni a redőnyön. Záráskor automatikusan feloldódik.

Az önkiszolgáló bővítéskor az **egész belső válaszfal**, a burkolata és ütközője is eltűnik. Az üzlet egy nagy, átjárható helyiség lesz. Új játék visszaállítja a kezdőfalakat.

## Ellenőrzés

A Unity tényleges jelenetén **21 ellenőrzéscsoport, 1105 sikeres állítás**, 0 sikertelen. Eredmény: `Builds/FeatureRelease/checks.json`. A két vegyes műszak valódi készletet rendel és helyez polcra, kávét főz és szolgál ki, önkiszolgáló árut tesz a pultra, érmékkel és hagyományos kasszázással fizettet. Az egyik műszakban a fizikai ütőtalálat állítja meg a tolvajt, a másikban a tolvaj elmenekül. Az érmék tényleges kamerából küldött kattintási sugarai, a felmosó célzása, a tárgydobás, a napváltás és a pénzügyi egyezőségek is ellenőrizve.

Új próbák: `FeatureReleaseChecks.Rules`, `WorldFeatures`, `CatchDay`, `EscapeDay`; az eredeti működési próbák az új árakhoz és fizetési szabályokhoz igazítva maradtak. A `ShopCheckFixtures` kizárólag az Editor-assemblyben ad tesztkészlethez szükséges pénzügyi kiinduló állapotot; nem része a WebGL-lejátszónak.

A játékból készült képek a `Builds/FeatureRelease/Preview` könyvtárban vannak. A WebGL böngészős ellenőrzése: `node Tools/Verify-WebGL.cjs --features`, éles ellenőrzésnél az oldal URL-jével. A forrás a privát repóban marad, a nyilvános `NemzetiDohanyboltSzimulator-WebGL` repó kizárólag a lefordított játékot tartalmazza.

## Megmaradó fejlesztési határok

A tárgyak és fejlesztések a futó játékon belül, napváltások között maradnak meg. Lemezes játékmentés továbbra sincs; a minijátékrekordok helyi PlayerPrefs-adatok. A vásárlók rögzített útvonalakon mozognak, a többtermékes kosarak és fejlett tömegelkerülés további munka. Az új kéz, kávégép, felmosó, ütő és kamerák a prototípus stílusába illő egyszerű 3D modellek.

## WebGL-kiadási ellenőrzés

A végleges WebGL-build sikeres: **0 hiba, 0 figyelmeztetés**, 445,869 másodperc, 20 835 352 bájt. Jelentés: `Builds/FeatureRelease/build-status.json`. A helyi Chromium-próba **11 képernyőképet** készített a betöltött játékról, az öt alkalmazásos asztalról, a valóban megérkező rendelésről, az árakról, a kávéfelületről, az élő kameráról, a Snake-ről és a kattintással felfordított memóriakártyákról. Böngészős hiba: **0**; napló: `Builds/Verification/local-console.json`. A forrás és a publikálás változatát a helyi `Builds/FeatureRelease/deployment.json` rögzíti.

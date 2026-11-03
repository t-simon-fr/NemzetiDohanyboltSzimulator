# Billentyűzet, alkoholos italok és szabad polcelhelyezés – 2026. november 3.

A GitHub #1 issue három új kérése elkészült: billentyűzetes kassza és monitor, Kevert és Kőfejtői alkoholos italok, illetve bármely termék elhelyezése bármely szabad bolti polchelyen.

## Kezelés

| Hely | Billentyű | Művelet |
|---|---|---|
| Kassza | Enter / numpad Enter | A pontosan összeállított visszajáró átadása |
| Kassza | Backspace / Delete | Utolsó darab vissza / teljes kiválasztás törlése |
| Kassza | 1–6 | 20 000, 10 000, 5000, 2000, 1000, 500 Ft kivétele |
| Kassza | Shift+1–6 | 200, 100, 50, 20, 10, 5 Ft kivétele |
| Vásárló tenyere | Tab / nyilak, Space | Érme megkeresése, kiválasztása vagy visszarakása |
| Vásárló tenyere | Enter / Backspace / Delete | Pontos összeg átvétele / utolsó választás vissza / összes törlése |
| Monitor | Tab / Shift+Tab / nyilak | Következő / előző / irány szerinti vezérlő |
| Monitor | Enter / Space | Kijelölt művelet végrehajtása |
| Monitor | Backspace | Játékból a játékmenübe, alkalmazásból az asztalra, asztalról a boltba |
| TobaccOS asztal | 1–5 | Árurendelés, fejlesztés, kávé, kamerák, játékok |
| Árurendelés | 1–9, majd Enter | Termék kijelölése, majd rendelés |
| Árurendelés | + / −, numpad + / − | Mennyiség 1–12 darab között |
| Eszköz | Esc | Kilépés a boltba |

Sárga keret mutatja a billentyűzettel kiválasztott vezérlőt. A kávéalapanyag-rendelés, kameraváltás és a memóriajáték összes kártyája elérhető. Snake közben a nyilak a kígyót irányítják. Új játék visszaállítja a minijátékok menüjét; a korábbi rekordok megmaradnak.

## Új termékek

| Termék | Kiszerelés | Beszerzés | Eladás |
|---|---|---|---|
| Kevert | 50 ml kisüveg | 220 Ft | 450 Ft |
| Kőfejtői | 500 ml dobozos világos sör | 280 Ft | 550 Ft |

A Kevert a mellékelt kisüveg alakját követi: fekete csavaros kupak, világos üveg, borostyánszínű ital, narancsbarna saját címke. A Kőfejtői zöld dobozt és saját címkét kapott. Mindkettő saját 3D modell, nem lapos termékkép.

A megvásárolt boltbővítés nyitja meg őket, ingyen kezdőkészlet nélkül. A rendelés, raktári karton, kézben hordás, saját polchely, önkiszolgáló vásárlás, kassza, lopási veszteség és napváltás a többi termék szabályait követi. A két új raktári hely a meglévő italállványok felső szintjére került. Az önkiszolgáló polc öt szintet, harminc szabadon használható helyet kapott.

A katalógus kilenc csomagolt termékét a `ShopCatalog.Count` jelöli; a külön főzött kávé indexe `ShopCatalog.Coffee == ShopCatalog.Count`. A nyers 7-es kávéindex már nem érvényes. A vevők italválasztása naponta változik, hogy mind az öt ital keresletet kapjon.

## Szabad polcelhelyezés

Mind a kilenc csomagolt termék és a kész kávé elfér a kezdőpolc 32, az önkiszolgáló 30 és az extra polc 9 helyén. A termék kézből és kartonból is tetszőleges szabad polchelyre tehető. A foglaltság, méret és tényleges ütközés továbbra is ellenőrzött. A cigarettapolc szintjei magasabb helyet kaptak a palackokhoz; a pult és a többi berendezés a helyén maradt.

A bolti polcok korábbi márkaazonosítója csak az új játék alapfeltöltését és a tesztek ajánlott elrendezését jelöli. A kiszolgálópult és a kávégép kimenete továbbra is külön állomás; kartonból az eladásokat nem lehet megkerülni. A raktári kartonok saját termékük készletét tárolják.

Az NPC a termék tényleges azonosítóját keresi a nyilvános polcokon. Oda tett cigarettát is önkiszolgálóként vesz le; a személyzeti polcra tett italt a játékos adja át a pultnál. Visszavont vásárlás az eredeti tényleges polchelyre tér vissza. A készletszámolás és a pénzügy nem a polccímkétől függ.

Az önkiszolgáló sorcímkék és a kezdőpolc árcédulái a tényleges tartalomhoz igazodnak. Üres helyet vagy vegyes készletet jelölnek, ha nincs egyértelmű egytermékes ár. A termékre nézve a felvételi súgó a valódi eladási árat mutatja.

## Ellenőrzés

A tényleges Unity-jeleneten **25 csoport, 3690 sikeres állítás**, 0 sikertelen. Eredmény: `Builds/KeyboardAlcoholRelease/checks.json`.

Az új `KeyboardAlcoholChecks.Keyboard` 44 állítása a monitor minden alkalmazását, rendelést, nyilakat, tabulálást, numpad Entert és a teljes memóriajáték billentyűzetes megoldását ellenőrzi. A `PaymentsAndAlcohol` 232 állítása három tényleges műszakot teljesít: mindkét új ital eladása, 16 bankjegyes és 5 érmes fizetés, Enteres lezárás, visszavonás, törlés, készlet és készpénz egyezősége. A kibővített `DrinkPackagingChecks.Run` 88 állítása mind az öt ital tényleges kartonos feltöltését és visszavételét ellenőrzi.

A `FreeShelfChecks.AllProductsAllShelves` 2203 állítása a 71 helyen mind a kilenc csomagolt termék és a kávé valódi kézi áthelyezését, a három polccsalád kartonos feltöltését, készletmegőrzést és napváltást vizsgálja. A `MixedSelfService` 14 állítása vegyes termékazonosítókat, árcímkéket, tényleges NPC-felvételt, vásárlás-visszavonást és a személyzeti polcon lévő ital kézi kiszolgálását ellenőrzi.

Képek: `Builds/KeyboardAlcoholRelease/mixed-public-shelf.png`, `Builds/KeyboardAlcoholRelease/mixed-staff-shelf.png`, `Builds/KeyboardAlcoholRelease/alcohol.png`, `Builds/TobaccOSPreview/expanded.png`, `Builds/DrinkPreview/shop-drinks.png`. A WebGL böngészős ellenőrzése `node Tools/Verify-WebGL.cjs --features`; a böngészőből érkező valódi Backspace, Tab, Enter, Space, nyíl- és számbillentyűket is elküldi, és külön képeket ment a billentyűzetes állapotokról.


A végleges WebGL-build sikeres: **0 hiba, 0 figyelmeztetés**, 20 961 025 bájt, 372 664 ms. Jelentés: `Builds/KeyboardAlcoholRelease/build-status.json`. A helyi Chromium-futás **15 képet és 0 böngészős hibát** eredményezett; a képeken billentyűzettel kijelölt rendelőkártya, alkalmazásváltás, kamerakép és Space-szel felfordított memóriakártya is ellenőrizve. Napló: `Builds/Verification/local-console.json`.

# #123 – Üzlet és raktár

## Méretek, árak és jogosultság

| Szint | Üzlet névleges mérete | Következő lépcső ára | Raktár névleges mérete | Következő lépcső ára |
|---|---|---:|---|---:|
| 0 | 5 × 6,3 m | 24 000 Ft | 3,5 × 4 m | 12 000 Ft |
| 1 | 7 × 6,3 m | 38 000 Ft | 3,5 × 5 m | 20 000 Ft |
| 2 | 9 × 6,3 m | 55 000 Ft | 5 × 5 m | 32 000 Ft |
| 3 | 11 × 6,3 m | — | 5 × 6 m | — |

A szintek függetlenek. Az üzlet jobbra nő; a raktár balra/előre, változatlan hátsó ajtóval. A játékos a TobaccOS Üzletkezelés lapján, nyitás előtt vagy az üres bolt bezárása után vásárolhat. A Fejlesztések fülön együtt van az üzlet- és raktárbővítés; a Bútorok fülről a katalógus közvetlenül az átrendezőbe vezet. A bútorokat ott megtervezi, majd egyben fizeti ki mentéskor. Lásd [bútorvásárlás](../../Acceptance/furniture-planning/README.md). Az első üzletbővítés nyitja az önkiszolgálót és a hűtő vásárlását.

A megvásárolható belső elhelyezési terület oldalanként 10 cm tervezési tartalékot tart. Üzlet: X = −2,4 … 2,4 + szint × 2; Z = −4,3 … 1,8. Raktár: X = −5,9 … −2,6 (0/1) vagy −7,4 … −2,6 (2/3); Z = −1,9 … 1,9 (0), −2,9 … 1,9 (1/2), −3,9 … 1,9 (3). A tényleges faltestek ezen kívül helyezkednek el.

| Modul | Alapterület | Ár |
|---|---|---:|
| Bolti polc | 1,2 × 0,5 m | 6 000 Ft |
| Keskeny polc | 0,6 × 0,5 m | 3 500 Ft |
| Kétoldalas polc | 1,2 × 0,9 m | 10 000 Ft |
| Raktári állvány | 1,2 × 0,6 m | 5 000 Ft |

A bolt/pult/raktár nyitása közötti közlekedő védett. Az elhelyezés ellenőrzi a vásárolt szobát, ütközést, játékoshelyet, NPC-hozzáférést, valamint a pult–raktár–hátsó ajtó útvonalat. Az illesztőrács 5 cm; a helyiségek és a bútorok mérete méterben értendő. Új bútor csak akkor fizethető ki, ha található hozzá szabályos kezdeti hely. Utána az átrendezőben mozgatható, Q/E-vel forgatható, menthető vagy visszavonható. A nyitott oldalak hozzáférését a tényleges navigáció ellenőrzi; az 1,2/1,5 m-es folyosók tervezési ajánlások, nem mindenhol kényszerített szélesség.

## Alaprajzi alternatívák

[Méretezett felülnézeti terv](floor-plans.svg). A képen a bútorhelyek példák; a játék nem kényszeríti ezeket a játékosra.

- **Kezdő bolt:** A: tágas pultos tér; B: külön megvásárolt keskeny polc a jobb oldalon. A hosszú polc még nem aktív; középső sort nem erőltetünk ide.
- **7 méteres bolt:** A: három fal menti, balra néző polc X = 3,95; Z = −2,9 / −1,65 / −0,4. B: ezek 20 cm-rel beljebb tolva. Hűtőhely: (3,35; 1,35).
- **9 méteres bolt:** A: a három fal menti polc megtartva; B: rövid középső sor, polcközéppontok X = 3,25 / 4,50 / 5,75; Z = −1,50, előlapjuk a bejárat felé. A sor mellett a hozzáférés körbejárható.
- **11 méteres bolt:** A: fal menti alap, a jobb szélen új berendezéseknek szabad tér; B: a fenti rövid középső sor, a jobb oldalon további szabad közlekedési/szolgáltatási területtel.
- **Raktár:** minden szinten a meglévő állványok és a fix (−4,25; 2) hátsó ajtó maradnak. A bővítés először elöl ad új helyet, majd balra szélesít. A megvásárolt állványokat az útvonal-ellenőrzés után bármely szabad falhoz vagy belső helyre lehet rendezni; kapacitásuk négy valós kartonhely. A teljes készlet beszerzési felső korlátját ez a geometriai fejlesztés nem változtatja meg.

## Modulhatárok és kompatibilitás

- `ShopLayout`: külön szintek, árak, területek. `Expanded` a szint > 0 képességjelzője. A korábbi `SetExpanded(true)` API a régi 9 méteres területet (2. szint) jelenti; a játékos `TryExpand()` vásárlása egyesével léptet.
- `ShopExpansionGeometry`: meglévő fal/padló/mennyezet átméretezése, raktárhatárok és bővítési lámpatestek; nem cseréli a jelenetgyökereket vagy az ajtókat.
- `ShopModularFurniture`: polcépítés, fizetett modulvásárlás, raktári kartonhelyek, visszaállítás. A régi hosszú polc minden eredeti ProductSlot-objektumát megtartja, és három új egység alá helyezi. A controller eredeti slot-/terméktömbjeinek hivatkozásai érvényesek maradnak; nincs készletmásolás.
- `ShopArrangement` / `ShopFurniture`: közös előnézet, forgatás, atomi mentés/visszavonás; külön bolti és raktári jogosultság. A termékek a slotot, a raktári kartonok a szülő dockot követik.
- `ShopExpansionUi`: a fejlesztési képernyő második oldala, közös egér/billentyű célpontokkal.

A jelenlegi projekt az elrendezést futamon/napváltáson belül tartja meg; új teljes futammentés nem készült. Az eredeti hosszú polc objektumneve/hivatkozása megmarad a migráció gyökereként, az eredeti polchelyek újrafelhasználásával. Új játék törli a vásárolt modulokat és visszaállítja az eredeti szinteket/pozíciókat.

Ellenőrzés: `expansion` natív suite a checkout-profilban; `node Tools/Verify-Acceptance.cjs --case=expansion`. Részletek és a tényleges készültség: [elfogadási jelentés](../../Acceptance/expansion/REPORT.md).

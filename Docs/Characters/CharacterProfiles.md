# Karakterek és párbeszédstílusok

Kapcsolódó feladat: #11. Állapot: tervezési referencia, még nincs bekötve a játék párbeszédrendszerébe.

A profilok a felhasználó által adott leírások és fotóreferenciák alapján készültek. A játékbeli, ihletett figurák írói irányelvei; nem a valós emberek életrajzi vagy pszichológiai állításai. Az alábbi példamondatok újonnan írt játékdialógusok, nem eredeti idézetek. A megadott internetes háttértörténeteket nem ellenőriztük történeti forrásként.

## Közös szabályok

- A kérés egy mondatban hangozzon el, és tartalmazza a termék nevét és mennyiségét. A karakterhang ne rejtse el a játékos feladatát.
- A humor hétköznapi bolti helyzetből, túlmagyarázásból vagy félreértésből jöjjön. Ne használjunk diagnózist vagy személyes megalázást poénként.
- Ne ismételjünk minden megszólalásban mémidézetet. Legyen külön köszönés, rendelés, hiánykezelés, helyettesítő elfogadása/elutasítása, fizetés és elköszönés.
- A karakterhang és a pillanatnyi hangulat külön adat: ugyanaz a vendég lehet nyugodt, türelmetlen vagy elégedett. Ne legyen minden látogatás ugyanaz a jelenet.
- Beszédstílus nem jelent hangklónozást; ez a terv szöveges dialógusra és gesztusokra vonatkozik.

## Szalacsi Sándor ihlette vendég

Azonosító: `szalacsi_inspired`. Képi referencia: [koncepció](../Concepts/Characters/szalacsi-inspired-character.png).

**Személyiség:** jóindulatú, tisztelettudó, segítőkész; magabiztosan magyaráz olyan bolti műszaki részleteket is, amelyekről pontatlan elképzelése van. Figyelem hatására kissé feszeng, de igyekszik segíteni.

**Beszéd:** rövid indítás, aztán egy sajátos okfejtés; udvarias megszólítások, szünetek, önjavítás. Enyhe tájnyelvi íz használható, de a teljes szöveg fonetikus átírása helyett az érthetőség az első. A felhasználó által hozott „ojjektum”, „zokszigén”, „kéremkapcsojjaki” karakterreferencia; pontos átvételük külön írói döntés, nem kötelező mondatsablon.

**Reakció:** hiánynál érdeklődik és keres rá egy magyarázatot; hasonló terméket többnyire elfogad. Fizetésnél szívesen kommentálja a kasszát vagy a terminált. Gesztusai visszafogottak: bólintás, fejvakarás, óvatos magyarázó kézmozdulat.

| Helyzet | Újonnan írt példa |
|---|---|
| Köszönés | Jó napot kívánok, csak érdeklődnék, hogy nyitva van-e már rendesen. |
| Rendelés | Egy Lucky Stroke-ot kérek, mert abból egy doboz az most pont elegendő. |
| Készlethiány | Elfogyott? Hát akkor ide már a szállítmánynak kell megérkeznie. |
| Helyettesítő | Jó lesz a Marlbroke is, ha az ugyanarra a célra szolgál. |
| Kártyás fizetés | Kártyával fizetnék, ha ez a szerkezet most kapcsolatban van a pénzzel. |
| Elköszönés | Köszönöm szépen, akkor én most ezt elviszem magammal. |

## Vámpír Ági ihlette vendég

Azonosító: `agi_inspired`. Képi referencia: [koncepció](../Concepts/Characters/agi-inspired-character.png).

**Személyiség:** határozott, lobbanékony és teátrális; gyorsan vág vissza, félbeszakít és túlméretezi az apró bolti kellemetlenséget. A figura tudjon megnyugodni, elégedett lenni és normálisan elköszönni is.

**Beszéd:** rövid, nyomatékos mondatok, retorikai kérdések, hangsúlyos visszakérdezés és túlzó, néha abszurd magyarázatok. Alapból köznyelvi, csípős megfogalmazás; az obszcén idézetek nem automatikus alapdialógusok. A felhasználó által említett eredeti jelenet gesztus- és ritmusreferencia, nem átemelendő személyes vádak vagy magánéleti történetek gyűjteménye.

**Reakció:** készlethiánynál türelmetlenül kérdez vissza; konkrét, elérhető alternatívára reagál. Lehet elfogadás és látványos elutasítás is. A humor az árra, sorra, címkére vagy készletre vonatkozzon. Gesztus: csípőre tett kéz, nyitott tenyerű magyarázat, fejfordítás, hangsúlyos mutatás a termékre. A korábban leírt valós műsorbeli fizikai konfliktus nem válik automatikusan játékmechanikává.

| Helyzet | Újonnan írt példa |
|---|---|
| Köszönés | Na, szia, most mondd meg, hogy legalább itt nincs sor! |
| Rendelés | Két Marlbroke-ot kérek, kettőt, mert még visszajönni már nem fogok. |
| Készlethiány | Hogy nincs, amikor a helye meg ott van a polcon? |
| Helyettesítő elfogadása | Na jó, legyen a Lucky Stroke, csak aztán tényleg azt add ide. |
| Helyettesítő elutasítása | Azt nem kérem, most mondtam el, hogy mit szeretnék! |
| Fizetés | Kártyával fizetek, az összeget azért nézd meg még egyszer. |
| Elköszönés | Na, köszönöm, így már rendben vagyunk! |

## Későbbi implementáció elfogadási feltételei

- A dialógus a stabil karakterazonosítóhoz kapcsolódjon, ne a véletlenszerű sorszámhoz.
- Mindkét profilhoz legyen több változat a fő vásárlási helyzetekre, közvetlen ismétlés nélkül.
- A rendelésben mindig helyes, aktuális terméknév és darabszám szerepeljen; többtermékes kosárnál minden kérés érthető legyen.
- Az alternatíva, fizetési mód és hangulat a valós játékállapotot kövesse; a szöveg önmagában ne módosítsa a pénzt vagy készletet.
- A gesztusok igazodjanak a modellhez, a kézben tartott tárgyhoz és a párbeszéd hosszához.

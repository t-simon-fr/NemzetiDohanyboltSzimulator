# Telefonfeltöltés – használat

A telefonfeltöltés új játékban a **6. munkanapon, a második hét hétfőjén automatikusan és ingyen aktiválódik**. Nem kell fejlesztést venni hozzá. Az első öt napon nem érkezik ilyen kérés; a TobaccOS Telefonfeltöltés alkalmazása jelzi az aktiválás időpontját.

A felhasználó által kért általános heti feloldási/tutorialrendszer és a lottó automatikus harmadik heti aktiválásának átvezetése a #139 feladata. Ez a szelet a telefonfeltöltés automatikus aktiválását valósítja meg; a partnercsomagokhoz, üzlet-/raktárbővítéshez és bútorokhoz nem ad heti korlátot.

## Kiszolgálás

1. A vevő megadja a szolgáltatót, telefonszámot és összeget; a kérés a vevőpanelen és a szolgáltatási űrlapon is visszaolvasható.
2. E vagy bal kattintás a pulti bankkártyaterminálon: külön telefonfeltöltési mód. A TobaccOS Telefonfeltöltés alkalmazásából is kezelhető.
3. Válaszd ki a szolgáltatót, írd be a számot és összeget (vagy válassz 1000/2000/5000 Ft-ot), majd ellenőriztesd az adatokat. Hibás formátum vagy a vevő kérésétől eltérő adat javítható, nem indít fizetést.
4. A játék a szokásos készpénzes vagy bankkártyás kasszára vált. Feltöltés kizárólag sikeres fizetéskor történik, egy bizonylattal. Esc a nézetet zárja; a feltöltési oldalon a Megszakítás törli az előkészítést. Elküldött vevő ki nem fizetett kérése sem teljesül.
5. A Bizonylat fülön visszaolvasható az utolsó sikeres feltöltés. Az Elszámolás fülön rendezhető a szolgáltatói egyenleg.

A három saját szolgáltató Üreskom, Notyet tel és Two. Kizárólag a játék által generált, 000 kezdetű, 11 számjegyű fiktív számok használhatók; nincs külső szolgáltatói kapcsolat vagy valódi feltöltés.

## Pénzügy és állapot

Alapjutalék **5%**, konfigurálható a `ShopMobileConfig` adataiban. Egy 2000 Ft-os feltöltésből 100 Ft a bolti jutalék, 1900 Ft a szolgáltatói rész. A kassza a teljes befizetést mutatja, a szolgáltatói tartozás azonban nem költhető el árura/fejlesztésre vagy lottókifizetésre. A belső szolgáltatói rendezés nem újabb eladás vagy költség. Az összesítő külön mutatja a névértéket, jutalékot és tartozást; a napi profit csak a jutalékot tartalmazza ebből a szolgáltatásból.

Napváltás megőrzi az aktiválást, az utolsó bizonylatot és a fennmaradó tartozást; a napi számlálók nullázódnak. Új játék mindezt törli. Az aktiválás, fizetés és elszámolás ismételt inputja nem duplikálhat semmit. Az automatikus vevők között készpénzes és kártyás is van; kifogyott polcos készlet mellett is jöhet feltöltési kérés.

## Modulok és ellenőrzés

`ShopMobileTopUpState`: validálás, immutábilis kérés/bizonylat, egyszeri tranzakciók és szolgáltatói könyv. `ShopMobileTopUp` / `ShopMobileTopUpUi`: játékbeli adapter, közös POS-/TobaccOS-űrlap. A meglévő pénztár, input-/kamerafókusz és NPC-k vékony kapcsolásokkal használják. A szolgáltatás nem készlet-SKU, nem kerül átadópultra/vonalkódolvasóra.

Terv: [PLAN](PLAN.md). Bizonyíték és a baseline/friss próbák pontos hatóköre: [elfogadási jelentés](../../Acceptance/mobile-topup/REPORT.md).

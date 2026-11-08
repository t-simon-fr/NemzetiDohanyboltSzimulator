# #107 – Telefonfeltöltés

## Eredmény és hatókör

A telefonfeltöltés a **6. munkanapon, második hétfőn automatikusan és ingyen aktiválódik**. Három fiktív szolgáltató, kizárólag generált tesztszámok és 1000/2000/5000 Ft-os kérések; külön szolgáltatási mód a meglévő POS-on és TobaccOS-ban. Sikeres készpénzes/kártyás fizetés pontosan egy visszaolvasható bizonylatot készít. [Használat és modulok](../../Architecture/MobileTopUp/README.md).

A teljes heti rendszer/tutorial és a többi heti szolgáltatás migrációja #139 marad; annak ticketjében is átvezettük az ingyenes automatikus aktiválást. Nem adtunk heti korlátot a szabad beruházásokhoz. Nincs új jelenet vagy fizikai terminálmodell, polcos SKU, valódi szolgáltatói kapcsolat vagy pénzmozgás.

## Natív ellenőrzés

[Összesített checkout-baseline](unity-baseline.json): **57 suite / 1597 sikeres állítás**, 44,65 s, sikeres fordítással. A végső review ezután két érkezési esetet javított: a normál telefonos vevők közt készpénzes és kártyás is legyen, és elfogyott áru mellett is jöhessen szolgáltatási kérés. Csak az actor fizetésimód-választása, controller szolgáltatásfallbackje és az új teszt változott.

[Érkezési javítások célzott futása](unity-arrivals.json): **4 suite / 116 sikeres állítás**, 7,62 s: mobile-integration38, stock-aware-orders34, continuous-arrivals6, reputation-integration38. A baseline többi suite-ját változatlan működésre visszük tovább; a 1597 és 116 szám átfed, nem összeadandó, és nem állítunk második friss teljesprofil-futást. A végső116-os kör után a felhasználó szolgáltatóneveket pontosított: **Üreskom, Notyet tel, Two**. A névlista és a névellenőrzés változott; a [névfrissítés utáni kör](unity-names.json) **3 suite / 77 sikeres állítás** (mobile-state27, mobile-integration38, computer-ui12). Ennek a [névfrissítéskori forrással](source-names.json) egyezik a lenyomata. A végső ellenőrzés ezután a hatvevőnkénti ütem és a szolgáltatóválasztás azonos modulo-osztálya miatti egyoldalúságot javította: a kérések immár mindhárom szolgáltatón váltakoznak. Ez egy generálási sor és egy regressziós állítás változása. A [legutolsó natív kör](unity-final.json) **3 suite / 78 állítás** (mobile-state28, mobile-integration38, computer-ui12), azonos lenyomattal a [végső buildforrással](source.json). A névmódosítás előtti forrás külön [kezdeti lenyomat](source-initial.json).

Az új pure-state próba28 állítással ellenőrzi az ingyenes, egyszeri nap6-feloldást, kizárólag fiktív számokat, adategyezést, sikertelen fizetést, megszakítást, egyszeri/reentranciavédett tranzakciót és elszámolást, jutalékot, elkülönített mobil-/lottópénzt és napváltást. A végső38 integrációs állítás valós ajtós nap5→nap6 átmeneten bizonyítja az ingyenes aktiválást (csak a szokásos300 Ft működési költség terhel), mindkét kasszázási módot, hibás/javított adatokat, fizetés megszakítását, egyedi bizonylatot, helyes profitot és a napváltáskor megőrzött1900 Ft szolgáltatói tartozást. A szolgáltatói rendezés nem újabb eladás/profit; normál érkezési politika és áru nélküli működés is tesztelt.

## Natív képi review

Codex saját review, nem független agent. Tényleges Game view GUI-val, 1918×879: [adatlap](native-form.png), [bizonylat](native-receipt.png), [elszámolás](native-account.png); a kapcsolódó JSON-ok rögzítik a forrást. A képek a kért szolgáltatónevekkel készültek; az utolsó generálási javítás az ábrázolt első kérés adatait és a megjelenítést nem változtatja. Minden mező és művelet elfér; a kérés végig látszik, a bizonylat és a szolgáltatói pénz olvasható.

Ez API-val előkészített natív állapot és valódi gameplay-metódusokkal lezárt integráció, nem OS-egérként elszámolt interakció. A végső teljes adatbevitel és kattintásos pénzkezelés tényleges böngészőbemenettel is sikeres (lásd alább).

## WebGL

A [végső build](build.json) sikeres: 0 hiba, 3 Unity/Emscripten figyelmeztetés, 245,28 s. A [forráslenyomat](source.json) megegyezik az utolsó natív körével. Mindhárom böngészős futás ugyanazt a négy ellenőrzött WebGL-assetet használta.

- [Telefonfeltöltés](browser.json): **38 sikeres ellenőrzés**, 0 konzolhiba; tényleges E, kattintás, gépelés, készpénzes és kártyás fizetés, hibás/javított adat, megszakítás, napváltás és egyszeri elszámolás.
- [Meglévő fizikai terminál](browser-terminal.json): **18 sikeres ellenőrzés**, 0 konzolhiba.
- [Meglévő lottópont](browser-lottery.json): **29 sikeres ellenőrzés**, 0 konzolhiba.

A [képi review](visual-review.json) mind a 18 végső mobilos képet ellenőrizte 720p/1080p méretben: [adatlap](1280x720-mobile-form-720.png), [bizonylat](1920x1080-mobile-receipt-1080.png), [elszámolás](1920x1080-mobile-account-1080.png), [napi összesítő](1280x720-mobile-cash-summary-720.png). A pénzügyi részletek a meglévő görgethető összesítőben vannak; a napváltó gomb külön végig elérhető.

A böngészős fixture csak a napot, konkrét NPC-t és kamerát készíti elő. A valódi 5→6. napi, ingyenes aktiválást a natív integrációs próba bizonyítja; a böngészős 6. napi állapot önmagában nem ennek bizonyítéka.

## Publikálás

Pagesre nincs kiadva. A helyi idegen anyag-/projektbeállítások és Recovery/Data tartalom nem része a feature commitnak.

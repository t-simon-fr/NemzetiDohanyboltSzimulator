# #122 – Üzletreputáció

A **TobaccOS → Üzlet hírneve** oldalon a mai megfigyelések és az utolsó lezárt nap külön olvashatók. A napi összesítő a hírnevet, változását, a négy részmutatót és konkrét okokat mutatja; nincs új eseményenkénti párbeszédablak.

A bolt 50/100-ról indul. Az érték kizárólag napzáráskor változik, legfeljebb ±4 ponttal, fokozatos simítással. Legalább 30 aktív nyitott másodperc és három lezárt látogatás szükséges. Kevés adatnál nincs automatikus jutalom vagy büntetés; adat nélküli részmutatónál a felület ezt jelzi. A rövid megfigyelések értékelése semlegeshez közelebb marad.

| Mutató | Miből számolódik? |
|---|---|
| Választék / készlet | Feloldott, ténylegesen elérhető termékfajták aránya, a hiányos rendelések visszajelzésével. Polc és beérkezett raktáráru is számít; úton lévő készlet és újra kirakott azonos termék nem ad többletet. Kávénál az igazi alapanyag és elkészült ital, lottónál az átvett készlet számít. |
| Tisztaság | A nyitvatartás alatt fennmaradó foltok száma és kora. A friss kosz enyhe, a régi erősebb hatású. Az el nem takarított folt kora napváltáskor megmarad; zárás után nem öregszik. |
| Árak | A játékos által állítható árak az alapárhoz képest, és az ár miatt távozó vevők aránya közül a rosszabb. Az alapár jó értékelés, az igen alacsony ár további előnye kicsi. Fix lottóár nem rontja a mutatót. |
| Kiszolgálási idő | Belépéstől a fizetésig, sorban állással és a fizetőeszköz használatával együtt mért idő, a kosárhoz igazított kerettel. Kávé, lottó és beváltás több időt kap. A kiszolgálható vevő elküldése nem javíthatja az átlagot. |

A súlyok sorrendben 30/20/20/30%. A leggyengébb megfigyelt tényező korlátozza, mennyire fedheti el a többi jó eredmény. Kizárólag elutasított látogatásokból nem szerezhető hírnév. Az ár miatti távozás csak ár-, az áruhiány/helyettesítés csak készletadat: ugyanaz a késés nem kap második büntetést a kiszolgálásnál. Tolvajok nem szerepelnek az értékelésben.

Az idő a nyitva tartó bolt aktív játékideje: a szünet/súgó, előkészület és zárás utáni idő nem számít. A bolti számítógép és pénztár használata számít, így a fizetés ideje nem rejthető el eszközfókusszal. A felület mp-értékei az aktív játékidőt jelzik, a gyorsított játékórától külön.

## Hatások

- Semleges 50-es értéknél megmarad a korábbi 12–28 mp-es érkezési tartomány. A két szélsőértéknél 14,4–33,6, illetve 9,6–22,4 mp; a véletlenszerű érkezés és háromfős sorkorlát megmarad.
- A kosár továbbra is 1–3 termék. A szélső reputáció legfeljebb 20% eséllyel lépteti eggyel feljebb/lejjebb a meglévő kosárméretet. A választék- és készletszabályok változatlanok, a sor nem foglal előre.
- Semleges fölött növekszik a korábban kiszolgált, meglévő karakterek kedvenc termékkel való visszatérése, legfeljebb 45%-ig. Csak elérhető kedvenc választható. Saját abszurd mondatuk mellett a valódi rendelés olvasható marad. Lottós és kifejezetten előírt látogatás elsőbbséget kap.

Nincs közvetlen pénzjutalom, új beszerzési kedvezmény, ármódosítás vagy kötelező fejlesztés. Napváltás megőrzi a hírnevet, az utolsó eredményt és a vásárlói szokásokat, a napi adatok nullázódnak. Új játék mindent visszaállít. Általános lemezes mentés nem készült.

## Modulok és ellenőrzés

`ShopReputationState` tartalmazza a tiszta állapotot, kimenetenkénti egyszeri könyvelést, naplezárást, konfigurációt és determinisztikus hatásokat. `ShopReputation` a meglévő rendszereket mintázza és a korábbi karakterek szokásait kapcsolja be; `ShopReputationUi` a TobaccOS/összesítő nézete. A controllerben csak vékony eseménykapcsolatok vannak.

[Ellenőrzési jelentés](../../Acceptance/reputation/REPORT.md). Pages-publikálás nincs.

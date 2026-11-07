# #132 – Sarokpolc és pontosabb falillesztés

A barna bal oldali díszburkolat nem rendelkezik aktív kolliderrel. A képen látható általános hiba NPC-megközelítési hibát jelent; az alsó falnál reprodukálható, ha a polc nyitott oldala a fal felé néz. Az ismételt vásárlás korábban visszaállította a forgatást, most megtartja a lerakott példány irányát. Nyilak mutatják a vásárlói oldalt; a hibaüzenet külön jelzi a fal felé néző vagy más módon elzárt polcoldalt, illetve a bejárat/sor elzáródását.

A névleges ShopArea/WarehouseArea továbbra is a korábbi méret- és gazdasági szerződést követi. A bútorok FurnitureArea-ja a tényleges belső falfelülethez igazodik, 2 mm-es elválasztással. A korábbi 25 mm-es fal előtti tartalék és a 20 mm-es külön statikus ráhagyás nem tartja távol a polcot. Valódi átfedésre és elzárt útvonalra továbbra is elutasítás jár. A rajzi keret, rács és falillesztés ugyanazt a fizikai bútorhatárt használja. A hűtő meglévő névleges szabad térfogata nem változik.

A Sarokpolc a Bútorok katalógus hatodik tétele: 6500 Ft, 0,8×0,8×2,5 m, öt valódi levágott sarkú polclap, két merőleges hátfal és átlós nyitott oldal, összesen tíz készlethellyel. R-rel mind a négy irányba fordítható; a jobb első sarokban a nyitott oldal a bolt belseje felé néz. A tervrajz ötszögként rajzolja, átlós vásárlói iránynyíllal. A navigáció konzervatívan a 0,8×0,8 m-es befoglalót védi; az NPC-k átlós, járható célpontot kapnak. A megosztott polclapmesh előnézetenként nem sokszorozódik.

A sarokpolc fizetése/eladása/megszakítása/napváltása/resetje a meglévő bútorrendszerben történik. Eladás előtt ki kell üríteni, visszavásárlási ára 3250 Ft. Nincs új teljes mentési rendszer vagy külön Pages-kiadás. [Ellenőrzés](REPORT.md).

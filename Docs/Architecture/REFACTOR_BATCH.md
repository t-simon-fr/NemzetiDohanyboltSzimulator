# Refaktor batch #69–#74 — végrehajtási terv

Engedély: a felhasználó kérte mind a hat nyitott refaktor implementálását. Baseline: a17d405. Runtime refaktor, azonos játékosi viselkedéssel; jelenet/prefab GUID, input enum, mentéskulcs és Pages nem változik.

| Issue | Határ / érintett adapterek | Invariáns és célzott ellenőrzés |
|---|---|---|
| #69 | Önálló beszerzési ajánlat; OrderSupply/OrderBasket delegálás | Teljes kosár validálása, nulla részleges könyvelés; ledger, feature-batch, warehouse-storage és hibás kosarak |
| #70 | Közös készletnézet, raktári mennyiségek és időzített szállítás külön tulajdonossal | Polc/kéz/karton/privát doboz/vevő/pending szerepek; stock-aware-orders, warehouse-storage, ingredient-pickup, feature-batch |
| #71 | Checkout munkamenet és explicit receipt-slot nyilvántartás | Több azonos tétel, visszavétel, egyszeri fizetés; checkout-revision, basket-day, recovery, physical-terminal, customer-hand |
| #72 | Unity-független kávéfőző állapot | Recept, betöltött/úton/kész alapanyag, időzítés, sikertelen átvétel visszaadása; coffee-world, coffee-display, ingredient-pickup |
| #73 | Napfázis/óra modell és fókusz-élettartam tulajdonos | Előkészület/nyitás/zárás/este/ajtós lezárás; kamera/kurzor/pause/input fogyasztás; entrance-day, physical-terminal, coffee-display, binding-acceptance, after-hours-upgrades |
| #74 | Bootstrap, központi scene-binding és runtime erőforrás-élettartam | Párosított enable/disable, új játék, mesh/material/audio/RT felszabadítás; lifecycle célzott ellenőrzés és közös integráció |

Sorrend: #69 → #70 → #71 → #72 → #73 → #74, külön commitok és függő PR-ek. Minden rész után érintett natív ellenőrzés, a stabil batch végén egy közös teljes integráció és helyi WebGL-próba. A történeti eredmények baseline-ok, nem friss refaktor-bizonyítékok. Készültség: implementált határok, friss sikeres invariáns- és integrációs próbák, valódi WebGL-interakció, függőségi merge és issue-státusz rendezve. Pages külön kérésre.

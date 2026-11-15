# #112 – Fagyasztóláda és jégkrém: terv

## Döntés

Dnyipro-minta (pult mögötti exkluzív tárolás, kézi átadás), moduláris bútorral kombinálva: a fagyasztó 6-os bútorkatalógus-fajta, mert a partnerhűtők a 7-es fajtától indulnak (`FirstFurnitureKind` 6→7; minden partner-hivatkozás relatív). Nincs külön esti-kereskedős beszerzés és nincs világ-gombos vásárlás: az átrendező a vásárlás + elhelyezés egyetlen útvonala.

## Érintett területek

- Katalógus: `ShopIceCreamCatalog` (`FirstType=27`, `Types=3`, `IsIceCream`/`Index`), `SupplyCapacity` 27→30, nevek/árak/költségek.
- Beszerzés: `IsSupplyAvailable` + `SupplyTypes` kiterjesztés; `Take()` kapu fagyasztó nélkül nem kér jégkrémet.
- Bútor: `ShopModuleKind.Freezer = 6`, `CatalogNames/Details` + "Fagyasztóláda", `ModularFurniture.Names/Prices` + 12 000 Ft, `CreateFreezer` (láda + nyitható fedél + 12 exkluzív slot, 3×4), `CommitPlannedPurchase`/`Buy` nem regisztrálja a jégkrém-slotokat publikusként.
- NPC: `%8==3`-sávos injektálás fagyasztó + készlet esetén (lottó-override megmarad).
- Ár: új `ShopPriceCategory.IceCream` + `CategoryOf` (GUI/keyboard hurok `Length`-alapú).
- Vizualitás: flat fagyasztott csomag ízenként színezve + nyomtatott felirat (lottó-minta).
- Suite: `Tools/Checks/IceCreamChecks.cs` (`Catalog` + `Integration`), `suites.json`-bejegyzések; probe `icecream` forgatókönyv + 4 akció; böngésző `icecream` case.

## Invariánsok

Pénz- és készletmegmaradás (kivétel/visszarakás/megszakítás/napváltás duplikáció/vesztés nélkül); megtelt/nem megfelelő hely nem nyel el; rossz íz nem teljesít; csak beszerzett típust kérnek; foglalás nincs.

## Ellenőrzés

Új suite + `ledger` + `stock-aware-orders` + `checkout-revision` + `price-editor` + `furniture-planning`/`furniture-editor`/`arrangement` (régi bútor-kijelzések változatlanok); tényleges natív interakció; közös WebGL (`icecream` case: supplier-UI, szállítás, ládatöltés, NPC-kérés, árkategória, 720p/1080p); vizuális próba átnézett képekkel.

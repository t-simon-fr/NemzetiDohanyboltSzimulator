# #109 – Vonaljegy és gyűjtőjegy: terv

## Döntés

Dnyipro-minta, supplier-utas változat: a jegyek normál, beszerezhető termékek (nem esti kereskedő, nem bizomány), pult mögötti exkluzív slotokkal, meglévő checkouttal. Részletes mechanikai döntések: [#109, implementáció előtti komment](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/109#issuecomment-6883336879).

## Érintett területek

- Katalógus: `ShopCatalog` +2 tétel (25–26), `SupplyCapacity` 25→27, új `ShopTicketCatalog` (első típus, darabszám, `IsTicket`).
- Beszerzés: `IsSupplyAvailable` + `SupplyTypes` kiterjesztés; inventory/karton/warehouse/probe a kapacitást követi.
- Tárolás: `ShopCounterStorage.TicketSlots` (5+5 exkluzív hely, Dnyipro-`Slots` változatlanul).
- NPC: `SpawnCustomer`-sávos injektálás (`%8==5`, ütközésmentes), természetes befoglalás készlet esetén.
- Kassza: `ShopBarcodeScanner` kiterjesztés; checkout változtatás nélkül (vegyes kosár, készpénz/kártya).
- Ár: új `ShopPriceCategory.Tickets`, `CategoryOf`, 4→`Length`-alapú UI-hurok (a fixet a vizuális próba találta).
- Vizualitás: flat jegy `ProductVisuals.Size` + `Add` ágban (lottó-minta, nyomtatott felirat).
- Suite: `Tools/Checks/TicketChecks.cs` (`Catalog` + `Integration`), `suites.json`-bejegyzések.

## Invariánsok

Pénz- és készletmegmaradás minden átadás/visszavétel/megszakítás/napváltás után; rossz típus nem teljesít; sorban álláskor nincs foglalás; csak beszerzett típust kérnek.

## Ellenőrzés

Új suite + `ledger` + `stock-aware-orders` + `checkout-revision` + `price-editor` (natív, Unity); tényleges natív interakció (átadás/leolvasó, készpénz+kártya); közös helyi WebGL (`tickets` case: supplier-UI, szállítás, slotok, NPC-kérés, árkategória, 720p/1080p); vizuális próba átnézett képekkel.

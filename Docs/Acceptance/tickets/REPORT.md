# #109 – Vonaljegy és gyűjtőjegy

## Eredmény

Két fiktív közlekedési jegy (Vonaljegy 600 Ft, Gyűjtőjegy 10 utazás 5000 Ft) rendelhető a supplier-monitoron, tárolható a pult alatti jegyslotokban, kiszolgálható kézi átadással és leolvasóval, elszámolható vegyes kosárban készpénzzel és kártyával. Rossz típus nem teljesít rendelést; csak beszerzett típust kérnek; sorban álláskor nincs foglalás. [Használat és modulok](../../Architecture/Tickets/README.md), [terv](../../Architecture/Tickets/PLAN.md).

## Natív ellenőrzés

A [végső célzott futás](unity-final.json) **6 suite / 146 sikeres állítás, 0 hiba**: ticket-catalog12, ticket-integration35, ledger12, stock-aware-orders34, checkout-revision20, price-editor33. A próbák lefedik a katalógus-illesztést (25–26 index, `SupplyCapacity` 27, Jegyek árkategória, flat vizualitás, NPC-költségvetés), a beszerzés→szállítás→pulti tárolás láncot, a készletezett/nem készletezett rendeléskapuzást, a handoveres és leolvasós átadást, a kártyás és készpénzes elszámolást pontos könyveléssel, az ismételt jóváhagyás/visszavétel tiltását, a rossz típus elutasítását készletváltozás nélkül, valamint a napváltás/új játék megőrzést/törlést.

Egy köztes futás piros lett az új suite-okkal, kódváltoztatás nélkül, közvetlenül a WebGL-build utáni első Unity-indításkor; a változatlan kód kétszeri újra futtatása azonnal zöld volt. Környezeti (build utáni editor-állapot), nem kódhiba; bizonyítéknak csak a zöldek számítanak.

## Natív képi és interakciós review

Reviewer: agent (képek ténylegesen átnézve), jóváhagyás a PR-en. Natív Game view: [rendelés](native-request.png) (Vonaljegy 1×, 0/1 átadva, 600 Ft, HUD 2600 Ft = 6000−3400), [fizikai jegy az átadón](native-ticket-delivery.png) (flat lap, bézs/kék papír, lottó-minta), [Jegyek árkategória](native-prices-tickets.png). A kategóriasor eredetileg 4-es osztással levágta az ötödik Jegyek gombot; `CategoryNames.Length`-alapúra javítva (gombok + keyboard-targetek), újraképen mind az öt elfér. A jegynyomat a lottóhoz hasonlóan a lapra néz, a termékazonosságot a rendeléspanel, HUD és polccímke hordozza. [Vizuális rekord](visual-review.json): passed.

Az interakciót a suite valós útvonalakon járta végig (handover/szkenner, készpénz+kártya, rossz típus, napforduló); a natív képekhez a fixture csak tőkét, szállítást, slotolást és egy konkrét NPC-rendelést készített elő.

## WebGL

[Végső helyi build](build.json): az első (teljes) build **327 s, 0 hiba / 3 figyelmeztetés**; utána egy elavult `delayCall`-os sorbarakás késve újra lefutott és **5,6 s-es, 0 hibás/0 figyelmeztetéses, bájtra azonos** játékost írt felül (forrás változatlan, [fingerprint](source.json) egyezik). Megjegyzés: a `ShopAcceptanceBuild.Queue()` `delayCall`-ja ebben a környezetben nem indul el magától; a szinkron `Build()`-hívás működik.

[Tikettek](browser.json): **10 sikeres böngészős ellenőrzés, 0 konzolhiba**, azonos player-asset SHA256 értékekkel. Valódi böngészőinput: supplier-kosár görgetés a jegyekig, 2+1 rendelés (3400 Ft), beküldés (egyenleg 2600 Ft), szállítás (raktár 2+1), pulti slotolás, NPC jegykérés ("Vonaljegy" a rendelésben), Jegyek árkategória-kattintás. Képek: [slotok720p](1280x720-ticket-slots-720.png) / [1080p](1920x1080-ticket-slots-1080.png) (pult + "Vonaljegy / Gyűjtőjegy" címke), [kérés720p](1280x720-ticket-request-720.png) / [1080p](1920x1080-ticket-request-1080.png) (rendeléspanel), [árak720p](1280x720-ticket-prices-720.png) / [1080p](1920x1080-ticket-prices-1080.png) (mind az öt kategória, mindkét jegy). A kérés-kép kamerája a folyosóra néz (probe-célzási korlát), a rendeléspanel teljesen olvasható. A teljes eladási folyamatot (átadás, fizetés, visszavétel-tiltás) a natív suite fedi; a böngésző a megosztott útvonalakat (beszerzés-UI, kérés, ár-UI, renderelés) igazolja.

## Hatókör

Új katalógustételek 25–26; a korábbi SKU-k változatlanok. Kezdő (nem bővített) bolt is elég hozzá; új bútor, gyorsbillentyű, menetrend-API, bérlet vagy utazási szimuláció nincs. A helyi idegen anyag-/projektbeállítások és Recovery/Data tartalom nem része a commitnak. Pages-kiadás nincs.

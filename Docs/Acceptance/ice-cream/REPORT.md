# #112 – Jégkrémes fagyasztóláda

## Eredmény

Három saját márkás csomagolt jégkrém (Hókristály csoki 450 Ft, Jeges Pötty eper 500 Ft, Sarki Fény vanília 550 Ft; beszerzés 200/220/240 Ft) rendelhető a normál supplier-monitoron, tárolható a 12 000 Ft-os fagyasztóládában (6-os bútorkatalógus, 12 exkluzív slot: ízenként 4), kiszolgálható kézi átadással és leolvasóval, elszámolható vegyes kosárban készpénzzel és kártyával. Rossz íz nem teljesít rendelést; csak fagyasztóval rendelkező, már beszerzett típust kérnek; sorban álláskor nincs foglalás. [Használat és modulok](../../Architecture/IceCream/README.md), [terv](../../Architecture/IceCream/PLAN.md).

## Natív ellenőrzés

A [végső célzott futás](unity-final.json) **9 suite / 244 sikeres állítás, 0 hiba**: icecream-catalog 13, icecream-integration 38, ledger 12, stock-aware-orders 34, checkout-revision 20, price-editor 34, furniture-planning 32, furniture-editor 34, arrangement 27. A próbák lefedik a katalógus-illesztést (27–29 index, `SupplyCapacity` 27→30, Jégkrém árkategória, flat fagyasztott vizualitás, NPC-költségvetés), a fagyasztó arrangement-vásárlás/útvonal/mentés láncot, a 12 exkluzív slotot (idegen áru kizárva, nem publikus), a beszerzés→szállítás→ládafeltöltést, a készletezett/nem készletezett rendeléskapuzást (`FreezerOwned` kapuval), a handoveres átadást, a kártyás és készpénzes elszámolást pontos könyveléssel, az ismételt jóváhagyás/visszavétel tiltását, a rossz íz elutasítását készletváltozás nélkül, valamint a napváltás/új játék megőrzést/törlést. A `FirstFurnitureKind` 6→7 eltolás miatt a partner-hivatkozások relatívak maradtak; a ticket- és furniture-planning suite-ok igazítják az eltolást.

Két köztes futás a WebGL-build utáni első Unity-indításkor pipeline-kapcsolati hibával (`An error occurred while sending the request`) piros lett, kódváltoztatás nélkül; a változatlan kód azonnali újra futtatása zöld volt. Környezeti (build utáni editor-állapot), nem kódhiba; bizonyítéknak csak a zöldek számítanak.

## Natív képi és interakciós review

Natív képet nem rögzítettünk; az interakciót a suite valós útvonalakon járta végig (arrangement-tervezés/mentés, slotolás, handover/szkenner, készpénz+kártya, rossz íz, napforduló). A vizuális döntést a WebGL-képek hordozzák (lásd alább). [Vizuális rekord](visual-review.json): passed.

## WebGL

[Végső helyi build](build.json): **260,8 s, 0 hiba / 3 figyelmeztetés, 27194042 bájt**, azonos forrás-fingerprinttel ([forrás](source.json)). A szövegjavítás utáni újrabUILD azonos méretrendben készült; a böngésző-ellenőrzés ebből a buildből futott.

[Jégkrém böngészős eset](browser.json): **11 sikeres böngészős ellenőrzés, 0 konzolhiba**. Valódi böngészőinput: supplier-kosár görgetés mindhárom típusig, 2+1+1 rendelés (860 Ft), beküldés, 8,1 s szállítás-tick, fagyasztó arrangement-mentés, ládaslotolás, NPC csokis-jégkrém-kérés, Jégkrém árkategória-kattintás. Képek: [láda720p](1280x720-icecream-slots-720.png) / [1080p](1920x1080-icecream-slots-1080.png) (nyitott fedelű láda a pult mellett, olvasható `JÉGKRÉM` / `−18 °C` felirattal), [kérés720p](1280x720-icecream-request-720.png) / [1080p](1920x1080-icecream-request-1080.png) (rendeléspanel: `1 × Hókristály`, `0 / 1 átadva`, `Összesen 450 Ft`), [árak720p](1280x720-icecream-prices-720.png) / [1080p](1920x1080-icecream-prices-1080.png) (hatodik `Jégkrém` kategória mindhárom ízzel). Az első képi körben a ládafelirat tükrözve jelent meg (TextMesh `Quaternion.identity` a +z-s fronton); javítva `Euler(0,180,0)`-ra, újrabUILD és újrafotózás után mindkét felbontáson olvasható. A teljes eladási folyamatot (átadás, fizetés, visszavétel-tiltás) a natív suite fedi; a böngésző a megosztott útvonalakat (beszerzés-UI, kérés, ár-UI, renderelés) igazolja.

## Hatókör

Új katalógustételek 27–29; a korábbi SKU-k változatlanok. Kezdő (nem bővített) bolt is elég hozzá; új évszak, olvadás, hűtőlánc-büntetés nincs. A helyi idegen projektbeállítások és Recovery/Data tartalom nem része a commitnak. Pages-kiadás nincs.

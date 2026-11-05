# Új feature-csomag – 2026-11-05

Issue-k: #53–#59. PR-ek: #62–#68. A feature-megvalósítás eredménye; későbbi külön kérésre [Pagesre is publikálva](../2026-11-05-feature-pages/REPORT.md).

## Eredmény

- Kávégép: E és bal kattintás menüt nyit; megszűnt a közvetlen főzés és az 1–2–3/Q receptváltás, a kézi feltöltés megmaradt.
- Óra: nyitáskor 08:00, 1 másodperc = 1 játékperc; kézi zárás után is halad a nap lezárásáig. Előkészületben áll, napváltáskor visszaáll; a kijelzés 23:59-nél megáll a nap végéig.
- Seprű: fa nyél, sörték, raktári állvány és söprési mozdulat. Az ütő önálló, elvékonyodó famodellt és pult mögötti falitartót kapott.
- Vonalkódolvasó: a megfelelő termék beolvasását követi a vevői zónába helyezés; fény- és hangjelzés, változatlan kasszaelszámolás.
- Árurendelés: termékenkénti +/-, beírt darabszám és egyben leadható kosár; előzetes, teljes pénz- és kapacitásellenőrzés.
- Csempész: az első este kínálatot ad, a későbbieken 65% az érkezés esélye; esténként stabil, 1–10 csomagos ajánlat. A tényleges, kifizetett termékeket tartalmazó doboz egyben vihető és elég szabad hely esetén a kétsoros, 10 férőhelyes privát polcra pakolható. Az érkezés gyakorisága és a mennyiség később balanszolható.

## Ellenőrzés és forrás

- [Unity-regresszió](unity.json): **650 sikeres állítás, 26 csoport**, benne 22 új készlet/pénz/óra állítás, 37 kávékijelző- és 27 tárolóellenőrzés.
- [Közös helyi WebGL-próba](browser-full.json): **30 sikeres ellenőrzés, 0 böngészőhiba**. Valódi CDP egér-, szöveg- és billentyűeseményekkel: kosárbevitel és vásárlás, szkennelés, seprű/ütő felvétel, nyomva tartott söprés, csempészvásárlás, doboz felvétele/bepakolása, három kávérecept és hiánykezelés.
- A teljes kör forrása `49ca2c5`, lenyomata a [forrásleírásban](source-initial.json). A natív futáskor a végső változtatások még nem voltak commitolva; a natív és a build lenyomata egyaránt `d70c3d98e14e28fd76640f0d9b40ec0340eb6d2f855447e2f6470c39adf20486`.
- A vizuális ellenőrzés két hiányzó mínusz-feliratot talált. A végső `a775310` forrásban kizárólag ez a két megjelenített szöveg változott a runtime-ban. [Végső forrás](source-final.json), [új build](webgl-final-build.json), [célzott rendelőpróba](browser-final-cart.json): **6 sikeres ellenőrzés, 0 böngészőhiba**, közvetlen szövegbevitel és tényleges + / - kattintás, pontos közös rendelés. A változatlan játékmeneti köröket nem ismételtük meg.
- Az első böngészős dobozpróbában a kamerát a fizikai ütközés elmozdította a fixture célpontjáról. Tényleges egérmozgással a dobozra célozva a felvétel és a bepakolás sikeres; ehhez runtime-javítás nem kellett.
- A teszthez használt probe csak Editor/SHOP_ACCEPTANCE buildbe kerül. A két build nem shipping kiadás.
- A korábbi felhasználói render-/projektbeállítások a helyi buildben megmaradtak, nem kerültek a feature-commitokba. A jelenetet nem generáltuk vagy mentettük újra.

## Vizuális/interakciós review

Reviewer: Codex. Felbontások: 1280×720 és 1920×1080. A képek megtekintve, a fenti interakciók tényleges bemenettel végrehajtva. A mennyiségmezők, összeg és gombok olvashatók; a két pultzóna elkülönül, a fa ütő és a seprű felismerhető, a privát polc két sora külön látszik. A végső képeken a mínusz gomb is olvasható.

Képek:

- [Beírt kosár, végső feliratok](1280x720-cart-typed.png), [rendelés után, nagy nézet](1920x1080-cart-submitted-large.png).
- [Leolvasó](1280x720-scanner-ready.png), [áthelyezett termék](1920x1080-scanner-transferred-large.png).
- [Raktári seprű](1280x720-warehouse-broom.png), [fali ütő](1280x720-wall-bat.png), [söprés után](1280x720-broom-swept.png).
- [Esti ajánlat](1280x720-night-offer.png), [doboz](1280x720-night-box.png), [két polcsor](1280x720-private-two-rows.png), [nagy nézet](1920x1080-private-two-rows-large.png).
- [Kávégép menüje](1280x720-coffee-display-ready.png), [tejhiány](1920x1080-coffee-display-no-milk-large.png).

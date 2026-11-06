# #106 – Szerecsenjáték Zrt. lottópont

Implementálva, Unityben, vizuálisan és helyi WebGL-ben ellenőrizve. Pagesre nincs kiadva. A funkció használata és modulhatárai: [Lottópont](../../Architecture/LOTTERY_POINT.md).

Megvásárolható fejlesztés (12 000 Ft), két saját kaparós, Ötöske számsorsjáték, bizományos készlet, kézi papír/leolvasó/kassza, visszatérő tulajdonos NPC, beváltás és üzemeltetői elszámolás. A külön állvány a pult raktár felőli végén áll; a kassza és az átjáró használható marad. Játékbeli üzemeltető minden felületen: **Szerecsenjáték Zrt.**

## Forrás és végrehajtott ellenőrzés

- Végső runtime commit: `30a927705eed2f85a80f3f8622602041c06b2014`; fingerprint: `f0af8c0bf9e0c88658058ab0e44697cd3ef00528b58a051a8ab727456474ac5f`. [Forrás](source.json).
- Friss teljes natív checkout-profil: **953 sikeres állítás / 44 csoport**, 0 hiba. [Unity-jelentés](unity.json). A futás az utolsó javítás munkapéldányán, annak commitja előtt történt; a fingerprint a végleges runtime-mal azonos.
- Teljes WebGL baseline: **29 sikeres ellenőrzés**, 0 konzolhiba, `0884b28` / `4aa6402e9151adad48ce575953bf347e436c2e9bf5c12a042d2de7a6f85970a5`. [Böngésző](browser-baseline.json), [build](build-baseline.json), [forrás](source-baseline.json).
- Végső friss WebGL: **8 sikeres ellenőrzés**, 0 konzolhiba. [Böngésző](browser.json), [build](build.json). Build sikeres, 0 error / 3 warning, 290 s. A végső build a fenti végleges fingerprintből készült.

A baseline végigjátszotta a fejlesztés vásárlását, mindhárom típus rendelését/szállítását, papírfelvételt és leolvasót, készpénzes és kártyás fizetést, ismételt jóváhagyás védelmét, tényleges ajtós napváltást, eredeti szelvényazonosítóval visszatérő NPC-t, nyertes beolvasását/kifizetését, negatív üzemeltetői egyenleg rendezését, hibás majd helyes számbevitelt és egyszeri másnapi sorsolást. A fixture csak vevőt, időt és célzást készít elő; a műveletek tényleges egér-, billentyű- és szövegbevitelt használnak.

A baseline után talált hiba: a mínuszjel elválasztóként szerepelt, ezért negatív szám pozitívként átmehetett. [Reprodukáló natív hibajelentés](negative-before-fix.json). A végső diff a számmező mínuszjel-kezelését, két státusz-visszajelzést, a kapcsolódó próbákat/probe-ot és futtatót érinti. A friss WebGL-kiegészítés valódi `-1 2 3 4 5` bevitellel igazolja az elutasítást, változatlan pénzt/készletet; majd helyes vevőszámokkal a nyomtatást, kártyás aktiválást és egyszeri másnapi sorsolást. A változatlan kaparós/beváltási/elszámolási út baseline-eredményét megtartottuk; nem állítjuk, hogy a 29-es teljes kör a végső builden ismét lefutott.

## Vizuális és interakciós ellenőrzés

Reviewer: Codex, 2026-11-06; saját ellenőrzés, nem független reviewer. Unity Editor tényleges OS-egérrel: kaparós felvétele (készlet 5 → 4), leolvasóra helyezése és fizetési állapot, még inaktív szelvény; terminál +5 készletrendelése névérték-terhelés nélkül. [Natív inputállapot](native-order.json). A natív próbánál a célzás rögzítve volt, a PlayerInteractor aktív maradt; az átmeneti kamerabeállítás nem került mentésre.

1280×720 és 1920×1080 tényleges Game/player képek átnézve: nagy, kontrasztos feliratok; minden fő vezérlő elérhető; a napi összesítő görgethető; a lottóterminál nem fedi a kasszát. Korábban javítva: kasszával ütköző elhelyezés és halvány fizikai kijelző. A baseline képein észlelt régi „úton” / „nyert” láblécet a végső runtime szállítás-/kifizetés-visszajelzésre cseréli; ezek baseline-képek, nem a javított lábléc bizonyítékai.

- [Fizikai állvány és szabad kassza – baseline](baseline-1280x720-lottery-physical-rack.png)
- [Készletoldal – baseline](baseline-1280x720-lottery-stock.png)
- [Nyertes beolvasás – baseline](baseline-1280x720-lottery-winning-scan.png)
- [Elszámolás – baseline](baseline-1280x720-lottery-settlement.png)
- [Napi összesítő – baseline](baseline-1280x720-lottery-daily-summary.png)
- [Végleges negatív szám elutasítása, 720p](1280x720-lottery-negative-number.png)
- [Végleges másnapi eredmény, 720p](1280x720-lottery-final-draw.png)
- [Végleges másnapi eredmény, 1080p](1920x1080-lottery-final-draw-large.png)

Eredmény: **passed**. A helyi felhasználói anyag-/projektbeállítás-módosításokat és Recovery/Data fájlokat érintetlenül hagytuk; ezek a helyi fingerprint környezetének részei, nem részei a PR-nek. Napváltási megőrzés támogatott; új általános játékmentés nem készült. A #106-ban rögzített kereskedelmi jogi/korhatár-felülvizsgálat továbbra is kiadási teendő.

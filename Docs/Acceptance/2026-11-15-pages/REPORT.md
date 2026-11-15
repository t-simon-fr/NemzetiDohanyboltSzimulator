# Főkiadás — Pages-kiadás (2026-11-15)

A felhasználó engedélyével a main legfrissebb játékverziója megjelent GitHub Pagesen. Ez a kiadás a 8fe066e (2026-11-06) óta elkészült **összes** main-fejlesztést tartalmazza.

- Forrás: `d9ee73ff3cf68ee47656affc5c0b8dbdbae1ded6` (main, `d9ee73f`); [forráslenyomat](source.json). A lenyomat a kiadáskori munkafát fedi (`dirty: true`): a Unity által újraszerializált, feladathoz nem tartozó anyag-/projektbeállítások, plusz a kiadáshoz készült, Editor-csak `QueueShipping` build-segéd — egyik sem kerül a játékosba.
- Publish commit: `99507dc4d20cf54281fdae324623f13976224f29`; [Pages státusz](pages.json): **built**, hiba nélkül. [Deployment](deployment.json): **success**.
- [Shipping build](build.json): sikeres, 0 hiba / 3 figyelmeztetés, 27 187 527 bájt, 440,0 másodperc. Acceptance probe nélkül (nincs `SHOP_ACCEPTANCE` define).
- [Release manifest és fájlhash-ek](release.json); cache-verzió: `d9ee73f`, a loader/data/framework/wasm hivatkozásokban is.
- [Élő játék](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=d9ee73f).

Tartalom a 8fe066e óta: #109 vonaljegy és gyűjtőjegy, #112 fagyasztóláda és jégkrémek, CI-kapu (#142) és agent-skill (#146), ellenőrzési hurok megerősítése (#152, #160) és a #112 utóhatásainak rendezése (#154), reputáció választék-metrika (#156), LaVazze Barista kávégép (#162), 17:00 utáni érkezésstop (#164), NPC útvonal simítás (#166), forgalom-követő kosz (#168), egyenkénti Dnyipro-kipakolás (#169), változatos kérések és mondatok (#174), párbeszéd-terv (#172/#173). Részletek a `Docs/Workflow/STATUS.md`-ben és a hivatkozott issue-kban.

## Ellenőrzési alap

A befejezett helyi QA-ra támaszkodunk: teljes `checkout` profil **57 suite / 1633 állítás, 0 hiba** (a kiadott tartalommal azonos runtime-kódon; azóta csak dokumentáció és STATUS változott), plusz a kávégép (35) és a reputáció-választék (17) WebGL-bizonyítékai, 0 konzolhiba; a 720p/1080p képek átnézve. A felhasználó korábbi kérése szerint publikáláskor nem ismételtünk játék- vagy böngészőtesztet; a pontos kiadási commit GitHub Pages built státuszát és a deployment sikerét ellenőriztük (fent).

A meglévő helyi anyag-/render-/projektbeállításokat megőriztük; nem kerültek forráscommitba. A jelenetet nem generáltuk vagy mentettük újra.

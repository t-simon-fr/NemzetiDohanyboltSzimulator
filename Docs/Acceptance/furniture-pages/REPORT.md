# Bútorvásárlás és korábbi fejlesztések — Pages-kiadás

A felhasználó engedélyével a main legfrissebb játékverziója megjelent GitHub Pagesen.

- Forrás: `8fe066ed93b040435c9fe4e753e5349a9c9fe13b`; [forráslenyomat](source.json).
- Publish commit: `0617fa1d0496cca0fb098d00f29d9bc3347782b5`; [Pages státusz](pages.json): built, hiba nélkül. A github-pages deployment is success.
- [Shipping build](build.json): sikeres, 0 hiba, 4 figyelmeztetés, 27 100 070 bájt, 434,78 másodperc. Acceptance probe nélkül.
- [Release manifest és fájlhash-ek](release.json); cache-verzió: `8fe066e`, a loader/data/framework/wasm hivatkozásokban is.
- [Élő játék](https://t-simon-fr.github.io/NemzetiDohanyboltSzimulator-WebGL/?v=8fe066e).

A kiadás az előző Pages-verzió óta elkészült összes main-fejlesztést tartalmazza: [#128 bútorvásárlás az átrendezőben és rácsigazítás](../furniture-planning/REPORT.md), [#123 üzlet-/raktárszintek és moduláris bútorok](../expansion/REPORT.md), [#125 Dnyipro](../dnyipro/REPORT.md), [#106 lottópont](../lottery-point/REPORT.md), [#115/#116 Albro és La Vazze, kattintásos feltöltés](../ingredient-packaging/REPORT.md).

A befejezett helyi QA-ra támaszkodunk: a végső #128 checkout 1080 natív állítás / 46 suite, 17 vásárlási és 15 készletes átrendezési WebGL-ellenőrzés, 0 konzolhiba; a 720p/1080p képek átnézve. A felhasználó korábbi kérése szerint publikáláskor nem ismételtünk játék- vagy böngészőtesztet. A pontos publish commit GitHub Pages built státuszát és deployment sikerét ellenőriztük.

A meglévő helyi anyag-/render-/projektbeállításokat megőriztük; nem kerültek forráscommitba. A jelenetet nem generáltuk vagy mentettük újra. A közzétett csomag tényleges munkapéldányát a forráslenyomat rögzíti.

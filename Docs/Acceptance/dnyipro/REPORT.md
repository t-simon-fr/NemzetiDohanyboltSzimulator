# #125 – Dnyipro csomagolás

A meglévő csempészcigaretta és szállítódoboz a [jóváhagyott referencia](../../Design/Dnyipro/reference.png) alapján kapott új, játékhoz igazított megjelenést: sötétkék/krém papír, kék-sárga sáv, felirat, fedélvarrat, enyhén kopott papírélek, kötözés és kézírásos kartonjelölés. A nyitható cigarettásdoboz a referencia része; új nyitogatási mechanika nem készült.

A `ShopDnyiproVisuals` a meglévő termék- és kartongyökérre épít; a méret, felvételi ütköző, kézpozíció, termékazonosító, ár és kibontási készletlogika megmarad. A díszítő primitívek ütközői letiltva/törölve, a létrehozott anyagokat az objektum saját `ShopProductMaterials` komponense takarítja fel.

## Ellenőrzés

- [Natív jelentés](unity.json): 49 sikeres állítás (`feature-batch` 22, `door-storage` 27). Változatlan vásárlási költség, egyszeri esti vásárlás, teljes tároló esetén veszteségmentes elutasítás, pontos kibontott készlet és új játék vizsgálva. Két korai futásnál a domain reload megszakította a kapcsolatot; a sikeres ismétlés friss jelentése szerepel itt.
- [Forrás](source.json): a végleges csomagolás és az átnézett környezet fingerprintje. A helyi felhasználói anyag-/projektbeállításokat, Recovery/Data fájlokat megőriztük; nem részei a PR-nek.
- A kezdeti WebGL-kör valódi inputtal működött, de vizuálisan túlméretezett feliratot mutatott. A végső javítás csökkenti a feliratot, és enyhe anyag-emisszióval olvashatóbbá teszi az esti papírcímkét. Emiatt friss build és ismételt célzott böngészős kör szükséges.

Futtatás: `node Tools/Verify-Acceptance.cjs --case=dnyipro`. A fixture a kereskedőt/időt/célzást készíti elő; a vásárlás valódi egérkattintás, a karton felvétele és pult alá kibontása tényleges E-bemenet.

Végleges WebGL: **5 sikeres ellenőrzés, 0 konzolhiba**. [Böngésző](browser.json), [build](build.json): sikeres, 0 hiba / 3 figyelmeztetés. Reviewer: Codex saját review, 2026-11-06; [karton](1280x720-dnyipro-carton.png), [pult alatti csomagok 720p](1280x720-dnyipro-packs.png), [1080p](1920x1080-dnyipro-packs-large.png) átnézve. A felirat a címkén belül marad, a márkanév felismerhető; a csomag az esti fényben szándékosan visszafogott. Játékbeli primitív modell, nem a render teljes fotórealisztikus részletessége. A végleges build a 86db5a1 runtime-commitból készült. Eredmény: passed. Pagesre nincs kiadva.

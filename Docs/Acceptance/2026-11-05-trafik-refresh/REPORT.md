# Trafik arculat és minimalista TobaccOS

Engedélyezett feladatok: #87, #88, #89. PR #90 és #91. A felhasználó az elfogadott referenciákat kérte megvalósítani, a számítógépen kevesebb díszítéssel és felesleges szöveg nélkül.

## Megvalósítás

- Barna/krém/zöld TobaccOS, visszafogott trikolór, nagyobb feliratok, közös méretezés és billentyűs célpontok. Az asztal hat alkalmazása, a rendelés és fejlesztés, a kávé, kamerák és két minijáték a meglévő játékmeneti műveleteket használja. A monitoron kívülről is megjelenik az új alapszín és asztal.
- Kategóriás árlista: dohány, üdítők/víz, alkohol, kávé. A begépelt ár csak Enter vagy a mező elhagyása után érvényesül; Esc megszakítja a szerkesztést a számítógép bezárása nélkül. Két −/+ gomb, 100 Ft vagy Shift mellett 5 Ft; alapár-visszaállítás és rövid inline hibajelzés. Az árhatárok és a már leadott rendelések ára megmarad.
- Trafikportál, T-jel, takart kirakat, visszafogott tető/eresz, kerítés és kuka. Egységes barna falburkolat, polcok és pultfelület, aprócikkes vitrin. A tető a kezdő- és bővített területet követi. A vitrin aprócikkei vizuális díszletek; új eladási terméket nem vezetnek be.
- A meglévő bal oldali pult, ajtó, feltöltési sáv és fizikai ütközők megmaradnak. A scene nem lett újragenerálva vagy mentve; a materialok saját runtime-példányok. A felhasználó külön render-/projektbeállításait és Recovery/Data fájljait megőriztük.

## Ellenőrzés és forrásazonosság

- [Teljes natív baseline](unity-baseline.json): **766 sikeres állítás / 34 suite**.
- [Végső célzott kör](unity-final-targeted.json): **98 állítás / 5 suite**. A teljes kör utáni érintett változások: kezdő/bővített tető határa, rendelő Tab-navigáció, Shift-es árfinomítás és a böngészős fixture. A teljes 766-os kört baseline-ként visszük tovább; nem állítjuk, hogy az utolsó forrásból új teljes futás történt. A célzott kör a price-editor, computer-ui, identity-visuals, floor-plan és arrangement viselkedést frissen ellenőrzi.
- [Végső inputpróba](unity-final-input.json): **44 friss állítás** a böngészőben talált mínuszjel- és Esc-fókuszjavítás, valamint az üres kategória gyorsbillentyű-védelme után. A WebGL naplókorlát miatt a probe a teljes JSON-t kis darabokban küldi. Egy kizárólag diagnosztikai futás az ismert Esc-hibát átugrotta; ez nem elfogadási bizonyíték. A lenti végső futás minden feltételt megkerülés nélkül ellenőriz.
- [Végső buildforrás](source.json), [sikeres helyi WebGL-build](webgl-build.json), [böngészős eredmény](browser.json): **31 sikeres ellenőrzés, 0 konzolhiba**. A végleges main tartalmát merge után a tesztelt ághoz hasonlítjuk; változatlan forráshoz nem indul új build.
- A build után csak a böngészős tesztfuttatóban változott egy előkészítő kattintás: az emlékezett kategóriát explicit a dohányfülre állítja. A [runner forrásazonosítója](runner-source.json) külön rögzített; az Assets runtime-tartalma a buildforráshoz képest változatlan.
- Valós CDP-kattintás és gépelés: kosárrendelés, bővítés/kávégép vásárlás, ármező Enter/Esc/hibás érték/mezőelhagyás, −/+ és Shift, kategóriaváltás, tejrendelés és receptválasztás, kamerák, memóriajáték, átrendezési felülnézet. 1280×720 és 1920×1080 nézet.
- Reviewer: Codex. A natív Game view és a végső böngészőképek ténylegesen átnézve. A natív képek a vizuális baseline-t, a végső WebGL-képek a végső forrást mutatják. Javítva az elsőre üres kamerakép, a különálló falburkolat-folt és a meg nem vásárolt terület fölé nyúló tető. Fordításkor előfordult domain reload miatti átmeneti pipeline-kapcsolatszakadás; a sikeres futások friss jelentései szerepelnek itt.
- Az Editor delayCall indítása várakozó állapotban maradt; ezt a friss buildstátusz alapján észleltük, és közvetlen Editor buildhívással indítottuk el. A többperces build közben a HTTP-kérés időtúlléphet, a friss build-result.json jelenti a tényleges eredményt.
- Pagesre **nincs publikálva**; az acceptance player nem shipping kiadás.

## Képek

- [Asztal](1280x720-desktop.png), [rendelés](1280x720-orders.png), [fejlesztések](1280x720-upgrades.png).
- [Árak](1280x720-prices.png), [hibás bevitel](1280x720-price-invalid.png), [kávé](1280x720-coffee.png).
- [Kamerák](1280x720-cameras.png), [játékok](1280x720-games.png), [memória](1280x720-memory.png), [átrendezés](1280x720-arrangement.png).
- [Külső](1920x1080-exterior-large.png), [belső](1920x1080-interior-large.png). A koncepciót egyszerű, a jelenlegi játékhoz illő geometriával valósítottuk meg, nem a generált képek fotószerű részletességét célozva.
- [Natív asztal](native-desktop.png), [natív árak](native-prices.png), [natív kamerák](native-cameras.png).

# #162 - Ellenőrzési jelentés (kávégép modellcsere)

A kávégép runtime primitív modelljét Blender-szkripttel generált, szerkeszthető forrású modell váltja (`Tools/Art/Build-CoffeeMachine.py` → `ArtSource/CoffeeMachine/`, prefab: `Assets/Game/Resources/Equipment/CoffeeMachine/`). A kijelző, a kiadó slot, a célpont, a felirat és az ütközési boríték pozíciója változatlan; a tekerő és a 2 fizikai gomb dísz. A gép felirata és a kávés segédszövegek „LaVazze Barista" lettek; a bolti külső felirat maradt.

## Natív ellenőrzés

Célzott kör: **coffee-world 29, coffee-display 37, hot-drinks-state 130, hot-drinks-integration 48 — összesen 244 állítás, 0 hiba** (`Builds/Verification/20261115-195233-630/report.json`). A `coffee-display` suite egy elavult alkatrész-névre hivatkozott („Coffee machine body"); a megőrzött boríték-ütközőre („Coffee machine body collider") mutat, az állítás tartalma változatlan.

Natív Game view képek (lejátszó kamera, nem acceptance-fixture): [kijelző közelről](native-machine-display.png) — receptek, státusz, prompt aktív; [gép oldalról](native-machine-side.png) — test, krém panel, tálca, tartály; [felirat](native-machine-sign.png) — „LaVazze Barista" névtábla, sötétzöld betűk krém alapon.

## WebGL

Build: **0 hiba / 3 figyelmeztetés, 331,3 s, 27 216 407 bájt**. A figyelmeztetések a Unity/Emscripten eszközkészletből származnak.

Böngésző: `node Tools/Verify-Acceptance.cjs --case=hot-drinks` → **35 ellenőrzés, 0 konzolhiba**, 12 kép ([jelentés](browser.json)). Valódi E-/egérinput a fizikai gépen: mind az öt recept megnyílik, a főzés pontosan fogyaszt, az átvétel valódi csészét ad. A [tea-nézet](1280x720-hot-drinks-tea-720.png) mutatja az új gépet böngészőben: krém előlap bordó szegéllyel, a működő érintőképernyő, dísz tekerő és gombok, iker-kifolyó, tálca, oldalsó tartály; a súgópanel címe „LaVazze Barista".

## Vizuális és interakciós próba

```text
Forráscommit / sourceFingerprint: 6e2edc60648ce0e7c2e257f5d92fbb4bd7dffe3c / 6545d1176d36d459e6f3289c4c3749bb1e071fc5e16029402f1dc92a76118b45 (dirty: a futás idején a Unity által újraszerializált, nem kapcsolódó assetek)
Környezet és felbontások: headless Chromium, WebGL player, 1280x720 és 1920x1080
Reviewer és időpont: OpenCode agent (nem független reviewer), 2026-11-15
Végigpróbált lépések: gép megvásárlása, 5 recept kiválasztása a fizikai kijelzőn, főzés, csészeátvétel kézbe, alapanyag-feltöltés kézzel, kártyás fizetés teára és forró csokira
Átnézett képek: natív native-machine-{display,side,sign}.png + 12 böngészős kép ebben a mappában
Megfigyelés, talált hibák és javításuk: a Blender-előnézethez képest a játékban is a koncepció színei jelennek meg; a felirat sötétzöldje krém alapon olvasható. Új hiba nem volt, javítás nem kellett.
Eredmény: passed
```

## Határok

- A csészék továbbra is dinamikus játékobjektumok, nem a modell részei; a tekerő és a 2 gomb dísz, nincs új input-mód.
- A forráslenyomat `dirty`: a 4 Unity által újraszerializált, feladathoz nem tartozó asset (anyag, URP beállítások, quality/connect) szerepel benne; a player a fenti commitból készült, a runner stale-ellenőrzése igazolja a források változatlanságát.
- Pages-publikálás nem történt, külön engedélyt igényel.

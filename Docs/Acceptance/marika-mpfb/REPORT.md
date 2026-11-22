# #183 — Marika: MPFB-alapú sztereotip vásárlómodell

Második karakterszelet: Marika (1-es személy) a Pisti-pilot (#181) után. Irány:
félrealisztikus MPFB-női alap + kézzel modellezett Karen-stílus (bob-frizura,
szigorú szemöldök, piros száj-variánsok), időtlen hétköznapi ruha testre festett
zónaként (bordó blúz, szénszürke nadrág, sötét cipő). Nincs szögletes ruha-
geometria: a test sziluettje végig az MPFB-anatómia. Nevek, mondatok és
személyiség-logika változatlan; tolvaj-futás gyorsított séta-átvétellel.
Issue: #183. Forrás: `Tools/Art/Build-Marika.py` (+ `ArtSource/Characters/Marika.*`).
MPFB-kimenet: CC0.

## Játékosi eredmény

Marika új MPFB-testben érkezik: vállig érő barna bob, szigorú szemöldök,
olvasható arc (semleges / mosoly / mogorva / pánik száj-variánsok a fejcsontra
kötve). Séta-, tétlen-, bemutató- és panaszkodó-animációk Blender-átvételekből,
Animator-vezérléssel (sebesség → séta, pultnál bemutatás, panasz → fejrázás).
A sor, kosár, átadás, fizetés, tolvaj-menet és az E-folyamat változatlan.

## Natív ellenőrzés

- Teljes `checkout` profil: **60 suite / 1722 állítás, 0 hiba**, 58,1 s,
  0 transport-retry.
  Jelentés: `Builds/Verification/20261122-162818-009/report.json`.
  Forráslenyomat: `8b7aa9895e84823a21d97744ff7ad15f0527ecb7683dcabe902a154bc2ba752e`.
- Új `customer-marika` suite: **30 állítás** — MPFB-modell összeszerelés
  (test + haj + szemöldökök + 4 száj, Humanoid avatar valid, 4 átvétel bekötve), befoglalóméret
  (fej ~1,80 m, láb a talajon), kosár- és kéz-pozíciók, séta-anim (csont-forgás
  ManualTick alatt), panasz→mogorva / fizetés→semleges / köszönet→mosoly
  szájváltások valós folyamaton át, kártyás elszámolás pénz/készlet
  invariánsokkal. A suite a Pisti-mintát követi.

## Vizuális próba (átnézve, passed)

- Blender-renderek: `ArtSource/Characters/marika-preview-{front,side,walk,face}.png`
  (szemből, oldalról, séta-középállás, Karen-arc).
- Natív Game-view: `native-marika-front.png` (szemből, arc + blúz),
  `native-marika-counter.png` (pultnál hátulról, blúz + bob).
  Átnézve: felismerhető Karen-figura, sima testvonal, nincs szögletes ruha,
  a bemutató kar-animáció olvasható.
- Végigpróbált interakciók: séta a sorban, zoom + RPG-panel, panasz/mosoly
  arcváltások, távozás. Reviewer: Muse Spark (AI), 2026-11-22.

## WebGL-ellenőrzés (valós böngészőinput, 0 konzolhiba)

- Acceptance build a végső forrásból: `success: true`, 0 error / 2 warning, 33034663 byte.
- `--case=marika-handover`: **2/4 PASS** (`Builds/Verification/acceptance-1795364815015`):
  zoom + RPG-panel érkezéskor rendben; a valós E-felvétel NEM veszi fel a
  cigarettát (`held: ''`), így az átadás nem jut el `Payment`-be.
- Ugyanez az E-lépés a **régi Pisti-modellen is FAIL** ugyanazon a builden
  (`--case=pisti-handover`), tehát nem a #183 regressziója, hanem játék-szintű
  WebGL-hiba — külön issue: **#184**. Natív `customer-marika`/`customer-pisti`
  suite-okban a teljes átadás pénz-/készlet-invariánsokkal zöld.
- `--case=dialogue`: **8/8 PASS** ugyanazon a builden.
- A teljes futás továbbra is az `after-hours-upgrades` ponton áll meg — **már a
  main-ágon is törött** (külön issue: #178). Ugyanígy a `stock-orders` fixture
  (külön issue: #179). Egyik sem a #183 regressziója (`errors: []`).

## Állapot

- Implementálva: `feature/183-mpb-marika` ágról (PR következő sorszámmal).
- Unityben ellenőrizve: fenti `checkout` + `customer-marika` jelentések.
- Vizuálisan ellenőrizve: fenti képek átnézve, interakció végigpróbálva.
- WebGL-ben ellenőrizve: fenti build + `marika-handover` (zoom + panel PASS) és
  `dialogue` (8/8) böngészős próba, 0 konzolhiba. A handover E-lépése játék-szintű
  WebGL-hiba miatt bukik (#184, a régi modellre is).
- Publikálva: nincs (külön kérésre).
- Következő szeletek: Józsi + Laci (ugyanez a pipeline).

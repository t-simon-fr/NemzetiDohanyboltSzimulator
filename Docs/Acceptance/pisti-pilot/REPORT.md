# #181 pilot — Pisti: szkriptelt sztereotip vásárlómodell

Pilot-blokk: arányos testű, melegítős-hátizsákos, túlpörgős arcú Pisti; a másik
három figura ebben a szeletben marad a régi modellen. Terv és jóváhagyott
referenciák: `ArtSource/Characters/` (lineup, Karen-jelenet, tolvaj póz-lap).
Issue: #181. (#11 Misi/Nóra lezárva, nem része ennek.)

## Játékosi eredmény

Pisti új, Blender-szkripttel generált testben érkezik: kék melegítő, hátizsák,
tüskés haj, olvasható arc (semleges / mosoly / mogorva / pánik száj-variánsok +
pislogás). Séta- és tolvaj-futás-ciklus, pultnál bemutató kéztartás, tétlen
ringás. A sor, kosár, átadás, fizetés, tolvaj-menet és az E-folyamat változatlan.

## Natív ellenőrzés

- Teljes `checkout` profil: **59 suite / 1692 állítás, 0 hiba**, 50,9 s,
  0 transport-retry.
  Jelentés: `Builds/Verification/20261122-124553-336/report.json`.
- Új `customer-pisti` suite: **25 állítás** — modell-összeszerelés (34 part),
  befoglalóméret (fej ~1,79 m, láb a talajon), kosár- és kéz-pozíciók, séta-anim,
  panasz→mogorva / fizetés→semleges / köszönet→mosoly szájváltások valós
  folyamaton át, kártyás elszámolás pénz/készlet invariánsokkal.
  A suite tudatosan fizikai átadással dolgozik (fixture-lerakás helyett), mert a
  tálca foglaltsága valós viselkedés.

## Vizuális próba (átnézve, passed)

- Natív Game-view: `native-pisti-front.png` (szemből, mosoly előtti semleges arc),
  `native-pisti-present.png` (pultnál bemutató kéztartás).
  Átnézve: felismerhető figura, nincs áthatolás a pulttal, a kar-animáció olvasható.
- WebGL 720p: `webgl-pisti-handover.png` (átadás utáni Fizetés-állapot, panel
  eltűnt, kamera visszaállt), `webgl-dialogue-zoom.png`, `webgl-ui-order.png`.
- Végigpróbált interakciók: E-felvétel a polcról, E-átadás a leolvasóra, E-gépelés,
  T-helyettesítés, távozás. Reviewer: Muse Spark (AI), 2026-11-22.

## WebGL-ellenőrzés (valós böngészőinput, 0 konzolhiba)

- Acceptance build a végső forrásból: `success: true`, 0 error / 5 warning.
- `--case=pisti-handover`: **4/4 PASS** (`Builds/Verification/acceptance-1795352765096`):
  zoom + RPG-panel érkezéskor, valós E-felvétel az új modellel, valós E-átadás
  `Payment`-be, panel- és kamera-visszaállítás.
- `--case=dialogue`: **8/8 PASS**; `--case=ui`: **10/10 PASS**.
- Teljes futás továbbra is az `after-hours-upgrades` ponton áll meg — **már a
  main-ágon is törött**, elavult 3D-gombkoordináták a TobaccOS-héjjal szemben
  (külön issue: #178). Ugyanígy a `stock-orders` fixture elavult ital-célzása
  (külön issue: #179). Egyik sem a #181 regressziója (`errors: []`).

## Állapot

- Implementálva: `feature/181-pisti-pilot` ágról (PR következő sorszámmal).
- Unityben ellenőrizve: fenti `checkout` + `customer-pisti` jelentések.
- Vizuálisan ellenőrizve: fenti képek átnézve, interakció végigpróbálva.
- WebGL-ben ellenőrizve: fenti build + `pisti-handover`/`dialogue`/`ui` böngészős próba, 0 konzolhiba.
- Publikálva: nincs (külön kérésre).
- Következő szeletek: Marika, Józsi + Laci (ugyanez a pipeline).

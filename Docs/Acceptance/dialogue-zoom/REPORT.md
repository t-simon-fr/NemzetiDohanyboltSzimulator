# #175 — RPG dialóguspanel kamerazoommal + összesítő-felújítás

Elfogadási jelentés. Terv: [Docs/Design/Dialogue/README.md](../../Design/Dialogue/README.md).
Issue-k: #175 (implementáció), #172 (spike/terv), #174 (változatosság-motor, már publikálva).

## Játékosi eredmény

A vevő megérkezésekor a kamera finoman ráközelít (FOV 60 → 48, allokáció nélkül) és alul
RPG-dialóguspanel mutatja a mondatot: monogram-chip a meglévő `ShopPeople.Colors`-ból,
hangulatcímke (`RUTINOS` / `BESZÉLGETŐS` / `SIETŐS` / `MUNKÁBA`, törzsvendégnél
`TÖRZSVENDÉG`), gépelős kiírás 45 karakter/mp-cel. Az E a mondatot mellékhatásként
befejezi, de világ-akciót sosem eszik meg — az átadás/felvétel folyamata változatlan.
Fizetés/köszönés/távozás után a FOV és a panel visszaáll. Az összesítő a jelenlegi
helyén újult meg (név + hangulat, tételes sorok státusszal, összeg), gombok nélkül —
az átadás marad fizikai.

Kizárva a terv szerint: portré-rajzolás (monogram most, arcok #11-ben), dolly-kamera,
új input-mód.

## Natív ellenőrzés

- Teljes `checkout` profil: **58 suite / 1665 állítás, 0 hiba**, 50,7 s, 0 transport-retry.
  Jelentés: `Builds/Verification/20261122-114121-715/report.json`.
  Forráslenyomat: `28d1871a…` (a futás alatt változatlan).
- Új `dialogue-zoom` suite (**34 állítás**): zoom be/kikapcsolás fázisonként, gépelés +
  E-befejezés világ-akció fogyasztása nélkül (`Completing text never consumes the world
  action`), átadás gépelés közben extra E nélkül (`Handover works mid-typing...`),
  gyors egymásután, távozás mondat közben, fókusz/szünet védelme, FOV-visszaállítás,
  pénz/készlet invariánsok, hangulat/monogram. Jelentés:
  `Builds/Verification/20261122-114056-766/report.json`.
- Fordítás tiszta; `WorkflowRunnerChecks` a CI-kapun fut.

Közben talált és javított viselkedéshiba: az első változatban az E gépelés közben
fogyasztotta a világ-akciót, így kézben tartott áruval az első E nem adott át
(`stock-orders` WebGL-eset buktatta le). Javítva mellékhatás-gépelésre; a natív
regresszió a suite-ban.

## Vizuális próba (átnézve, passed)

- Natív Game-view: `native-dialogue-typing.png` (gépelés közben, `[E] Folytatás`,
  zoomolt nézet), `native-dialogue-substituted.png` (E-léptetett + T-helyettesített
  mondat, frissült összesítő: Marlbroke, 2 300 Ft). Átnézve: nincs átfedés a
  világ-hint és az RPG-panel között (az első capture-on talált átfedést javítottam:
  a hint a panel láthatósága alatt rejtve marad).
- WebGL 720p (`1280x720-dialogue-zoom.png`, gépelés közben, tördelt 2 sor) és 1080p
  (`1920x1080-dialogue-zoom-large.png`, E-vel befejezett hosszú mondat) átnézve.
- Végigpróbált interakciók: zoom + panel megjelenik Orderben; E befejezi a gépelést;
  T helyettesít (Marlbroke); summary frissül; távozáskor visszaáll a kamera.
- Reviewer: Muse Spark (AI), 2026-11-22. Eredmény: **passed**.

## WebGL-ellenőrzés (valós böngészőinput, 0 konzolhiba)

- Acceptance build a végső forrásból: `success: true`, 0 error / 3 warning, 258,7 s.
  Staleness-kapu tiszta (a build újabb minden `Assets/` forrásnál).
- `--case=dialogue`: **8/8 PASS** (`Builds/Verification/acceptance-1795348008634`):
  zoom + panel érkezéskor, fokozatos gépelés, E befejez valós töltés nélkül,
  T-helyettesítés, elutasító távozás lencse-visszaállítással.
- `--case=ui`: **10/10 PASS** (`Builds/Verification/acceptance-1795348054388`):
  megújult összesítő valós átadással, kártyás fizetés, elszámolás, menü — mindkét
  felbontáson.
- Teljes futás (`Builds/Verification/acceptance-1795347304544`): minden eset zöld az
  `after-hours-upgrades` pontig, amely **már a main-ágon is törött** (elavult
  3D-gombkoordináták a TobaccOS-héjjal szemben) → külön issue: #178. Ugyanígy a
  `stock-orders` fixture elavult ital-célzása → külön issue: #179. Egyik sem a #175
  regressziója (a bukott állapotokban `errors=[]`, a mechanizmus a #175-öt nem
  érintő főági UI-evolúció).

## Állapot

- Implementálva: `feature/175-dialogue-zoom-panel` (PR: #177 utáni sorszám).
- Unityben ellenőrizve: fenti `checkout` + `dialogue-zoom` jelentések.
- Vizuálisan ellenőrizve: fenti képek átnézve, interakció végigpróbálva.
- WebGL-ben ellenőrizve: fenti build + `dialogue`/`ui` böngészős próba, 0 konzolhiba.
- Publikálva: nincs (külön kérésre).

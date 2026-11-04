# Aktuális állapot

A GitHub issue-k az elsődleges feladatlista. A `Docs/CONTINUE.md` régebbi részei történetiek.

- #2–#8: implementáció, Unity-integrációs és helyi WebGL-bemeneti ellenőrzés kész. Részletek: [elfogadási jelentés](../Acceptance/2026-11-03/REPORT.md).
- Aktuális checkout profil: **145 sikeres állítás**, nyolc csoport. Böngésző: **34 különböző sikeres ellenőrzés**, 0 console error a végleges futásokban.
- Javítva az elkészült kávé téves hiányjelzése és a HUD-on ismétlődő vevői mondat.
- #11: Misi és Nóra saját karakterkoncepciója, abszurd személyisége és dialógusterve elkészült a `docs/11-character-concept` ágon. A 3D-modellek és játékbeli dialógus-integráció még nincs implementálva.
- A kassza/kéz grafikai átdolgozását a felhasználó félretette. A pult alatti csempészáru továbbra is terv. Az ajtó/tábla alapú napváltás most engedélyezett munka (#13).
- Az előző kiadás Pagesre került: forrás `ef5da37`, publish `29af415`. Az új #13 változtatás még nincs publikálva. A helyi acceptance WebGL-build külön tesztplayer; a shipping buildben nincs tesztprobe. Publikálás csak külön kérésre.

## Parancsok

```powershell
pwsh -File Tools/Checks/WorkflowRunnerChecks.ps1
pwsh -File Tools/Verify-Project.ps1 -Profile checkout
# Sikeres helyi acceptance build után:
node Tools/Verify-Acceptance.cjs
```

Workflow: [WORKFLOW.md](../WORKFLOW.md). Böngészős előfeltételek: [Acceptance/README.md](../Acceptance/README.md). Minden új futás saját jelentést készít; az itt hivatkozott eredmények csak a jelentésben azonosított tartalomra érvényesek.

## #13 – Ajtó és műszakkezelés

- Implementálva a feature/13-door-day-flow ágon: NPC-k által mozgatott fizikai ajtó, játékost blokkoló bejárat; E-s nyitvatartási tábla; kézi naplezárás az ajtónál.
- Üdvözlő és külön vevői párbeszédmodal eltávolítva; T: helyettesítés, X: elutasítás a HUD mellett.
- Az üres zárt bolt automatikusan esti állapotba kerül, könyvelési zárás csak távozáskor történik.
- Unity: 145 sikeres állítás, nyolc suite. Helyi WebGL: 14 sikeres ellenőrzés, 0 konzolhiba. Vizuális próba: passed, 1280×720 és 1920×1080. [Tartós jelentés és képek](../Acceptance/2026-11-04/REPORT.md).
- Még nincs mainre mergelve vagy Pagesre publikálva.

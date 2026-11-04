# Aktuális állapot

A GitHub issue-k az elsődleges feladatlista. A `Docs/CONTINUE.md` régebbi részei történetiek.

- #2–#8: implementáció, Unity-integrációs és helyi WebGL-bemeneti ellenőrzés kész. Részletek: [elfogadási jelentés](../Acceptance/2026-11-03/REPORT.md).
- Aktuális checkout profil: **192 sikeres állítás**, tíz csoport. Böngésző: **58 sikeres ellenőrzés**, 0 console error a végleges futásokban.
- Javítva az elkészült kávé téves hiányjelzése és a HUD-on ismétlődő vevői mondat.
- #11: Misi és Nóra saját karakterkoncepciója, abszurd személyisége és dialógusterve elkészült a `docs/11-character-concept` ágon. A 3D-modellek és játékbeli dialógus-integráció még nincs implementálva.
- A kassza/kéz grafikai átdolgozását a felhasználó félretette. A pult alatti csempészáru továbbra is terv. Az ajtó/tábla alapú napváltás most engedélyezett munka (#13).
- Az előző kiadás Pagesre került: forrás `ef5da37`, publish `29af415`. A #13 változtatás azóta kiadva: forrás `6c39f37`, publish `bfd4519`. A helyi acceptance WebGL-build külön tesztplayer; a shipping buildben nincs tesztprobe. Publikálás csak külön kérésre.

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
- Mainre mergelve és Pagesre publikálva: `6c39f37` / `bfd4519`; élő indulás ellenőrizve.

## #15 – Alaprajz és átrendezés

- Felhasználó által jóváhagyott implementáció; kapcsolt csomagok: #16, #17, #18.
- A feature/15-shop-layout ágon elkészült a bal oldali pult, a kezdő területen belül maradó bejárat, a kartonos személyzeti útvonal, a berendezések adatmodellje és az A* útkeresés.
- A TobaccOS-ból felülnézeti átrendezés indulhat; polc/hűtő, forgatás, előnézet, közlekedési ellenőrzés, mentés/megszakítás és következő napi megőrzés.
- Unity: 192 sikeres állítás. Teljes helyi WebGL: 58 sikeres ellenőrzés, 0 konzolhiba; külön 15 ellenőrzéses átrendezési próba a kisebb nézetben. Vizuális próba: passed, 1280×720 és 1920×1080. [Jelentés](../Acceptance/2026-11-04-layout/REPORT.md).
- Ez a csomag nincs mainre mergelve vagy Pagesre kiadva.

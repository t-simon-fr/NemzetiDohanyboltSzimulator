# Aktuális állapot

A GitHub issue-k az elsődleges feladatlista. A `Docs/CONTINUE.md` régebbi részei történetiek.

- Legutóbbi játékfunkciók: `dee3417`, `Docs/CheckoutRevisionRelease.md`.
- A hét új funkció implementált; korábbi Unity-ellenőrzés: 70 sikeres állítás, a kiadási jegyzetben felsorolt hatókörrel.
- A teljes vizuális/interakciós próba és az új WebGL-build még nincs kész. A képrögzítés önmagában nem igazol használhatóságot.
- Pages nem frissült ezzel a csomaggal. Publikálás csak külön kérésre.
- Új workflow: `Docs/WORKFLOW.md`, `AGENTS.md`, GitHub issue/PR sablonok, `Tools/Verify-Project.ps1`, `Tools/Capture-Review.ps1`.
- A tervezett ajtó/tábla alapú napváltás és pult alatti csempészáru továbbra is terv.

## Aktuális parancsok

```powershell
pwsh -File Tools/Checks/WorkflowRunnerChecks.ps1
pwsh -File Tools/Verify-Project.ps1 -Profile checkout
pwsh -File Tools/Capture-Review.ps1 -Scenario main-menu
```

Minden futás egyedi, helyi jelentést készít a Builds/Verification könyvtárba. Az issue-ban megőrzendő a commit, a sourceFingerprint, a próba hatóköre és eredménye; megosztott vizuális bizonyítékhoz tartós artifact-link szükséges.

## Önálló feladatok

| Issue | Funkció | Állapot |
|---|---|---|
| [#2](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/2) | Kártyás összegbevitel | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#3](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/3) | Egységes kassza | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#4](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/4) | Két pultzóna | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#5](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/5) | Bal klikk | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#6](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/6) | Kávérecept | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#7](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/7) | Menük és vezérlés | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |
| [#8](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/8) | Párbeszéd | Implementálva; részletes bizonyíték és hiányzó próbák az issue-ban. |

Workflow-bevezetés: [#9](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/issues/9). Végrehajtási bizonyítékok: [EVIDENCE.md](EVIDENCE.md).

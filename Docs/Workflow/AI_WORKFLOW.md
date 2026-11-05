# #61 – AI-fejlesztési workflow

2026-11-05. Cél: kevesebb felesleges kör, rövidebb várakozás, megőrzött pénz/készlet/állapot-biztonság. A napi folyamat szabályainak gazdája a [WORKFLOW.md](../WORKFLOW.md); ez a spike a döntések indokát és pilotját rögzíti. Az [AGENTS.md](../../AGENTS.md) rövid belépési pont marad.

## Audit és döntések

| Megfigyelés | Bizonyíték | Bevezetett változtatás |
|---|---|---|
| A meglévő issue/PR alap, kötelező fordítás és hibánál megállás használható. | Tools/Verify-Project.ps1, Workflow.Common.psm1, #9 | Megtartjuk az eszközöket; nincs második pipeline vagy külön agent-keretrendszer. |
| A runner csak smoke/checkout profilt adott, a célzott körhöz kézi script run parancs kellett. | A spike előtti Verify-Project paraméterlistája; 26 suite a checkout profilban | Explicit `-Suite`, előzetes registry-validálás, deduplikálás és `-PlanOnly`. A régi profilok kompatibilisek. |
| A fordítás/linkelés sokkal nagyobb várakozás, mint a natív assertionök darabszáma önmagában. | [462,9 s első WebGL-build](../Acceptance/2026-11-05-feature-batch/webgl-initial-build.json), [252,1 s feliratjavítás](../Acceptance/2026-11-05-feature-batch/webgl-final-build.json), [426,3 s shipping](../Acceptance/2026-11-05-feature-pages/build.json) | A stabil runtime és natív vizuális kör után egy közös acceptance build; új build csak releváns változás/hiba miatt. A shipping továbbra is külön, probe nélküli build. |
| A képek csak a build után árultak el egy WebGL-betűkészlet-problémát. | [Feature-jelentés](../Acceptance/2026-11-05-feature-batch/REPORT.md) | Előbb natív interakció és egy vizuális passz, majd WebGL-specifikus review. Valós eltérésnél a szükséges új build és célzott újrapróba indokolt. |
| A dokumentációs feladatnak nem volt önálló, megismételhető helyi linkellenőrzője. | A spike előtti Tools fájllista; a #60 csak dokumentációt módosít | `Tools/Review-Docs.ps1`: helyi relatív hivatkozások, repo-határok; nincs Unity vagy hálózati kérés. Nem állítja, hogy távoli URL vagy Markdown-anchor érvényes. |
| Régi PR-sablon minden tasknál Unity/vizuális/WebGL pipát sugallt, kiadásnál mindig élő játékpróbát írt elő. | Korábbi .github/pull_request_template.md és WORKFLOW 5. pont | Ellenőrzési kategória és indokolt N/A mezők; engedélyezett kiadásnál a már sikeres helyi QA nem ismétlődik, az adott deploy commit built státusza kötelező. |
| A taskok és forrásállapotok hosszú beszélgetésben könnyen összekeverhetők. | Történeti STATUS-bejegyzések és a friss kiadási jelentések eltérő dátuma | Rövid, aktuális folytatási bejegyzés és egy közös jelentés; történeti bizonyíték nem minősül új futásnak. |

A három fenti build összesen 1141,3 másodperc (19 perc) gépi buildidő. Ez egy korábbi csomag mért adata, nem jövőbeli időígéret. Az első két buildet valós megjelenítési eltérés indokolta; a shipping build a probe kizárása miatt kellett. A workflow célja a szükségtelen plusz körök elkerülése.

## AI és emberi szerepek

Alapértelmezésben egy végrehajtó dolgozik. Az audit, implementáció és diff-review külön munkalépés, nem automatikusan három agent. A diffet a változtatás lezárása után kell újra végigolvasni, és megnevezni a felülvizsgált invariánsokat; ugyanannak az agentnek a review-ja nem független review.

Alügynök csak kifejezett felhasználói vagy magasabb szintű engedély esetén indulhat. Ilyenkor önálló, közös fájlt nem író feladatot kapjon: például olvasási audit vagy külön modul. Kódírásnál legyen fájltulajdonos és szükség esetén külön worktree, egy integrációs gazdával. Unity/Play/build egyszerre egy tulajdonosé ugyanazon projektben. A függő modulok párhuzamos írásától nem várunk automatikus gyorsulást.

A felhasználótól termékdöntést kell kérni, ha több eltérő játékosi működés lehetséges. A többi önálló munka közben folytatható. A már engedélyezett batch, szükséges javítás és kész PR merge miatt nem kérünk újra engedélyt. Új scope, romboló lépés és külön nem engedélyezett publikálás döntési pont marad. A #60-ból keletkezett #69–#74 refaktorok külön feladatok, nem indultak el e spike címén.

## Eszközök

```powershell
# Csak terv: nem indít Unity-t, és nem hoz létre verification futást.
./Tools/Verify-Project.ps1 -Suite ledger,coffee-display -PlanOnly

# Két kiválasztott suite, ugyanazzal a fordítási/Play/hibakezelési úttal.
./Tools/Verify-Project.ps1 -Suite ledger,coffee-display

# A régi integrációs belépési pont megmarad.
./Tools/Verify-Project.ps1 -Profile checkout

# Dokumentációs feladat: csak helyi relatív linkek.
./Tools/Review-Docs.ps1 -Path Docs/Architecture/MODULARIZATION.md,Docs/Workflow/AI_WORKFLOW.md
./Tools/Checks/WorkflowRunnerChecks.ps1
```

A `-Suite` felülírja a profil választását, az ismételt suite-ot egyszer futtatja, az ismeretlen/üres választást még Unity-hívás előtt elutasítja. A suite neveit és célját a `Tools/Verification/suites.json` határozza meg. A jelentés a korábbi mezők mellett a választott suite-okat, a fordítás, az egyes suite-ok és a teljes futás időtartamát is rögzíti.

A scriptet PowerShellben, a fenti tömbszintaxissal használd több suite-hoz. Más shellből indított `pwsh -File` tömbparaméter-átadását ne feltételezd automatikusan: ilyen esetben `pwsh -Command` alatt hívd a scriptet vagy adj meg egy suite-ot.

## Pilot és értékelés

A valós pilot a #60 dokumentációs feladat átadása az új folyamat szerint: scope → kódhivatkozásokkal alátámasztott terv → hat kis follow-up issue → diff és helyi linkellenőrzés → külön PR/merge. Ehhez nem indult Unity/WebGL. A natív runner változásához külön, két suite-os integrációs próba tartozik, a pénz- és kávéműködés módosítása nélkül.

A friss eredményeket és időket a [pilot-jelentés](Evidence/2026-11-05-ai-pilot.json) rögzíti. A bizonyíték külön kezeli a docs-pilotot, a runner önellenőrzését, a tervkimenetet és a valós célzott Unity-futást. Nem A/B sebességbenchmark: eltérő feladatok és gépterhelés mellett az időket nem általánosítjuk.

Eredmény: 15 eszköz-önellenőrzés sikeres; a két kiválasztott suite 49 állítása 2,85 másodperces teljes runner-körben futott le. A négy dokumentum relatív linkellenőrzése 0,05 másodperc volt, Unity-hívás és WebGL-build nélkül. Az első valós runner-próba a PowerShell `Suite` paraméter és belső `suite` változó névütközését találta meg; külön változónévvel javítottuk, majd a célzott próbát sikeresen megismételtük. A kódreview során a tervkimenetet, a hibás suite elutasítását, a régi profil feloldását, a linkellenőrzés repo-határát és a report-időzítést is átnéztük; független reviewer nem futott.

Sikerkritérium: a docs-pilot 0 Unity/0 WebGL kört kér; a célzott runner kizárólag két kiválasztott suite-ot futtat a 26-os profil helyett; ismeretlen suite és hiányzó dokumentum nem fogadható el; a régi smoke/checkout útvonal megmarad. A minőségi követelményeket a változtatás típusa és kockázata határozza meg, nem a futtatott tesztek minél nagyobb száma.

Az első éles feature után a mért suite/build idő és az indokolatlan újrafuttatások száma alapján érdemes finomítani. Most további workflow-automatizmus nem indokolt; a következő fejlesztési munka a #60-ban kijelölt moduláris backlog priorizálása.

# Fejlesztési munkafolyamat

## 1. Feladat és elfogadási feltételek

GitHub issue az önálló funkció vagy bug alapegysége. A #1 issue megmarad történeti gyűjtőnek; az új munkát a kapcsolt issue-kban követjük. Használd a feature/bug issue-sablont. Írd le a játékos által megfigyelhető eredményt, a függőségeket és 3–6 ellenőrizhető feltételt. Az elfogadott ötlet és az engedélyezett implementáció külön fogalom.

## 2. Kis munkacsomag

Egy issue → rövid terv → `feature/<issue>-<slug>` vagy `fix/<issue>-<slug>` ág → megvalósítás → ellenőrzés → külön commit → PR a sablonnal. Nagy issue-t bonts kisebb, külön ellenőrizhető részekre. Minden commit működő állapotot adjon; kapcsolódó refaktor mehet vele, idegen funkció nem. Korábbi, már mainre került csomagokat nem írunk át utólag. A user által kért közvetlen main munka felülírja az ág/PR alapértelmezést.

## 3. Egységes ellenőrzés

Unityben a ShopPrototype projekt legyen megnyitva. Mentsd a munkádat; a futtató kilép a Play módból, fordít, majd új Play példányon tesztel, végül kilép. A futtató nem ment vagy generál jelenetet.

```powershell
pwsh -File Tools/Verify-Project.ps1 -Profile smoke
pwsh -File Tools/Verify-Project.ps1 -Profile checkout
pwsh -File Tools/Checks/WorkflowRunnerChecks.ps1
```

`smoke`: ledger + aktuális checkout. `checkout`: smoke + teljes többtermékes nap + visszavétel/megszakítás. Minden futás új `Builds/Verification/<timestamp>/report.json` jelentést kap, HEAD-del és az Assets/Packages/ProjectSettings/Tools tartalmának lenyomatával. Hibánál nem futnak tovább a tesztek, a visszatérési kód nem nulla. Az aktuális gépi jelentés nem kerül Gitbe; a bizonyíték összefoglalója és tartós hivatkozása az issue-ba/PR-be kerül.

Új suite bejegyzéshez fájl, belépési pont, érintett profil és cél szükséges. A `CardAndSummaryChecks` régi külön bankszámlás elvárásokat tartalmaz, ezért nem része az aktuális csomagnak. A történeti kiadási jelentések csak a saját forrásváltozatukra érvényesek.

## 4. Vizuális játékpróba

Lásd `Docs/Workflow/VISUAL_REVIEW.md`. Az érintett folyamatot tényleges játékosvezérléssel is próbáld végig, és nézd meg a képeket. Segédparancs:

```powershell
pwsh -File Tools/Capture-Review.ps1 -Scenario main-menu
pwsh -File Tools/Capture-Review.ps1 -Scenario settings
pwsh -File Tools/Capture-Review.ps1 -Scenario current
```

A Game view képet mentő natív ScreenCapture az IMGUI menüket is tartalmazza; a csak kamerát renderelő pipeline screenshot erre nem elegendő. A parancs elmenti a képet és a forráslenyomatot, de nem állítja, hogy a képet valaki átnézte. A main-menu/settings forgatókönyv új játékállapotot készít elő: futtatás előtt ments, és ne használd megőrzendő Play-sessionben.

## 5. Készültség és kiadás

Az alábbi mezők külön bizonyítékok, nem automatikusan egymást követő pipák:

| Állapot | Szükséges bizonyíték |
|---|---|
| Implementálva | forráscommit + elfogadási feltételekhez rendelt változtatások |
| Unityben ellenőrizve | friss fordítás + releváns sikeres futás, profil és forráslenyomat |
| Vizuálisan ellenőrizve | átnézett képek + végigpróbált interakció + reviewer megjegyzése |
| WebGL-ben ellenőrizve | ugyanebből a forrásból készült build + böngészős próba + console eredmény |
| Publikálva | engedély + deploy commit + élő URL és élő ellenőrzés |

Látható funkció issue-ját az elfogadási feltételek, Unity-próba és vizuális próba után zárjuk. Ha WebGL-t érint vagy ez a célplatform, a helyi böngészős próbát is várjuk meg. A publikálás külön kiadási döntés; nem szükséges minden funkcióhoz. A forrás módosítása érvényteleníti a változtatással érintett korábbi bizonyítékot.

A korábbi `Tools/Verify-WebGL.cjs` történeti forgatókönyv. Az aktuális helyi elfogadási player és valódi böngészőbemeneti próba: `ShopAcceptanceBuild.Queue`, majd `node Tools/Verify-Acceptance.cjs`. Indítási feltételek, tesztfixture-hatókör és bizonyítékok: [Docs/Acceptance/README.md](Acceptance/README.md). Ez a folyamat nem publikál Pagesre.

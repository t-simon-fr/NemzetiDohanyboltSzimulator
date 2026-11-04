# #23–#25 – Mozgás és fizikai eszközkezelés

## Megvalósított működés

- **#23 sprint/ugrás:** bal Shift tartása gyorsít (3,5 m/s → 5,775 m/s), Space földről ugrik, alap ugrásmagasság 0,65 m. Átlósan sincs sebességtöbblet, levegőben nincs újabb elrugaszkodás, a Space tartása nem indít automatikus új ugrást. A CharacterController kezeli a talajt, falat és mennyezetet. Menü, eszközkezelés és átrendezés alatt a játékos nem mozog. Mindkét új művelet megjelenik a billentyűbeállításokban és a súgóban; nincs új stamina-rendszer.
- **#24 terminál:** a korábbi dekoráció helyét egy használható fizikai eszköz veszi át a kassza mellett, a visszajáró tálcáját szabadon hagyva. Saját kijelzőn fizetendő/beírt összeg és kész/hibás/sikeres állapot. Szabványos 1–2–3 / 4–5–6 / 7–8–9 / 0 számsorrend, JAV/TÖR/OK/ESC. Az egér és billentyűzet ugyanazt a bevitelt kezeli; hibás/üres vagy hiányos rendelés nem könyvelhető, az elfogadás egyszeri. Siker után a kijelző és fókusz megmarad, új vevő/nap/új játék alaphelyzetbe állítja. A készpénzes kassza külön működik.
- **#25 kávégép:** E a kijelzőn megnyitja a fizikai kezelőt; rövid/hosszú/latte kattintható, a kijelzőn folyamatosan látszik a recept, a kávé/tej készlet, a főzés hátralévő ideje, a kész ital és az alapanyaghiány. Főzés alatt nem váltható recept és nem indítható második főzés. A billentyűzet és TobaccOS ugyanazt az állapotot használja. Kilépés után is folytatódik a főzés; szünetben áll. Az Átveszem gomb a valódi csészét adja kézbe, a kiadó foglalását a meglévő tárgykezelés kezeli.
- A csésze és a gépház nem metszik egymást; a tálca és kifolyó a kiadóhoz igazodik. A felmosó a kezdő bolt jobb hátsó sarkába került, hogy ne álljon a gép kezelőfelülete előtt. A megvásárolt gép és az authored jelenet megmarad; nincs jelenet-újragenerálás.
- Az eszközökön használt Escape visszatér a boltba; a következő Escape nyitja a szünetmenüt. Az eszközgomb kattintása nem használhatja ugyanabban a képkockában a mögötte lévő világot, különösen az ital átvétele és fókusz elhagyása után.

## Unity

- Végleges checkout: **310 sikeres állítás**, 15 suite. A korábbi 234 állítás mellett 12 mozgás-, 29 fizikai terminál- és 35 kávékijelző-ellenőrzés.
- Jelentés: `Builds/Verification/20261104-161348-836/report.json`, tartós másolata `unity-report.json`.
- Végleges forrás: `865dde5`; lenyomat: `feb2a0a5cba156a9ac4479de6bb7bc85b33d2f8eca9024d4ee791308be61dd09`. A jelentés a commit előtt, azonos tartalmon készült (`0a3485e` HEAD, dirty munkafa). A korábbi, idegen lokális módosításokat megőriztük.
- A mozgáspróba valódi CharacterControllerrel ellenőriz talajt, sprintet, átlós sebességet, ugrást, mennyezetet, eszköz- és menügátat. A terminál minden számgombját kivetített sugarakkal éri el; javítás, törlés, hibás/üres beírás, egyszeri jóváhagyás, tényleges kijelző és készpénz-kompatibilitás is ellenőrizve.
- A kávépróba valódi fizikai gombokat céloz: mindhárom ital, egyszeri alapanyag-fogyás, tiltott főzés/receptváltás, fókusz elhagyása, kész csésze, foglalás, hiányjelzések és új játék. A szünetből visszatérés a világban és eszközfókuszban is a megfelelő vezérlést állítja vissza. A csésze a gépháztól szabadon marad, és a kattintás nem teheti vissza rögtön a felvett italt.
- Natív Game view-k átnézve: `visual-20261104-152652-475` (terminál) és `visual-20261104-155016-649` (kész latte). A fizikai billentyűk és kijelzők olvashatók; a kávégép előtt nincs felmosó, a csésze látható.

## WebGL és végleges vizuális próba

Az első integrált böngészős próba a szünetből visszatérve megálló főzést talált: a menü már zárva volt, de a szabad kurzor miatt nem futott a világ Tick-je. Javítva: a szimuláció a tényleges menü/eszközgátat követi, az Escape-et csak a menü/eszköz kezeli, a folytatás a megfelelő kurzor- és eszközfókuszt állítja vissza. A szünet továbbra is megállítja a főzést. Az új buildből a javított folyamat és a teljes ellenőrzés is sikeres.

- Unity 6000.6.7f1, helyi Chromium WebGL, tényleges egér- és billentyűesemények. A tesztfixture csak előkészít és állapotot olvas; a kávéfőzés valós időben fut.
- Build: 2026-11-04 16:18:19 UTC, **0 hiba / 0 figyelmeztetés**, 21 097 217 bájt, 252,44 másodperc. [Buildjelentés](webgl-build.json); a négy buildasset SHA256-lenyomata a böngészős jelentésekben van.
- Teljes futás: **113 sikeres ellenőrzés, 0 konzolhiba**, 16:19:59–16:23:25 UTC. [Tartós összefoglaló](browser-full.json), helyi teljes állapotnapló: `Builds/Verification/acceptance-1793809199429/report.json`.
- Előzetes célzott kávégép-újrapróba ugyanebből a buildből: **19 sikeres ellenőrzés, 0 konzolhiba**. [Jelentés](browser-coffee.json).
- Reviewer: Codex, 2026-11-04. Átnézett nézetek: **1280×720 és 1920×1080**. A fizikai terminál számsorrendje, hibája és sikeres összege olvasható; a segédpanel szabadon hagyja a vezérlőket. A kávégép receptje, haladása, kész csészéje és hiányjelzése olvasható; a tiltott műveletek szürkék. A gombokat ténylegesen kattintva is végigpróbáltuk.
- A teljes futás a korábbi checkout, három azonos termék, tárgykezelés, felmosás, kávé/TobaccOS, párbeszéd, menü/billentyűmegőrzés, UI, átrendezés és ajtó/napváltás folyamatait is ellenőrizte. **Vizuális és interakciós eredmény: passed.**

| Bizonyíték | Kép |
|---|---|
| Terminál: hibás összeg | [1280×720](1280x720-physical-terminal-error.png) |
| Terminál: sikeres fizetés | [1280×720](1280x720-physical-terminal-approved.png) |
| Terminál: teljes kezelő | [1920×1080](1920x1080-physical-terminal-large.png) |
| Kávégép: készenlét | [1280×720](1280x720-coffee-display-ready.png) |
| Kávégép: főzés | [1280×720](1280x720-coffee-display-brewing.png) |
| Kávégép: elkészült latte | [1280×720](1280x720-coffee-display-latte.png) |
| Kávégép: tejhiány | [1920×1080](1920x1080-coffee-display-no-milk-large.png) |
| Kávégép: kávéhiány | [1920×1080](1920x1080-coffee-display-no-coffee-large.png) |

## Kiadás és review

Forráscommitok: `346f156` (#23), `bd6a73c` (#24), `0a3485e` + `865dde5` (#25). Az ágak egymásra épülnek a #26 UI-PR fölött; az egyes PR-ek csak a saját munkacsomagjuk különbségét mutatják. A közös WebGL-bizonyíték az utolsó ág integrált tartalmára vonatkozik.

Review: [#27 mozgás](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/27), [#28 terminál](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/28), [#29 kávégép](https://github.com/t-simon-fr/NemzetiDohanyboltSzimulator/pull/29). Az issue-k a mainre kerülésig nyitva maradnak.

Mainre merge és Pages-kiadás nem történt; a kérés a forrás és helyi ellenőrzés elkészítésére szólt.

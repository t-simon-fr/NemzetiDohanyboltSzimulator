# #132 – Ellenőrzési jelentés

[Használat és működés](README.md). A barna díszfestés nem ütköző. Az alsó falnál a kifelé néző vásárlói oldal váltotta ki az általános útvonalhibát; ezt az ismételt lerakás forgatás-visszaállítása is előidézte. Konkrét hibaüzenet, iránynyilak és megőrzött ismétlési irány készült. A valós belső falfelület és a rajzi/rácsos határ most közös, 2 mm-es fizikai elválasztással.

A 6500 Ft-os Sarokpolc a normál polc fa/fém anyagait és 2,5 m-es magasságát követi: 0,8×0,8 m, öt valódi levágott sarkú polclap, két hátfal, tíz készlethely, átlós nyitott oldal. A rajzi kontúr ötszög, a navigáció konzervatívan a teljes befoglalót védi. Üresen 3250 Ft-ért eladható; a fizetés, megszakítás és következő napi megőrzés a közös rendszerben működik.

## Natív ellenőrzés

[Teljes integráció](unity.json): **1148 sikeres állítás / 48 suite**, benne 32 új corner-wall állítás. Az alsó fal rossz/jó iránya és ismételt forgatása, négy üzletszint tényleges falhatára, dekor-kollider hiánya, valódi sarokgeometria/slotok, két falhoz illesztés, NPC termékmegközelítés, pontos terhelés, készletes eladási tiltás, jóváírás, napváltás és reset ellenőrzött. A katalógus ellenőrzése explicit kiválasztja a bútorfület, így nem függ az előző suite által nyitva hagyott laptól.

Ez a teljes kör a kizárólag rajzi kontúrt érintő finomítás előtti baseline. Az első natív kép leváló kontúrvonalakat mutatott a beágyazott GUI-forgatás miatt; a pontok külön elforgatása javította. Ezután a [friss computer-ui kör](unity-ui-final.json) **12 sikeres állítást** adott; a végső rajzot natívan és a célplatformon is átnéztük. A 12 eset átfed a teljes körrel, nem adandó hozzá új egyedi tesztként.

[Natív Game view](native-corner-preview.png), [capture és review](native-capture.json): 1918×879, GUI-val együtt. Ez előkészített állapot API-val, nem natív egérjátékpróba. A konkrét kattintásos interakciók a böngészőben teljesültek.

## WebGL és képi review

[Egy közös végső build](build.json): **0 hiba / 3 figyelmeztetés, 443,01 s**. [Sarok/fal próba](browser.json): **12 sikeres ellenőrzés**; [korábbi bútoros regresszió](browser-furniture.json): **16**; [készletes átrendezés](browser-arrangement.json): **15**. Mindhárom kör 0 konzolhibával zárult. Közös indítási esetük átfed, nem 43 különböző eset.

A fixture csak a kezdőállapotot és az egércélokhoz szükséges vetítést adja. Valódi egér, R-forgatás, görgetés, ismételt lerakás, mentés, eladás, megszakítás, készletes mozgatás és ajtós napváltás történt. A korábbi futtató elvárt falpozícióit az új fizikai határhoz igazítottuk, a pénz-/készlet-/útvonal-ellenőrzéseket megtartva.

Codex saját képi review: [kifelé néző polc konkrét hibája](1280x720-front-wall-wrong-facing.png), [befelé fordított falpolc és azonos irányú ismétlés 720p](1280x720-front-wall-repeat-720.png) és [1080p](1920x1080-front-wall-repeat-1080.png), [hat katalógustétel](1280x720-six-item-catalog.png), [sarokelőnézet 720p](1280x720-corner-preview-720.png) és [1080p](1920x1080-corner-preview-1080.png), [6500 Ft-os terv](1280x720-corner-plan.png). A falhatár és a kontúr egyezik, az átlós nyitott oldal befelé mutat, a hiba és fő műveletek olvashatók. 720p-ben az oldalsó katalógus görgethető. A képek valódi játékból származnak.

## Forrás és korlátok

[Build forrásazonosító](source.json): a HEAD az előző main, a változások az azonosított munkafában voltak. A bizonyítékok megtartják az összes checket/hibát/buildhash-t és az utolsó állapotot; a teljes ismétlődő minták a megnevezett helyi Builds könyvtárakban maradnak. Reviewer: Codex saját diff- és képi review, nem független agent. A build után csak dokumentáció és szemantikát nem módosító kódformázás változott. Az idegen helyi fájlok megmaradtak. Nincs új teljes futammentés vagy Pages-publikálás.

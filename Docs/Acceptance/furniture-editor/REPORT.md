# #130 – Ellenőrzési jelentés

## Megvalósított eredmény

[Használat és működés](README.md): a normál/keskeny polc a kezdőpolc magas, ötszintes modelljét használja; a kijelölt bútor valódi mesh-előnézetet és olvasható méretet kap. 10 cm-es bútorszél-illesztés, fal- és szomszédvonzás, segédvonalak; üres bútorok eladása mentéskori 50%-os jóváírással; több külön hűtő vásárlása. A készlet és a lottótartalék megmarad.

## Unity

[Teljes integráció](unity.json): **1112 sikeres állítás / 47 suite**. A teljes pénz-, készlet-, útvonal-, reset- és modellregresszió sikeres. A kezdeti képi kör konkrét falütközést mutatott: a statikus ráhagyás 5 mm-rel túlfedte az elhelyezési határt. A fal menti szélső termékhez tartozó NPC-megállási pont is túl közel került a falhoz. A fizikai ütközésráhagyást korrigáltuk; a valódi NPC és az előzetes validáció most ugyanazt, oldalirányban járható megállási pontot használja. A polc elé/mögé történő megközelítés iránya és a többi akadály ellenőrzése megmarad.

A forgatás utáni böngészős kör külön, félmilliomod méteres float-kilógást talált. A határellenőrzés 1 mm-es tűrést kapott. A végső [célzott kör](unity-final.json) **159 sikeres állítás**: furniture-editor 34, arrangement 27, expansion 98. Külön eset igazolja a falnál tényleges lerakást, a szélső termékhez vezető tényleges ügyfél-célpont járhatóságát, a submilliméteres kerekítés elfogadását és az 1 cm-es valódi kilógás elutasítását. A 159 eset átfed a teljes körrel; nem adandó hozzá új egyedi tesztként. A teljes kör a szűk tűrésjavítás előtti baseline, a módosult geometriai feltételekre a célzott kör friss.

A további új esetek: eladás tervezése/megszakítása/egyszeri jóváírása; termékes hűtő eladásának tiltása; két külön hűtő és saját készlethelyek; az egyik hűtő eladása a másik megőrzésével; kezdeti polctulajdon és megvett bútorok resetje; lottótartalék és elégtelen fedezet esetén nincs részleges könyvelés. A viszonteladás külön pénzmozgás, nem termékeladás.

## Vizuális és WebGL

A végső [WebGL-build](build.json) sikeres: **0 hiba / 3 figyelmeztetés, 292,75 s**. A két végső kör [16 bútoros ellenőrzést](browser.json) és [15 készletes átrendezési ellenőrzést](browser-arrangement.json) teljesített, 0 konzolhibával. A közös indítási ellenőrzések átfednek, ezért nem állítjuk, hogy 31 különböző eset. Valódi egér/forgatás/görgetés/mentés/eladás/megszakítás és ajtós napváltás történt; a fixture csak a kezdő pénzt, méretet, egy vizsgált terméket és célpontokat állítja be.

Codex saját képi review: [kezdőpolc magassága](1280x720-tall-shelf.png), [falhoz illesztés 720p](1280x720-wall-snap-720.png) és [1080p](1920x1080-wall-snap-1080.png), [két hűtő terve](1280x720-two-fridges-plan.png), [feltöltött hűtő eladásának tiltása](1280x720-stocked-sale-blocked.png), [eladási jóváírás terve](1280x720-resale-plan.png). A magasságjelzés, illesztési visszajelzés és fő műveletek olvashatók/elérhetők; a segédvonalak látszanak. Az előnézetben a még el nem helyezett új ismételt példányt Esc-kel el lehet vetni a már tervezett két hűtő elvesztése nélkül. A nyers képek mind a célplatformból származnak, nem generált mockupok.

A korábbi készletes kör félrácsos egércéljánál a futtató mindkét szomszédos, érvényes 10 cm-es kerekítést elfogadja, miközben a pontos termékazonosságot, a pénzt, a navigációt és a napváltás utáni pozíciót továbbra is ellenőrzi. A natív ScreenCapture ebben az Editor-környezetben nem adott képet; ezt nem jelöltük natív vizuális sikernek. A képi és tényleges inputellenőrzés a WebGL-célplatformon teljesült. A JSON-összefoglalók minden checket/hibát/buildhash-t megtartanak, az ismétlődő állapotmintákból csak az utolsót. A teljes nyers minták helyben, a megnevezett Builds könyvtárakban maradnak.

## Forrás és korlátok

[Forrásazonosító](source.json). A jelentések az aktuális munkapéldány lenyomatát tárolják; a HEAD még a korábbi main, a módosult kód a munkafában volt. A végső build után csak a böngészős futtató félrácsos elvárása és tisztább képi célzása, valamint a dokumentáció változott; a player C# azonos. Reviewer: Codex saját diff- és képi review, nem független agent. Új teljes mentési rendszer és Pages-publikálás nem része ennek a csomagnak. Az idegen helyi fájlok megmaradtak.

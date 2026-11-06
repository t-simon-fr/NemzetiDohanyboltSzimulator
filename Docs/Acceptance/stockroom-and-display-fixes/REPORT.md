# Raktárnyílás és nyitott öngyújtótartó — #94, #96

A bal oldali barna burkolat korábban egyetlen hosszú elemként átfutott a raktárbejáraton. Most a két tényleges oldalfal bounds-át követi, így a teljes nyílás szabad marad. A felhasználó pontosítása alapján az öngyújtós vitrin üvege és a hozzá létrehozott anyag teljesen eltűnt; az alsó tartó és mind a hat öngyújtó megmaradt.

Forrás: 903646b9a0059ed4a9db633cce721f735484f049; [azonosító és lenyomat](source.json). Külön commitok: 7e50d2c (#94), be4aaab (#96), 903646b (közös böngészős ellenőrzés). PR #95 és #97, függőségi sorrendben.

- [Natív ellenőrzés](unity.json): fordítás és 13 célzott identity-visuals állítás sikeres. A futás az utolsó commit előtt, annak már előkészített fájltartalmából történt.
- [Végső helyi WebGL-build](build.json): sikeres, 0 hiba, 3 figyelmeztetés.
- [Böngészős ellenőrzés](browser.json): 4 sikeres ellenőrzés, 0 konzolhiba. Tényleges egérrel játékindítás, W-vel áthaladás a raktárnyíláson, változatlan pénz/napállapot, E-vel monitorfókusz.
- Reviewer: Codex. A natív és WebGL-képeken ellenőrizve: nincs panel a nyílás előtt, nincs üvegtömb a monitor mellett, az öngyújtók/tartó megmaradtak. Mindkét WebGL-felbontás átnézve.

Képek: [natív nyílás](native-opening.png), [natív tartó](native-display.png), [720p nyílás](1280x720-warehouse-opening.png), [1080p nyílás](1920x1080-warehouse-opening-large.png), [720p tartó](1280x720-lighter-display-open.png), [1080p tartó](1920x1080-lighter-display-open-large.png).

Kollízió, készlet, fizetés és más üvegfelületek nem változtak. Az előző teljesebb játékmeneti bizonyíték a [trafikfrissítés jelentésében](../2026-11-05-trafik-refresh/REPORT.md) található; ezt nem futtattuk újra. A felhasználói helyi fájlok megmaradtak, jelenetgenerálás vagy -mentés nem történt. Pagesre még nincs kiadva.

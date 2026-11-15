# #156 - Ellenőrzési jelentés (WebGL)

A #156 a reputáció választék-metrikáját javítja: a két vonaljegy- és három jégkrémtípus csak akkor számít a nevezőbe, ha a játékos ténylegesen hordozza (ebben a játékban rendelte, a raktárban vár, vagy a polcon áll), és a már hordozott kategória kifogyás után is megmarad. A natív bizonyíték a teljes `checkout` profil 57 suite / 1626 állítás zöld futása; ez a mappa a player viselkedését igazolja böngészőben.

## Natív kiindulás

- Teljes profil: **57 suite / 1626 állítás, 0 hiba**, `Builds/Verification/20261115-185025-287/report.json`, `transportRetries: 0`.
- `smoke` profil (a #160 bővítés után): **16 suite / 516 állítás**, `Builds/Verification/20261115-185852-398/report.json`.
- `reputation-integration` 36 → **45** állítás; a kezdő bolt választéka újra `4/4`.

## WebGL player

[Build](build.json): `ShopAcceptanceBuild.Queue()` 2026-11-15 18:59:17 UTC-kor, elkészült **19:04:03 UTC**, **0 hiba / 3 figyelmeztetés**, 286,53 s, 27 194 796 bájt. A figyelmeztetések a Unity/Emscripten eszközkészletből származnak. Egy build készült; a futás a `build-result.json` friss UTC-időbélyegét ellenőrizte, és a runner a player-asset SHA256 értékeket is rögzítette.

[Player-asset lenyomatok](build.json) (SHA256): `data.unityweb` `2d31ab9d…`, `framework.js.unityweb` `af7fbd6e…`, `loader.js` `f76e28f8…`, `wasm.unityweb` `45a5c620…`.

## Böngészős próba

[Böngésző jelentés](browser.json): `node Tools/Verify-Acceptance.cjs --case=reputation`, 19:05:57–19:06:38 UTC, **17 sikeres ellenőrzés, 0 konzolhiba**, 11 kép. A case végig valódi egér-/billentyű-inputot használ: asztali Hírnév alkalmazás, három fizikai nyugta kártyás fizetéssel, majd a táblán/ajtón E-vel napzárás.

Két új állítás a #156 viselkedésére (runner-oldali, nem igényelt új buildet):

1. `Tickets and ice cream remain orderable from the starter shop` — a `supplyTypes` tartalmazza a 25-ös és 27-es típust, tehát a kategóriák beszerezhetők, csak nem árusítottak.
2. `Starter shop keeps full choice availability while never carrying tickets or ice cream` — `reputationMissing === 0` és `reputationScores[0] >= 50 + 50·min(1, seconds/120)`. A választék-pont ugyanis `100·elérhető/nevező` időátlaga, `confidence = min(1, nyitott mp / 120)`, ezért teljes elérhetőségnél a pont pontosan `50 + 50·confidence`. A feltétel időfüggetlen, és a régi viselkedést kizárja: ha a jegyek és jégkrémek feltétel nélkül a nevezőben lennének, a nyers arány `4/9 = 44,4%` lenne, amiből a pont legfeljebb `50 − 5,6·confidence ≈ 47` lehet.

Mért értékek (3 lezárt látogatás, 164,8 nyitott másodperc, 0 hiányzó tétel):

| Mutató | Érték |
|---|---|
| Választék / készlet | **100 / 100** |
| Tisztaság | 98,7 / 100 |
| Árak | 90 / 100 |
| Kiszolgálási idő | 99,4 / 100 |
| Napi eredmény | 50 → **54 (+4,0)** |
| Következő nap | 54, üres napi minták |

## Vizuális és interakciós próba

```text
Forráscommit / sourceFingerprint: 4d61512c5f569ae409fd2a69dc0f736540880d7a / 9d11b32d06fba793d00cfa43f3e67d52618656f6cd8ccdd95ae02e4d2172652f (dirty: a futás idején a runner-állítás szerkesztése és Unity által újraszerializált, nem kapcsolódó assetek)
Környezet és felbontások: headless Chromium, WebGL player, 1280x720 és 1920x1080
Reviewer és időpont: OpenCode agent (nem független reviewer), 2026-11-15 19:06 UTC
Végigpróbált lépések: menü indítás egérrel, Hírnév app megnyitása kattintással, 3 valódi nyugta + kártyás jóváírás, Ma/Lezárt nap tabok, napzárás táblán/ajtón E-vel, következő nap Enterrel, törzsvendég-fixture
Átnézett képek: 1280x720- és 1920x1080-reputation-{empty,today,summary,last,regular}.png ebben a mappában
Megfigyelés, talált hibák és javításuk: a mai nézet Választék sora 100/100, a napi összesítő 54,0 (+4,0) és Választék 100; a feliratok és a négy mutató mindkét felbontásban elférnek, a tabok egyértelműek. Új hiba nem volt, javítás nem kellett.
Eredmény: passed
```

Képek: [adat nélkül 720p](1280x720-reputation-empty-720.png) / [1080p](1920x1080-reputation-empty-1080.png), [mai megfigyelések 720p](1280x720-reputation-today-720.png) / [1080p](1920x1080-reputation-today-1080.png), [napi összesítő 720p](1280x720-reputation-summary-720.png) / [1080p](1920x1080-reputation-summary-1080.png), [lezárt nap 720p](1280x720-reputation-last-720.png) / [1080p](1920x1080-reputation-last-1080.png), [törzsvendég 720p](1280x720-reputation-regular-720.png) / [1080p](1920x1080-reputation-regular-1080.png).

## Határok

- A fixture a belépést, a várakozást és a nyugta előkészítését a rendes játékmeneti metódusokkal végzi (egyszerre egy termék, meglévő készletből); a kártyajóváírás és a napzárás valódi input.
- A forráslenyomat a futás teljes munkafáját fedi, ezért `dirty`: a #156 commit után a böngészős állítás szerkesztése és a Unity által újraszerializált, a feladathoz nem tartozó assetek (anyag `_EMISSION` kulcs, URP global settings, quality/connect beállítások) szerepelnek benne. A player a 18:59-kor indult buildből készült; a `Tools/Verify-Acceptance.cjs` későbbi szerkesztése nem kerül a playerbe, a runner a stale-ellenőrzéssel igazolja, hogy az `Assets`/`ProjectSettings`/`Packages` tartalom nem változott a build után.
- Pages-publikálás nem történt, külön engedélyt igényel.

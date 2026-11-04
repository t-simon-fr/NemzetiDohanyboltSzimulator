# Elfogadási ellenőrzések

Aktuális fejlesztés: [#22 egységes trafik UI](2026-11-04-ui/REPORT.md). Függőségek: [#20–#21 ajtótábla és pult alatti készlet](2026-11-04-door-storage/REPORT.md), [#15 alaprajz és átrendezés](2026-11-04-layout/REPORT.md). Előző csomagok: [#13 ajtó és műszak](2026-11-04/REPORT.md), [#2–#8](2026-11-03/REPORT.md).

```powershell
pwsh -File Tools/Verify-Project.ps1 -Profile checkout
```

Helyi böngészős player: a `ShopAcceptanceBuild.Queue` Editor-belépési pontot a Unity pipeline-on keresztül kell indítani. Példa:

```powershell
& Tools/Invoke-Pipeline.ps1 -Command 'script eval' -Parameters '{"code":"var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(\"ShopAcceptanceBuild\")).First(x=>x!=null);t.GetMethod(\"Queue\").Invoke(null,null);return \"queued\";"}'
```

Várd meg az új `Builds/AcceptanceWebGL/build-result.json` fájlt (a módosítási ideje a most indított buildhez tartozzon), benne `success: true`. Ezután:

```powershell
node Tools/Verify-Acceptance.cjs
# Célzott újrafuttatás új hiba vagy módosított feltétel esetén:
node Tools/Verify-Acceptance.cjs --case=menu
node Tools/Verify-Acceptance.cjs --case=entrance
node Tools/Verify-Acceptance.cjs --case=dialogue
node Tools/Verify-Acceptance.cjs --case=arrangement
node Tools/Verify-Acceptance.cjs --case=door-storage
node Tools/Verify-Acceptance.cjs --case=ui
```

Feltétel: Node WebSocket-támogatással és helyi Chromium a meglévő ms-playwright cache-ben, vagy WEBGL_BROWSER környezeti változóval megadva. A script csak helyi tesztbuildet szolgál ki; nem deployol. A normál publikus build nem kap SHOP_ACCEPTANCE jelzőt.

A #13 folyamatában a `summary` fixture a zárt bolthoz visz: az elszámolás csak E-re jelenik meg. Az `entrance` eset valódi billentyűbemenettel próbálja végig a táblát, a vendég által mozgatott ajtót, a bent maradást és a következő napot. A `dialogue` eset T-vel választ helyettesítő árut; nincs külön párbeszédmodal.

A `door-storage` eset öt valódi Dnyipro csomagot készít elő, E-vel felvesz/visszatesz egyet, majd utcai tesztkamerából ellenőrzi a táblafordítást. A felvett áru forráshelye a meglévő biztonságos megszakítás miatt lerakásig foglalt marad. Az utcai kamera nem ad kijárási lehetőséget a normál játékosnak.

A `ui` eset vegyes/azonos rendelést és részleges fizikai átadást készít elő, követi a darabszámot és a lejáró értesítést, hosszú magyar mondatot mutat mindkét felbontásban, majd valódi egérkattintással próbálja a hibás/javított kártyás fizetést, az elszámolás Következő nap gombját és a nagy nézet menüjét. A fixture nem helyettesíti a valós billentyűkiosztás-, szünet- és pénzügyi regressziós eseteket.

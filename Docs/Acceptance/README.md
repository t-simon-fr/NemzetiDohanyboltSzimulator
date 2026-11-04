# Elfogadási ellenőrzések

Aktuális fejlesztés: [#15 alaprajz és átrendezés](2026-11-04-layout/REPORT.md). Előző csomagok: [#13 ajtó és műszak](2026-11-04/REPORT.md), [#2–#8](2026-11-03/REPORT.md).

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
```

Feltétel: Node WebSocket-támogatással és helyi Chromium a meglévő ms-playwright cache-ben, vagy WEBGL_BROWSER környezeti változóval megadva. A script csak helyi tesztbuildet szolgál ki; nem deployol. A normál publikus build nem kap SHOP_ACCEPTANCE jelzőt.

A #13 folyamatában a `summary` fixture a zárt bolthoz visz: az elszámolás csak E-re jelenik meg. Az `entrance` eset valódi billentyűbemenettel próbálja végig a táblát, a vendég által mozgatott ajtót, a bent maradást és a következő napot. A `dialogue` eset T-vel választ helyettesítő árut; nincs külön párbeszédmodal.

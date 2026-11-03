# Elfogadási ellenőrzések

Aktuális bizonyíték: [2026-11-03 — #2–#8](2026-11-03/REPORT.md).

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
```

Feltétel: Node WebSocket-támogatással és helyi Chromium a meglévő ms-playwright cache-ben, vagy WEBGL_BROWSER környezeti változóval megadva. A script csak helyi tesztbuildet szolgál ki; nem deployol. A normál publikus build nem kap SHOP_ACCEPTANCE jelzőt.

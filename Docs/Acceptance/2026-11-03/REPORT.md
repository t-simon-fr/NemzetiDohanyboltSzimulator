# #2–#8 elfogadási ellenőrzése — 2026-11-03

Reviewer: Codex. A kijelölt képek megnyitva és vizuálisan átnézve. A forrás a `verify/2-8-acceptance` ág változásaival együtt szerepel a jelentésekben; az induló HEAD ad69d8d, a pontos Unity-tartalmat a sourceFingerprint azonosítja. A WebGL-forrásfájlok és lefordított artifactok hash-e: [webgl-build.json](webgl-build.json).

## Eredmény

- Friss Unity-fordítás, 7 csoportban **130 sikeres állítás**, 0 sikertelen: [unity-report.json](unity-report.json).
- A helyi WebGL-build elkészült: 0 hiba, 0 figyelmeztetés, 21 018 412 byte, 249,84 másodperc.
- Chromiumban **34 különböző sikeres elfogadási ellenőrzés**, 0 böngészős/Unity-console error a négy végleges futásban. Egy teljes kör és három, a hiányzó feltételekre célzott kiegészítés: a kapcsolódó JSON-fájlok ugyanebben a könyvtárban.
- 1280×720: kassza, pult, egér/billentyű, eszközök, kávé, párbeszéd és beállítások. 1920×1080: beállítások, elszámolás és napváltás.

| Issue | Végigpróbált viselkedés és kép |
|---|---|
| #2 Kártya | Valódi Enter és számgombok: üres és hibás összeg elutasítva; Delete, Backspace javít; pontos összeg egyszer kerül elszámolásra; ismételt Enter nem dupláz. [Hibás összeg](card-wrong.png). |
| #3 Közös pénz | Kártya és készpénz, beszerzés, fejlesztés, napváltás és fizikai kasszaösszeg egyeztetése. Teljes nyolcvásárlós műszak, összesített kosár elszámolása és megszakítás. |
| #4 Pult | Három azonos cigaretta: 3→2→1→0 hiányzó darab, külön helyek, egyszeri összesített fizetés; eltérő és önkiszolgáló vegyes kosarak kivétele/visszaadása. [Három azonos termék](triple.png). |
| #5 Bemenet | Fizikai tárgy felvétele és lerakása valódi bal klikkel és E-vel; ütő bal klikkes lendítése; felmosó nyomva tartott klikkel eltünteti a foltot; szünet alatt nincs világművelet. [Lerakott tárgy](mouse-placed.png). |
| #6 Kávé | 1/2/3 a gépnél, mindhárom elkészült pohár helyes termékazonosítóval; latte tejhiánya nem fogyaszt kávét. A monitor mindhárom receptgombja valódi kattintással működik. Fizetett alapanyag-szállítás és kézi betöltés Unityben. [Monitor](coffee-monitor.png). |
| #7 Menü | Egérrel indítás, Escape szünet, tényleges óra és mozgás megállítása, GUI-n E→R átállítás; oldal-újratöltés után R-rel valódi felvétel; alapértékek visszaállítása; ütköző E/W billentyűk felcserélése regressziós próbában. [Mentett kiosztás](settings-persisted.png), [nagy felbontás](settings-large.png). |
| #8 Párbeszéd | Egy- és többtermékes kérés, tényleges hiány, kattintható helyettesítő, elfogadáskor új kérés/ár és elutasításkor távozás pénzmozgás nélkül. [Többtermékes párbeszéd](dialogue.png). |

[Elszámolás](summary.png): a gombok külön területen látszanak, Enter ténylegesen a második napra visz, a kassza megmarad.

## Javított hibák

1. A készlethiány-jelzés csak a kávé/tej alapanyagot figyelte, ezért egy már elkészült utolsó latte mellett is hiányt jelzett. Most a kész pohár is beleszámít az elérhető készletbe, a hiányzó mennyiséghez pedig csak a még elkészítendő adagok alapanyaga szükséges. A helyettesítő keresése ugyanazt a feltételt használja; nincs véletlen 0-s termékre visszaesés.
2. A vevő fizetési/elköszönő mondata kétszer jelent meg a HUD-on. Most egyszer jelenik meg; a hiányzó termékek sora csak rendeléskor látható.

## Hatókör és korlátok

A WebGL-próba külön helyi, `SHOP_ACCEPTANCE` fordítási jelzővel készült playeren futott. A probe csak állapotot készít elő és megfigyel; a vizsgált kattintások és billentyűleütések a tényleges böngészőből érkeznek. A készletfelállítás és a főzési idő gyorsítása determinisztikus fixture-művelet. A többvásárlós készlet-/pénzfolyamatot külön Unity-integrációs próbák is lefedik.

A headless böngészőben a fixture-előkészítés ugyanazon képkockán több pointer-lock kérést okozott; a kizárólag helyben kiszolgált HTML összefogja az átfedő kéréseket. Nem szimulál sikeres lockot és nem nyeli el az elutasítást; az eredeti natív requestPointerLock fut. A kezdeti sikertelen futásokat nem számoltuk a sikeres eredménybe.

A kassza és kéz művészi átdolgozása továbbra is félretett munka. Az áttetsző menük kontrasztja és a kávégép két súgófelületének háttere későbbi vizuális finomítás; a vizsgált gombok/szövegek elérhetők voltak. Nem állítunk teljes akadálymentességi vagy többböngészős megfelelést.

Pagesre nem került ki új build. A normál shipping buildben a tesztprobe nincs benne; éles deploy/élő URL ellenőrzés nem történt.

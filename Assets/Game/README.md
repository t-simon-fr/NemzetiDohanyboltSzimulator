# Játékprototípus

A teljes használati útmutató a Unity-projekt gyökerében található [MVP_GUIDE.md](../../MVP_GUIDE.md) fájlban van.

A `Scenes/ShopPrototype.unity` már tartalmazza a teljes MVP-t. Play módban nyitópanel fogad; a bolt a panelről vagy O-val nyitható. A kis kezdőkészlet, az automatikus vásárlók, a sor, a fizetés, az utánrendelés, a kézi polcfeltöltés és a napvégi elszámolás összekapcsolódik.

## Kód

- `Scripts/Mvp/ShopLedger.cs`: termékadatok, árak, pénzügyi számítások, szereplők szövegei.
- `Scripts/Mvp/MvpShopController.cs`: napciklus, sor, rendelés, fizetés, utánpótlás és készlet.
- `Scripts/Mvp/MvpShopHud.cs`: játékbeli állapotok és panelek.
- `Scripts/Mvp/MvpCustomerActor.cs`: a próbafigurák útvonalai.
- `Scripts/Mvp/ShopStation.cs`: kassza, szállítódoboz és nyitástábla.
- `Scripts/Interaction`: termékek, fix helyek és a játékos interakciói.
- `Scripts/Player`: első személyű vezérlő.
- `Editor/MvpSetup.cs`: az MVP bekötése a graybox jelenetbe.

A korábbi egyvásárlós `CustomerOrderController` az inaktív figuramintán megmarad. Az MVP-ben az új műszakvezérlő kezeli a vásárlókat. A graybox újragenerálása és az **Enable Complete MVP** menüpont egyaránt beköti a teljes változatot.

A játék iránydokumentuma egy szinttel a Unity-projekt fölött, a `GAME_VISION.md` fájlban található.

# Megvásárolható vészgomb

A TobaccOS **Üzletfejlesztés** alkalmazásában nyitás előtt, egyszer vásárolható meg, **7500 Ft-ért**. Nem igényli az önkiszolgáló bővítést. Az árat a `ShopSecurity.PurchasePrice` határozza meg; a vétel beruházásként, a kasszában és a könyvelésben azonos összeggel jelenik meg.

A vásárlás felszerel egy piros gombot a pult személyzet felőli oldalára és egy biztonsági redőnyt a bejáratra. **E a gombon: reteszelés / feloldás.** Kézben tartott áruval vagy kartonnal is használható. A zöld jelzőfény nyitott, a piros reteszelt bejáratot jelent. A reteszelés csak nyitvatartás alatt engedélyezett.

Reteszeléskor a bejáratot fizikai ütköző zárja; a vásárlók befelé és kifelé is megállnak előtte. A már bent lévők tovább vásárolhatnak és fizethetnek. A játék nem indít új látogatókat a zárt bejárathoz. A kint várakozó vásárló türelmi ideje belépés előtt nem fogy. Feloldás után folytatódik az útjuk.

Kézi és automatikus záráskor a retesz feloldódik, így mindenki távozhat és a műszak lezárható. A megvásárolt fejlesztés a következő napra megmarad; új játék indításakor eltűnik. A futó játékon túli tartós mentés a többi fejlesztéshez hasonlóan még külön feladat.

A berendezés futáskor, a meglévő pult és bejárat alapján épül fel, így a jelenetet nem kell újragenerálni. A gomb követi a pult mozgatását. A vásárlók útvonala ugyanazt a reteszállapotot használja, mint a látható redőny. A lopási viselkedés és az ütő külön fejlesztési pont; az aktuális vásárlók mozgása már tiszteletben tartja a reteszt.

Ellenőrzés Play módban: `Tools/Checks/PanicButtonChecks.cs`, belépési pont: `PanicButtonChecks.Run`. A `Capture` belépési pont `shop`, `locked`, illetve `button` nézettel képeket ír a `Builds/PanicButtonPreview` könyvtárba.

## Ellenőrzött állapot – 2026. november 3.

A teljes feature-csomagban 21 ellenőrzéscsoportban **1105 sikeres állítás**, ebből **45** a vészgomb célzott próbája. A fennmaradó próbák a kasszát, a teljes kezdő- és bővített műszakot, a berendezések kezelését, a feltöltést, a raktárat és a TobaccOS-t ellenőrizték. A Unity nem jelzett fordítási hibát, konzolhibát vagy figyelmeztetést. Aktuális összesített eredmények: `Builds/FeatureRelease/checks.json`; a korábbi külön vészgombpróba eredménye: `Builds/PanicButtonPreview/checks.json`. A három előnézeti képen a fejlesztési oldal, a pult mögötti gomb és a lezárt redőny vizuálisan is ellenőrizve.

Az ellenőrzés során a korábbi feltöltési teszt rögzített 3-as termékindexét a víz név szerinti azonosítására cseréltük, hogy a jelenlegi négymárkás cigarettakínálat mellett is a megfelelő árut rendelje.

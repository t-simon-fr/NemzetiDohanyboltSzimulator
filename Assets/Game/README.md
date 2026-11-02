# Játékprototípus

A használati útmutató: [MVP_GUIDE.md](../../MVP_GUIDE.md). Játékirány: [Docs/GAME_VISION.md](../../Docs/GAME_VISION.md). Következő alkalom: [Docs/CONTINUE.md](../../Docs/CONTINUE.md).

A `Scenes/ShopPrototype.unity` a kompakt, cigarettával induló bolt. A bal oldali monitor beszerzésre és bővítésre használható, a kasszából darabonként vehető ki magyar címletértékű pénz. A bővített rész polcairól a vásárlók maguk gyűjtik be az árut.

## Forráskód

- `Editor/CounterFixtureSetup.cs`: meglévő, kézzel szerkesztett pult felszerelésének igazítása; a bolt többi része megmarad.
- `Scripts/Mvp/CounterLayout.cs`: mozgatható pultcsoport és vásárlói megállási/sorhelyek.
- `Scripts/Mvp/CashRegisterDrawer.cs`: pénzfiók nyitás-zárás és kattintás tiltása mozgás közben.
- `Editor/CompactShopSetup.cs`: menthető teljes jelenet, kompakt bolt és bővítés, állandó pultpozíció.
- `Scripts/Mvp/MvpShopController.cs`: műszak, vásárlói sor, készlet, fizetés és beszerzés.
- `Scripts/Mvp/ShopDevices.cs`: játékbeli monitor, kasszafiók, kamera és pénztálca.
- `Scripts/Mvp/ShopWorldControl.cs`: kattintható fizikai gombok és pénzrekeszek.
- `Scripts/Mvp/ShopCashDrawer.cs`: címletek darabszámai, kiválasztás, pénzváltás, elkülönített vásárlói pénz.
- `Scripts/Mvp/ShopLayout.cs`: térbővítés, önkiszolgáló polcok és célpontok.
- `Scripts/Mvp/MvpCustomerActor.cs`: útvonal, polcról felvett termék és visszarakás megszakításkor.
- `Scripts/Mvp/ShopLedger.cs`: termékkatalógus, gazdaság, karakterek szövegei.
- `Scripts/Mvp/MvpShopHud.cs`: rövid állapotkijelzés, súgó és napi összesítés.
- `Scripts/Mvp/ProductVisuals.cs`, `ShopWorldText.cs`, `Shaders/WorldText.shader`: próbaáruk és takaráshelyes feliratok.
- `Scripts/Interaction`: termékfelvétel és terméktípushoz kötött fix helyek.
- `Scripts/Player`: belső nézetű vezérlés.

A **Build Compact Shop** menüpont újraépíti a jelenetet. A régi **Build Graybox Shop Scene** ugyanoda vezet. A korábbi egyvásárlós segédkód megmaradt, a kompakt jelenet nem használja; a régi bekapcsolómenük nem tesznek mellé második vásárlókezelőt.

A tartós játékmentés és a végleges grafika még hiányzik. A Windows-build a `Builds/MVP` mappában készül, Git nem tárolja. A próbák a projekt `Tools/Checks` mappájában vannak.

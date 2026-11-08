# Tea és forró csoki – használat és modulok

A megvásárolt kávégép öt receptet kínál. A TobaccOS **Kávégép** alkalmazásában rendelhető a La Vazze kávé, Albro tej, Levélke teaalapanyag és Kakaóka kakaópor. Tíz adag csomagára rendre 2000 / 1500 / 1200 / 1000 Ft; szállítás nyolc másodperc, alapanyagonként összesen 60 adag tárolható a gépben, kézben/raktárban és úton.

A fizikai csomagot E-vel vagy bal kattintással lehet felvenni, majd ugyanígy a gépbe tölteni. A tea egy adag teaalapanyagot, a forró csoki egy adag kakaóport és egy adag tejet használ; kávét egyik sem fogyaszt. Alapáruk 450 és 900 Ft, az Árak → Meleg italok kategóriában módosíthatók. A tea 3,5, a csoki 4 másodperc alatt készül.

A gép kijelzőjén kattintással választható recept; az indítás és átvétel a meglévő gombon történik. A kész poharat a vásárlói átadóhelyre kell tenni. A rendszer a konkrét termékazonosságot ellenőrzi, ezért másik ital nem teljesíti a rendelést. Nincs új gyorsbillentyű vagy rendelési modal.

## Kódfelépítés

- `ShopHotDrinkRecipes`: receptadatok, mennyiségek/idő, alapanyag-nevek, beszerzési árak; ugyanaz a definíció vezérli a főzést és a vevői alapanyagkeretet.
- `ShopCoffeeMachineState`: négy külön alapanyagkészlet, időzített szállítás, foglaltság, egyszeri főzés és foglalt kimenet esetén recepthelyes visszatérítés. A régi kávé/tej API kompatibilis marad.
- `ShopCoffeeRecipes` / `ShopActivities`: fizikai csomagok, eredeti egyszerű teás/kakaós kartonmodell, kézi feltöltés, meglévő pohárkimenet.
- `ShopCoffeeDisplay` / `ShopComputerActivities`: öt fizikai receptgomb és négy beszerzési sor; pontos hiányjelzés.
- `ShopCustomerOrders`, controller és reputáció: csak valós készletből rendelhető italok, közös tejkeret, nincs előzetes sorbanállói foglalás. Függő szállítás nem elérhető áru; megérkezett feltöltőcsomag rendeléshez számít, főzéshez előbb kézzel be kell tölteni.

A tea/csoki SKU-k 23/24; a korábbi 0–22 értékek változatlanok, a kész italok nem rendelhető polcos áruk. A receptek 120/250 Ft önköltsége egyezik az elfogyasztott tea, illetve kakaó + tej árával. Napváltáskor a meglévő készletkezelés érvényes, új játék teljesen törli a gépet és a csomagokat.

Elfogadási feltételek és ellenőrzési terv: [PLAN](PLAN.md). Végleges bizonyíték: [elfogadási jelentés](../../Acceptance/hot-drinks/REPORT.md).

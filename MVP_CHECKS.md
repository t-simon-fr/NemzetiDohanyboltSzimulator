# MVP ellenőrzési jegyzőkönyv

Dátum: 2026. november 1. • Unity 6000.6.7f1 • Jelenet: `Assets/Game/Scenes/ShopPrototype.unity`

## Lefutott vizsgálatok

| Vizsgálat | Eredmény |
|---|---|
| Unity scriptfordítás | Sikeres, fordítási hiba nélkül |
| `LedgerChecks.Run` | 12 sikeres ellenőrzés |
| `ProductInteractionChecks.Run` | 24 sikeres ellenőrzés |
| `FullMvpChecks.RunDay` | 92 sikeres ellenőrzés a tényleges boltjelenetben |
| `FullMvpChecks.CheckNextDayAndReset` | 6 sikeres ellenőrzés |

Összesen 134 sikeres automatikus ellenőrzés. A tesztek a Unity Pipeline helyi felületén futottak, és a játék metódusait, fizikáját és jelenetobjektumait használták. A tényleges billentyű- és egérkattintásokat nem szimulálták.

## A teljes nap eredménye

Nyolc helyesen kiszolgált vásárló, négy személyiség, háromfős sor, téves áru visszavétele, manuálisan kijavított visszajáró, víz utánrendelése és polcra rakása.

- Árbevétel: **3100 Ft**.
- Eladott áruk beszerzési értéke: **1360 Ft**.
- Napi működési költség: **300 Ft**.
- Nyereség: **1440 Ft**.
- Utánrendelésre kifizetett összeg: **450 Ft**.
- Zárókassza: **8350 Ft**.
- Megmaradt készlet a boltban és a dobozban összesen: **1 darab**.

A napváltás átviszi a kasszát és a készletet. Az új játék visszaállítja a 6000 Ft-os kezdőállapotot és a termékenkénti két darabot. A korai zárás egyszer vonja le a napi költséget, a már megvett, úton lévő áru pedig megmarad.

## Vizuális ellenőrzés

A nyitópanelt, a játékbeli sort és készletszámlálót, a címletes fizetést, a beszerzést és a napvégi összesítőt a Game nézetből rögzített képen is ellenőriztem. A képek a projekt figyelmen kívül hagyott `Temp/MvpChecks` mappájában vannak.

## Játékosi próba

A következő lépés egy teljes műszak kézi végigjátszása a végleges tempó és kezelés finomításához. A látvány szürkedobozos, a figurák előre meghatározott útvonalakon haladnak. A játékmenet bezárás utáni tartós mentése későbbi feladat.

## Önálló Windows-változat

A `Builds/MVP/NemzetiDohanyboltSzimulator.exe` elkészült. A Unity BuildReport eredménye **Succeeded**, nulla hiba és nulla figyelmeztetés. A csomag mérete körülbelül 105 MB (100 MiB).

A külön indítható alkalmazás nyolc másodperces, grafika nélküli indulási próbán is futva maradt, és a naplóban nem jelent meg futásidejű kivétel. A grafikus felületek vizuális ellenőrzése az Editor Game nézetében történt.

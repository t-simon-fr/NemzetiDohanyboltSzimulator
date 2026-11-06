# Refaktor batch #69–#74 — végrehajtási terv

Engedély: a felhasználó kérte mind a hat nyitott refaktor implementálását. Baseline: a17d405. Runtime refaktor, azonos játékosi viselkedéssel; jelenet/prefab GUID, input enum, mentéskulcs és Pages nem változik.

| Issue | Határ / érintett adapterek | Invariáns és célzott ellenőrzés |
|---|---|---|
| #69 | Önálló beszerzési ajánlat; OrderSupply/OrderBasket delegálás | Teljes kosár validálása, nulla részleges könyvelés; ledger, feature-batch, warehouse-storage és hibás kosarak |
| #70 | Közös készletnézet, raktári mennyiségek és időzített szállítás külön tulajdonossal | Polc/kéz/karton/privát doboz/vevő/pending szerepek; stock-aware-orders, warehouse-storage, ingredient-pickup, feature-batch |
| #71 | Checkout munkamenet és explicit receipt-slot nyilvántartás | Több azonos tétel, visszavétel, egyszeri fizetés; checkout-revision, basket-day, recovery, physical-terminal, customer-hand |
| #72 | Unity-független kávéfőző állapot | Recept, betöltött/úton/kész alapanyag, időzítés, sikertelen átvétel visszaadása; coffee-world, coffee-display, ingredient-pickup |
| #73 | Napfázis/óra modell és fókusz-élettartam tulajdonos | Előkészület/nyitás/zárás/este/ajtós lezárás; kamera/kurzor/pause/input fogyasztás; entrance-day, physical-terminal, coffee-display, binding-acceptance, after-hours-upgrades |
| #74 | Bootstrap, központi scene-binding és runtime erőforrás-élettartam | Párosított enable/disable, új játék, mesh/material/audio/RT felszabadítás; lifecycle célzott ellenőrzés és közös integráció |

Sorrend: #69 → #70 → #71 → #72 → #73 → #74, külön commitok és függő PR-ek. Minden rész után érintett natív ellenőrzés, a stabil batch végén egy közös teljes integráció és helyi WebGL-próba. A történeti eredmények baseline-ok, nem friss refaktor-bizonyítékok. Készültség: implementált határok, friss sikeres invariáns- és integrációs próbák, valódi WebGL-interakció, függőségi merge és issue-státusz rendezve. Pages külön kérésre.

## Implementált modulhatárok

| Felelősség | Tulajdonos | Megmaradó adapter |
|---|---|---|
| Mellékhatásmentes beszerzési ajánlat | ShopPurchaseRules / ShopPurchaseQuote | OrderSupply és OrderBasket felhasználói üzenetei |
| Raktári mennyiség, kapacitás, szállítás | ShopInventory | Controller API, WarehouseArea/Carton fizikai megjelenítés |
| Átadott tételek és explicit nyugtahelyek | ShopCheckoutSession | ProductSlot és a készpénz/kártya/érme fizikai UI |
| Kávé, tej, recept, főzés és ingredient-szállítás | ShopCoffeeMachineState | ShopActivities, ShopCoffeeDisplay, fizikai csészék/csomagok |
| Napfázis és órák | ShopDayCycle | Ajtó/tábla, vevők, esti kereskedő és elszámolás |
| Kamera és szünet visszaállítása | ShopCameraSnapshot / ShopDeviceFocus / ShopPauseScope | Eszköz, menü és átrendező |
| Létrehozási sorrend | ShopBootstrap | Meglévő MonoBehaviour komponensek és szerializált mezők |
| Jelenethivatkozások | ShopSceneBindings | ShopFloorPlan.Find kompatibilis API |
| Létrehozott natív erőforrások | ShopRuntimeResources | Saját építő és annak OnDestroy-ja |

### Készlettulajdon és invariánsok

- A fizikai ShopProduct továbbra is egyetlen példány; a ProductSlot helyfoglalást tart fenn kézbevétel/megszakítás alatt. A controller a létrehozott termékek fizikai regiszterét tartja fenn, a ShopInventory kizárólag raktári és úton lévő mennyiséget ír.
- A WarehouseCarton ugyanennek a raktári mennyiségnek a fizikai adaptere; mozgatása önmagában nem von le készletet. Polcra pakoláskor történik raktári kivét és fizikai terméklétrehozás; sikertelen kézbevételnél a kivét visszaáll.
- A pult alatti privát doboz a ShopAfterHours/ShopCounterStorage tulajdona. Kicsomagolásakor normál fizikai termékek keletkeznek; nem lesz belőlük második raktári számláló.
- Az eladásig a vevő kosarában lévő fizikai termék a bolt készlete. A menekülő tolvajnál lévő árut a rendelési ajánlat kizárja; a várakozó vevő rendelése nem foglal előre árut.
- Az úton lévő termék kapacitást foglal, de rendelhető vevői kínálatot még nem jelent. A kávé közös kávé/tej keretét a kávégépmodell és a valóban megérkezett ingredient-csomagok adják.

### Élettartam és erőforrások

- A bootstrap egyszer, Startban fut. Enable/disable a singleton és az öt átadóhely feliratkozását párosítja; új játék az állapotot üríti, nem komponenseket épít újra.
- A menü/eszköz/átrendező saját fókuszát és szünetét visszaadja. Az UI-átmenet képkockájában a világinterakció elfogyasztott.
- A ShopActivities anyagai, ütő-mesh-e és CCTV RenderTexture-jei, a kasszavizuál anyagai, a TobaccOS eszközanyagai és a trafik runtime mesh/material erőforrásai explicit ShopRuntimeResources tulajdonban vannak. RT felszabadítás előtt a CCTV kamerák lecsatlakoznak.
- A shared world-font runtime anyag és statikus font-esemény a shop megsemmisítésekor elengedődik. Az eszköz font-eseménye enable/disable párban kapcsolódik.
- A scanner saját hangklipjét és anyagait már elengedte; a pénzvizuál saját mesh/material tulajdona, az egyéb berendezések meglévő OnDestroy-takarítása megmaradt. Az importált kasszamodell, font, textúra és keycap mesh kölcsönzött asset, nem törölhető runtime tulajdon.
- Assembly/GUID/namespace költöztetés, scene-regenerálás, input enum és PlayerPrefs migráció nem történt. Stripping/link.xml módosítás nem szükséges: az új modellekre közvetlen runtime hivatkozások mutatnak.

## Nap- és fókuszátmenetek

| Kiindulás | Esemény / feltétel | Eredmény |
|---|---|---|
| Felkészülés | A játékos megfordítja az ajtótáblát | Nyitva; az óra 08:00-ról indul. |
| Nyitva | Tábla ZÁRVA állásba kerül | Zárás; új vevő nem érkezik, a bent lévők kiszolgálhatók. |
| Zárás | Minden vevő/tolvaj távozott, nincs kézben áru | Esti maradás; az óra tovább halad, a bolt fejleszthető. |
| Zárás vagy esti maradás | Üres bolt, üres kéz, explicit ajtóhasználat a közelben | Összesítés; a függő szállítás megérkezik, a napi könyv lezárul. |
| Összesítés | Következő nap, nem negatív kassza | Nap + 1, felkészülés és 08:00; a készlet és megvett fejlesztések megmaradnak. |
| Összesítés | Negatív kassza | Az összesítés marad; új játék szükséges. |
| Bármely állapot | Új játék | Első nap, kezdőkészlet/egyenleg, modulállapotok és fejlesztések alaphelyzetben. |

A napmodell órája és a vevők kiszolgálási ideje külön léptethető, a meglévő működés szerint. A kézi nyitás/zárás nem kapott új automatikus határidőt.

| Fókusz | Tulajdonos és visszaállítás |
|---|---|
| Normál világ | Szabad mozgás, zárolt/eltakart kurzor. |
| Eszköz | ShopDeviceFocus birtokolja a kamera-pillanatképet és a világinput tiltását. Kilépés helyreállítja a kamerát, és elfogyasztja az átmeneti képkocka világakcióját. Eszközváltás/átrendezőre váltás közben a kurzor nem kap átmeneti pointer-lock kérést. |
| Menü | ShopPauseScope rögzíti az előző időskálát, tiltást és kurzort, Time.timeScale=0; visszatérés az előző eszköz- vagy világfókuszt állítja helyre. |
| Átrendező | Saját szünet-scope és kamera-pillanatkép; mentés/megszakítás visszaállítja a nézetet és az időt, a lezárt UI nem indít ugyanabban a képkockában világakciót. |
| Komponens letiltása | Menü, átrendező és eszközfókusz elengedése; a singleton és az átadó-események lekapcsolása. Újraengedélyezés visszaköti őket, bootstrap-duplikáció nélkül. |

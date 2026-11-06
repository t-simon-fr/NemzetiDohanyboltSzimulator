# Lottópont

Feladat: #106. Játékbeli üzemeltető: **Szerecsenjáték Zrt.** A játékos eladó, az NPC a szelvény vásárlója; a játékos nem fogad és nem kapar saját magának. Nincs valós szolgáltatói kapcsolat vagy külső értékű nyeremény.

## Használat

1. TobaccOS → Üzletfejlesztés → Lottópont, 12 000 Ft. Nyitás előtt és zárás után, üres boltban vásárolható. A külön állvány a pult raktár felőli végére kerül; a kassza, leolvasó és átadózóna szabad marad.
2. A terminálon vagy TobaccOS → Lottópont alatt +5-ös tételekben rendelhető bizományos készlet, 8 mp szállítással. A névértéket csak az eladott szelvény után kell elszámolni. Típusonként legfeljebb 30 eladatlan/függő szelvény fér el.
3. A soron lévő vevőnek kért kaparós a tartóról E-vel vagy bal kattintással vehető fel. A vonalkódolvasóra rakva a szokásos kasszázás következik. A szelvény csak sikeres készpénzes vagy kártyás fizetés után érvényes.
4. Ötöske esetén a terminál Számsorsjáték lapján kell megadni a kért öt különböző számot 1–20 között, vagy a gépi számválasztást használni, ha a vevő azt kérte. Nyomtatás/felvétel után ugyanúgy fizetni kell. Sorsolás a következő játéknapon, egyszer.
5. A korábbi vásárló későbbi napon a saját szelvényével térhet vissza. Beváltás → beolvasás → kifizetés, vagy a szelvény visszaadása. A helyi kifizetési limit 10 000 Ft; nagyobb nyereménynél a fiktív üzemeltetőhöz irányítjuk. A beváltási idő az eladás/sorsolás után 7 játéknap.
6. Az Elszámolás lap mutatja az eladásokat, kifizetéseket, jutalékot és az üzemeltetői egyenleget. A pozitív egyenleg befizetendő, a negatív jóváírásként átvehető. Ezek játékbeli egyensúlyértékek, nem valós szerződési vagy számviteli feltételek.

| Termék | Fix ár | Bolti jutalék |
|---|---:|---:|
| Falusi Fortuna | 500 Ft | 50 Ft |
| Nyugdíj Plusz | 1 000 Ft | 100 Ft |
| Ötöske, 5 szám / 20 | 500 Ft | 50 Ft |

## Modulhatárok és pénz

- `ShopLotteryState`: tiszta bizományosi készlet, szállítás, nyomtatott/érvényes/beváltott szelvények, egyszeri eredmények és sorsolások, visszatérési jogosultság, üzemeltetői könyv. A pénzhez csak ellenőrzött callbacken keresztül fér hozzá; sikertelen fizetés/kifizetés után nem változik a tranzakció állapota.
- `ShopLotteryRetailer`: Unity-vizuál, fizikai papír és tartó, a meglévő vevő/checkout adaptere, készpénz-tartalék és eszközfókusz integráció. A runtime anyagoknak egy tulajdonosa van, a `ShopRuntimeResources`; a primitív meshek megosztottak.
- `ShopLotteryUi`: azonos szolgáltatási nézet a fizikai terminálon és a TobaccOS-ban. Azonos vezérlődefiníciókból rajzol és kínál billentyűzetes/célzott tesztcélokat.
- A hagyományos csomagolt termékek raktári katalógusa továbbra is 9 elemű. A fix árú lottótermékek az azonosítók végére kerültek; nem jelennek meg az általános ár- vagy rendeléseditorban.

Az üzemeltetői egyenleg: `eladások − kifizetések − jutalék − korábbi rendezések`. Pozitív része a `ShopLedger.LotteryReserve`; az `AvailableFunds` ebből nem enged vásárolni vagy fejleszteni. A készpénz fizikailag továbbra is a meglévő közös kasszában van.

Példa: 500 Ft-os eladásnál 500 Ft kerül a kasszába, 450 Ft félretéve, 50 Ft jutalék/nyereség. A kassza költségsora egyszer számolja el az üzemeltető 450 Ft-os részét; annak későbbi átutalása már csak pénzmozgás. Egy 500 Ft-os nyeremény kifizetése után −50 Ft az üzemeltetői egyenleg, az 50 Ft jóváírás annak rendezése, nem új árbevétel vagy jutalék.

## Állapotvédelem

- Egyedi `SJ-…` azonosító, egyszer rögzített kaparós eredmény, másolatként kiadott számtömbök. A nyomtatás önmagában nem jogosít beváltásra.
- Az NPC-k rendelése nem foglal készletet. Csak a fejlesztés és a megérkezett készlet után kérnek szelvényt; a sorban várva elfogyhat. A lottórendelések önálló szolgáltatási kosarak.
- A félbehagyott vásárlás ki nem fizetett papírját érvénytelenítjük, a bizományos készlet egyszer visszaáll. A visszaadott papír régi azonosítója nem aktiválható újra.
- A kifizetéshez a soron lévő NPC beolvasott, fizetett, érvényes, még be nem váltott szelvénye és elegendő tényleges kasszapénz kell. Állapot/nap/ismételt input ellenőrzése a pénzmozgás előtt történik.
- Normál vevők, tej/kávé, a bankkártyaterminál és a kassza meglévő útját használjuk. A lottóhoz nem készült külön pénztárca vagy új általános mentőrendszer.
- Napváltás megőrzi a szelvényeket, a tulajdonosukat, a készletet, a sorsolásokat és az elszámolást. Új játék visszaállítja ezeket. A projekt jelenleg nem ment teljes játékmenetet kilépésen át; csak a már létező beállítás-/elrendezésmentés maradt.

A játék jelenlegi négy vevőtípusa felnőtt karakter. Jövőbeli fiatalkorú NPC bevezetésekor a lottóhoz való hozzáférést is külön le kell tiltani. A #106-ban rögzített, kereskedelmi megjelenés előtti jogi és korhatár-felülvizsgálat továbbra is kiadási teendő; ez a technikai implementáció nem állásfoglalás arról.

## Ellenőrzés

`lottery-state`, `lottery-integration`, `lottery-numbers` a natív registryben; a teljes checkout-profil részei. Böngésző: `node Tools/Verify-Acceptance.cjs --case=lottery`. A fixture előkészíti a vevőket/időt/célzást, az eladás, számbevitel, fizetés, beváltás és rendezés valódi játékosinputot használ. A normál shipping playerbe nem kerül acceptance probe.

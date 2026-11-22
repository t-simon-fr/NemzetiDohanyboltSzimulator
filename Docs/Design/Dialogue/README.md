# Párbeszéd-rendszer újradolgozása — terv

Státusz: implementálva a #175-ben (dialógus-zoom + RPG-panel + összesítő-felújítás). Bizonyíték: [elfogadási jelentés](../../Acceptance/dialogue-zoom/REPORT.md). Kapcsolódó gyűjtő: #1 (dialógus-dobozok átdolgozása sor).

## Cél

1. A vevő megérkezésekor **enyhe zoom a vásárlóra**, és mint egy RPG-ben: **párbeszédpanel** mutatja, amit mond.
2. A generált [rendelésösszesítő-koncepció](order-summary-concept.webp) **marad a jelenlegi helyen** (kassza/számla nézet): név-chip, hangulatjelzés, tételes sorok, összeg.
3. A karakterek **ne mindig ugyanazt kérjék**, és ne ugyanazzal a mondattal.

## Jelenlegi állapot (mért kiindulás)

- `ShopPeople.Order`: „Kérek {tételek}. {Greeting[person]}" — személyenként 4 fix köszönés/köszönet/reklamálás.
- `ShopCustomerOrders.CreateCustomerOrder`: első tétel a spawnolt termék, a többi a katalógus sorrendben következő eleme; kosárméret `1 + visit % 3`.
- `MvpShopHud`: egyetlen doboz mindenkinek (név + fázis + szöveg + sorok), portré és hangulatjelzés nélkül.
- Kamera: a játékos gyermeke (`PlayerInteractor` + `FirstPersonController` pitch-mezővel).

## Terv

### 1. Dialógus-esemény és kamera

- Trigger: az aktív vevő a pulthoz ér és `ServiceStage.Order` lesz → a kamera finoman ráközelít (javasolt: FOV-lerp, nem dolly — nincs ütközésveszély), a dialóguspanel alul beúszik.
- Vége: fizetés kész / köszönés / távozás / panelzárás → FOV és panel visszaáll.
- Peremhelyzetek: vevő távozása mondat közben (skip/dismiss lezárja), gyors egymásután (újratrigger), szünet/súgó nyitva (nincs zoom), TobaccOS-fókusz (nincs zoom monitorozás közben).
- Nincs új input-mód: az E/kattintás-folyamat változatlan; opcionális: E lépteti a gépelős kiírást.

### 2. Változatosság-motor (játéklogika, nem UI)

- Személyiség-súlyok: Pisti siet (1 tétel), Laci melóba visz (egy SKU-ból több), Marika dumál (vegyes kosár, könnyebb helyettesítő-elfogadás).
- Napszak-ízlés: reggel kávé-jelleg, este sör-jelleg — csak súlyozás, a készletkapuzás marad.
- Determinisztikus seed `(nap, visit)` alapján (`ShopReputationState.Sample`-minta), hogy a tesztek stabilak maradjanak.
- Template-pool szándékonként (köszönés/rendelés/köszi/reklamálás/helyettesítő) × személyenkénti hang; a törzsvendég a saját előzményére utal.
- `BasketSize`/készletlogika és a `SingleProductVisits`-fixturek változatlanok.

### 3. Panelek kinézete

- Dialóguspanel (új): beszélő-chip a meglévő `ShopPeople.Colors`-ból + hangulatcímke (sietős/beszélgetős) + mondat; opcionális gépelős kiírás. [Zoom-koncepció](dialogue-zoom-concept.webp): vevő-közeli nézet, alul RPG-panel névvel, hangulatjelzéssel, mondattal és `[E] Folytatás` hinttel.
- Összesítőpanel (meglévő helyén): a koncepciókép szerint — név, hangulat, tételes sorok ikonokkal, összeg. Gombok nélkül: az átadás marad fizikai.
- Betűkészlet marad (SDF, ékezetekkel); új betűtípus nincs.

### 4. Ellenőrzési terv (a későbbi implementációhoz)

- Natív: determinizmus (azonos seed → azonos rendelés/mondat), változatosság (különböző mondatok látogatásokon át), panel megjelenik/eltűnik a fázisváltásokra, kamera visszaáll.
- Képek 720p/1080p a dialógus-pillanatról; WebGL-eset valódi vevőfolyammal.
- Perf: a kamera-lerp nem allokál frame-enként; a template-választás O(1).

### 5. Eldöntött kérdések (jóváhagyva)

- Gépelős kiírás + E lépteti a szöveget.
- Portré: színes monogram a meglévő `ShopPeople.Colors`-ból; Misi/Nóra-arcok később (#11).
- Zoom: FOV-lerp (nincs ütközésveszély, nincs dolly).

## Kiszerelési javaslat

- Spike-issue a tervvel (ez a dokumentum).
- Implementáció 1: változatosság-motor + suite-állítások.
- Implementáció 2: kamera + panelek + képi/WebGL-bizonyíték.

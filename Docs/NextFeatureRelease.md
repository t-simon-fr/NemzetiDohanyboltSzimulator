# Új üzletfejlesztések, bankkártya és naplezárás – 2026. november 3.

## Elkészült

A #1 issue jóváhagyott hét feladata: három kávérecept kézi feltöltéssel, többtermékes kosár, vásárolható hűtő, bővítéskor L alakú pult és arra költöző kávégép, valódi külterület és kiesett tárgyak visszahelyezése, grafikai részletek, fejlesztői mód. Ezen felül bankkártyás fizetés és a Pagesen jelzett napvégi elakadás javítása.

- Rövid kávé 650 Ft, hosszú kávé 750 Ft, latte 1100 Ft. A kávé és a tej külön rendelhető; a raktárba érkező csomagot E-vel fel kell venni, majd a gépnél E-vel betölteni. Minden recept egy kávéadagot, a latte egy tejadagot is fogyaszt. A gépnél 1/2/3 választ receptet, E főz.
- A vásárlók 1–3 tényleges terméket vesznek. Az önkiszolgáló áruit maguk hozzák a pulthoz; a személyzeti polcról a játékos adja át az árut. Csak a hiánytalan kosár fizethető. A visszavont vásárlás megőrzi a készletet.
- A 18 000 Ft-os hűtő a bővítés után, nyitás előtt vásárolható a fejlesztési alkalmazásban. 16 kézzel tölthető polchelye van; a 4°C kijelzés ebben a változatban díszlet, nincs hőmérséklet- vagy áramfogyasztás-szimuláció.
- A bővítés az eltűnt fal mellett L alakú pultot kapcsol be; a külön megvásárolt kávégép erre kerül. Az összes régi polchely továbbra is használható.
- Járda, utca, cégér, lámpák és növények készültek. A föld alá esett áru, karton és eszköz az ajtó elé kerül vissza. A karakterek arcot, ruharészleteket, kart, kezet és haj/sapka variációt kaptak.
- F1 fejlesztői panelt nyit az Editorban és development buildben. Pénz, vásárlók és tolvajok létrehozása, készletfeltöltés, fejlesztések, kosz, időléptetés és új játék érhető el. Normál nyilvános WebGL-buildben a panel le van tiltva.
- Egyes vásárlók bankkártyával fizetnek. E/F a kasszánál vagy az új POS-terminálon, majd Enter vagy a jóváhagyás gomb. Nincs visszajáró, a kasszafiók zárva marad. A bevétel külön bankszámlára kerül; rendelés, fejlesztés és működési költség először ezt használja, majd készpénzt. A bankegyenleg és a készpénz a következő napra megmarad; új játék törli őket.
- A napvégi részletes elszámolás görgethető. A Következő nap (Enter), Új játék (R) és Kilépés gomb külön, mindig látható területet kapott. Fizetésképtelen nap után az Új játék is elérhető. A hosszú készletlista korábban kiszoríthatta a gombokat a rögzített panelből.

## Ellenőrzés

29 ellenőrző csoport, 3796 sikeres állítás, 0 sikertelen a Unity-jeleneten. Jelentés: `Builds/NextFeatureRelease/checks.json`.

Az új ellenőrzések valódi csomagfelvételt és feltöltést, mindhárom receptet, 8 fős többtermékes műszakot, fizetés-visszavonást, készletmegőrzést, kültéri tárgy-visszanyerést, kártyás fizetést, ismételt jóváhagyás tiltását, bankból rendelést, napváltást és fizetésképtelenségből újrakezdést vizsgálnak. A fejlesztői billentyűkezelő belépési pontja és inputblokkolása ellenőrzött; a natív billentyűinjektálás nem igazolta önmagában az F1-et.

A korábbi regressziós forgatókönyvek egyelemű vásárlásokkal is futnak. A régi kávétesztekben a tesztsegéd a valódi csomagot veszi fel és tölti be; az üzleti kód nem tölt automatikusan alapanyagot.

WebGL-ellenőrzés: `node Tools/Verify-WebGL.cjs --features --dayend`, majd ugyanez a közzétett URL-lel. A böngészős futás képet ment a zárásról és az Enterrel megnyitott következő napról is.

## További feladatok

A műszak utáni maradás/csempész és az állítható eladási ár/kereslet a következő, helyi kiadásban elkészült: [EveningPricingRelease](EveningPricingRelease.md). A felhasználó kérésére az nem került Pagesre.

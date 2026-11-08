# #111 – Tea és forró csoki

## Eredmény

A meglévő kávégépen öt recept választható. A tea saját teaalapanyagból, a forró csoki kakaóporból és a lattéval közös tejkészletből készül. Négy külön beszerzési/készletsor, fizikai csomagszállítás és kézi feltöltés, pontos hiányjelzés, meglévő pohárátadás és fizetés. [Használat és modulok](../../Architecture/HotDrinks/README.md).

## Natív ellenőrzés

Az összesített checkout-profil [baseline futása](unity-baseline.json) **48 suite / 1162 sikeres állítás** után megállt egy régi partnerségi teszt 23 elemre rögzített kosárméretén. A teljes futás eredménye ezért **failed**, nem friss teljes siker. A fixture katalógusméretét a tényleges kapacitásra igazítottuk; runtime-kód nem változott. A [végső célzott futás](unity-final.json) **7 suite / 376 sikeres állítás**: partner-pricing20, partner-integration79, partner-targeting34, reputation-state27, reputation-integration38, hot-drinks-state130, hot-drinks-integration48.

A két kör együtt **55 suite / 1538 sikeres állítás** lefedését adja. A 48 érintetlen suite explicit baseline, a hét érintett/visszamaradt suite friss futás; nem ismételtük a már sikeres teljes részt. A baseline és végső futás közti változások kizárólag az ellenőrzést érintik: a PartnerChecks kosárméret, az acceptance Snapshot öt receptet is lefedő gombkoordinátái, illetve az új böngészőteszt saját kattintási segédje. A játék runtime-működése nem változott. A végső jelentés és [build előtti forrás](source.json) lenyomata egyezik.

Az új próbák ellenőrzik az append-only SKU-kat, mind az öt recept összes alapanyagát, szállítási időt és egyszeri érkezést, fizetés/készletkapacitás miatti elutasítást, megérkezett csomagok kézi betöltését, közös tej elfogyását, foglaltságot, ismételt inputot, menü újranyitását, kimeneti visszatérítést, rossz ital visszautasítását, konkrét teás/csokis pohárátadást és egyszeri kártyás könyvelést. A testben módosított árú tea+csoki 1450 Ft; a rögzített önköltség 370 Ft. A kávé/tej régi beszerzése, mindhárom kávérecept és a készletérzékeny rendelés a baseline-ban szintén sikeres.

## Natív képi és interakciós review

Reviewer: Codex saját review, nem független agent. Valódi Game view GUI-val, 1918×879: [tea kiválasztva](native-tea.png), [főzés](native-brewing.png), [TobaccOS](native-computer.png). Az öt recept és a négy alapanyag elfér, a készlet olvasható. Az első képi körben talált régi „1 / 2 / 3” súgó helyett a tényleges kattintásos receptválasztás szerepel; a melegital-árkategória neve is pontosítva.

A natív képeket és interakciót az utolsó, csak acceptance-telemetriát érintő változtatás előtti runtime-ról visszük tovább; a megjelenés és a működés azonos. A natív próba Input System egérállapot-eseményekkel, a rendes Update/vezérlő útvonalon választotta ki a teát, indított egy főzést és vette fel a tényleges teás poharat. [Végállapot](native-input.json): tea9, tej10, kávé0, kézben tea, eszközfókusz None. Nem OS-egérként elszámolt próba. A beszerzés/feltöltés és a főzés idejének előreléptetése API-fixture volt. A WebGL-kör a teljes folyamatot valódi böngészőbemenettel ellenőrzi.

## WebGL

[Végső helyi build](build.json): **0 hiba / 3 Unity/Emscripten figyelmeztetés, 257,72 s**. Előtte egy [433,58 másodperces hibamentes build](build-initial.json) készült, de a tesztadapter még csak négy gomb koordinátáját adta ki. Az acceptance Snapshot bővítése indokolta az új buildet; a játék viselkedése és megjelenése nem változott.

[Új melegitalok](browser.json): **35**, [három kávérecept és szünet/fókusz](browser-coffee.json): **19**, [közös forgalmi/kiszolgálási regresszió](browser-checkout.json): **26 sikeres böngészős ellenőrzés**, mindenhol **0 konzolhiba**, azonos player-asset SHA256 értékekkel. A főmenü/indítás állításai átfednek. A teljes helyi minták a JSON-okban megnevezett Builds-könyvtárakban maradnak.

A böngészős runner kezdeti próbáinál a manuálisan megállított fixture-óra nem dolgozta fel az Esc-et, a kakaó előtt a fizikailag előtte álló tejet kellett felvenni, illetve a korábbi pohár takarta az egyik rögzített célzási sugarat. A végső runner a rendes játékórát indítja az input előtt, elölről veszi fel a csomagokat és szabad elülső átadóhelyet használ, ahonnan a játék a poharat a saját nyugtahelyére mozgatja. A hibás próbák nem számítanak sikernek. Csak `Tools/Verify-Acceptance.cjs` változott a végső natív/build-forrás után; a [böngészős forráslenyomat](browser-source.json) ezt külön rögzíti, runtime/probe nem változott, ezért új player-build nem kellett.

A teljes új folyamat valódi böngészős input: három alapanyag beszerzési gombja (3700 Ft), E/egér felvétel és feltöltés, fizikai tea/csoki receptgombok, főzés, ismételt/foglalt input elutasítása, menübezárás/újranyitás, valós pohár átvétele és E-s átadása, 1350 Ft-os kártyás fizetés és ismételt jóváhagyás. A fixture csak tőkét/gépet, időelőreléptetést, nézőpontot és egy konkrét kétitalos NPC-rendelést készít elő; a hiánypróba végén külön kiüríti a tejet. Az új italok közben a kávékészlet végig0; a tea/csoki után tea9, kakaó9, tej9. Tejhiánynál csoki nem indul, tea igen. A natív körben a rossz ital, az egyedi eladási ár és a 370 Ft-os önköltség is ellenőrzött.

Codex saját [vizuális review](visual-review.json): mind a 11 új WebGL-kép átnézve. [TobaccOS720p](1280x720-hot-drinks-computer-720.png) / [1080p](1920x1080-hot-drinks-computer-1080.png), [tea720p](1280x720-hot-drinks-tea-720.png) / [1080p](1920x1080-hot-drinks-tea-1080.png), [csoki1080p](1920x1080-hot-drinks-chocolate-1080.png), [tejhiány720p](1280x720-hot-drinks-no-milk-720.png) / [1080p](1920x1080-hot-drinks-no-milk-1080.png), [átadás720p](1280x720-hot-drinks-served-720.png) / [1080p](1920x1080-hot-drinks-served-1080.png), [tea csomag](1280x720-hot-drinks-tea-package.png), [kakaó csomag](1280x720-hot-drinks-cocoa-package.png). A hosszú La Vazze-sor saját sorában törik, átfedés/vágás nélkül; a négy rendelés, öt recept, készlet, fő művelet és hiányjelzés elérhető/olvasható.

## Hatókör

A korábbi SKU-k 0–22 változatlanok; tea23 és forrócsoki24 kész ital, nem polcos beszerzési áru. Két saját, egyszerű kartonmodell került be, új vendéglátóhely vagy gyorsbillentyű nélkül. A helyi idegen anyag-/projektbeállítások és Recovery/Data tartalom nem része a commitnak. Pages-kiadás nincs.

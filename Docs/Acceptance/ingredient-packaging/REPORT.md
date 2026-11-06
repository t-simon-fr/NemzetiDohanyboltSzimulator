# Albro / La Vazze modellek és bal klikkes feltöltés

Feladatok: #115, #116. Végleges runtime-forrás: `0ee401881b5a62f4a8f36e3d57353e024aff501a`; [forrásazonosság](source.json), sourceFingerprint: `0ccd5d820076d96b08285a2775d4072b261514691b434c4f9f0ef0c09d56af05`.

## Eredmény

- Albro kupakos karton és La Vazze hajtott kávécsomag a jóváhagyott [referencia](../../Design/Ingredients/README.md) alapján, saját importált modellekkel és textúrákkal. Megosztott assetek, egyetlen gyökércollider, olvasható márkanevek raktárban és kézben.
- A hordott alapanyag a játékos felé néz, nem fordított. Eldobás/visszavétel megőrzi a modellt és az alapanyagmennyiséget.
- A gépre bal kattintás és E egyaránt pontosan egy csomagot tölt be. Feltöltéskor nem nyílik menü és nem indul főzés; a következő üreskezes kattintás a menüt nyitja.
- A gépnél megszűnt a régi, elavult gyorsbillentyűket mutató lebegő súgó, és az alapanyag tooltipje nem ismétli a használati utasítást.
- A meglévő árak, 10 adagos csomagok és receptek változatlanok. Jelenet-regenerálás és Pages-publikálás nem történt.

## Ellenőrzés

**Unity: 165 sikeres állítás / 7 célzott suite, 0 hiba.** [Jelentés](unity.json): ingredient-packaging 21, ingredient-pickup 21, coffee-world 29, coffee-display 37, refactor-coffee 17, tool-storage 20, checkout-revision 20. A riport a commit előtti azonos fájltartalmat mérte; a forráslenyomat egyezik a végleges commitéval. Az automatikus domain reload két korábbi futást még a tesztek indítása előtt megszakított; a friss végleges futás sikeres. Nem teljes checkout-profilként állítjuk ezt a célzott kört.

**Natív interakció:** Unity Game view, 1920-as asztali ablak; a két tényleges OS-bal kattintás a kézben tartott kávét és tejet is betöltötte: 10/10 adag, 46 500 Ft, nincs eszközfókusz vagy főzés. [Állapot](native-input.json). A stabil képi ellenőrzéshez a nézésvezérlő ideiglenesen le volt tiltva; az interaktor működött, a vezérlő utána visszaállt. Ez nem mozgásteszt. A kávé natív felvétele Input System E-eseménnyel, a tejé tényleges egérkattintással történt; a WebGL-kör mindkét inputot teljesen böngészőbemenettel is ellenőrizte.

**WebGL: egy sikeres build**, 0 hiba, 3 figyelmeztetés, 27 078 850 byte, 573,37 s. [Build](build.json). A build utáni fájllenyomat a natív tesztével azonos. A régi 30 s-os script-eval kérés időtúllépett, a build folytatódott; a friss build-result.json igazolta a sikert. Nem indítottunk emiatt új buildet.

**Böngésző: 14 sikeres állítás, 0 hibás állítás, 0 konzolhiba.** [Jelentés és buildhash-ek](browser.json), `node Tools/Verify-Acceptance.cjs --case=ingredient-pickup`. Tényleges E-felvétel, kattintásos felvétel, mindkét alapanyag bal klikkes és E-s feltöltése, ismételt üreskezes használat, pénz-/adagmegőrzés. A tesztfixture csak előkészíti a helyzetet/célzást; az input a valódi PlayerInteractor.Update útján fut.

## Vizuális review

Reviewer: Codex, 2026-11-06. A tényleges Game view és az alábbi végleges WebGL-képek egyenként átnézve 1280×720 és 1920×1080 felbontásban. A márkák, kupak/hajtások és betűk látszanak; a csomag nem takarja a célkeresztet, nem fordított, a tooltip elfér. Kezdetben a kávé nyomtatása belelógott a testbe: javítva, a hajtásokat követő nyomtatott felületek a test elő-/hátlapját helyettesítik. A végleges képeken nincs fedés. További runtime-módosítás a build után nem történt.

- [Mindkét termék a raktárban](1280x720-ingredient-coffee-target.png)
- La Vazze kézben: [720p](1280x720-la-vazze-held.png), [1080p](1920x1080-la-vazze-held-large.png)
- Albro kézben: [720p](1280x720-albro-held.png), [1080p](1920x1080-albro-held-large.png)
- [Feltöltött gép, 10 kávé / 10 tej](1920x1080-ingredient-machine-refilled-large.png)

Implementálva, Unityben, vizuálisan és helyi WebGL-ben ellenőrizve. Publikálva: **nem**, külön felhasználói kérésre. A felhasználó korábbi, ide nem tartozó material/project-settings módosításait és Recovery/Data fájljait nem commitoltuk vagy állítottuk vissza.

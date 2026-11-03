# Esti nyitva maradás és állítható árak – 2026. november 3.

A #1 issue két jóváhagyott feladata helyben elkészült. A felhasználó kérésére ez a kiadás nem kerül GitHub Pagesre; az éles változat továbbra is 43f88b0.

## Este a boltban

A napvégi összesítőn a **Maradok a boltban** gomb vagy **S** zárás utáni állapotba lép. A műszak pénzügyi lezárása egyszer történik meg; nem érkeznek új vásárlók, lehet tovább pakolni és takarítani. A raktáron át a hátsó udvarra lehet menni. **O** visszavisz az elszámolásra; onnan Enter a következő nap.

A hátsó udvaron esténként megjelenik egy kitalált szereplő, a Gyanús alak. Közelről **E** nyitja a beszélgetést. Egy este egy 5 csomagos Dnyipro ukrán cigaretta-ajánlat vehető meg, **3500 Ft-ért**. Enter vagy a vásárlás gomb fogadja el; Esc kilép. Nincs ingyen készlet vagy automatikus polcfeltöltés: az öt valódi csomag az udvari átvételi asztalra kerül, és kézzel lehet bevinni a boltba. A megszokott polcokra tehető, később a rendes kasszánál eladható. A Dnyipro nem rendelhető a normál nagykerből.

A fizetés a bankszámlát, majd a készpénzt használja, ugyanúgy mint a normál beszerzés. Az ismételt jóváhagyás nem ad újabb árut. A kereskedő ajánlata csak a következő estén újul meg; foglalt átvételi asztal esetén előbb el kell vinni a korábbi csomagokat. Az éjszakai világítás a nappalra visszatéréskor helyreáll. Új játék törli a vásárolt árut és az esti állapotot.

## Árképzés

A TobaccOS asztalán az **Árak** gomb vagy **6** nyitja az új alkalmazást. Előző/következő választ terméket, a gombok **100 Ft-os és 5 Ft-os** lépésekben változtatnak. **+ / −** 100 Ft, **Shift + / −** 5 Ft. Az **Alapár** visszaállítja a katalógusárat. Tab/nyilak és Enter/Space is működik.

Az ár az alapár 50–300%-a között, öttel osztható forintösszeg lehet. Az alkalmazás mutatja az eladási árat, a beszerzési árat, az alapárat és a becsült ár-elfogadást. A feloldott normál termékek, a megvásárolt kávégép három receptje és a már megvett Dnyipro is árazható. Az árcédulák, felvételi feliratok és kávégombok a beállított árat mutatják.

A vevők öt költségkeret-csoportba tartoznak: alapár, illetve 10%, 20%, 30% és 40% elfogadott felár. A csoport a látogatás és a nap alapján változik. Ha a kosár bármelyik tétele drágább az adott keretnél, a vásárló nem vásárol, és a készlet nem fogy el. Az alapár és az akciós ár minden csoportnak megfelel; a nagy felár több visszaforduló vevőt eredményez. A becslés az öt csoport arányát jelzi, nem garantált napi darabszám.

Az árakat a vevő érkezésekor rögzítjük. A már leadott rendelés összege, kapott pénze és visszajárója akkor is változatlan, ha közben más árat állítasz be. A pénztár, az érmés és a kártyás fizetés a rögzített kosárárat használja. Az összesítő külön mutatja az ár miatt visszafordultak számát. Az árak megmaradnak a futó játék következő napjára, új játék alapárakat állít vissza.

## Ellenőrzés

**32 csoport, 3854 sikeres ellenőrzés, 0 sikertelen.** Jelentés: `Builds/EveningPricingRelease/checks.json`. Az új `EveningPricingChecks` három forgatókönyve az árfüggő visszautasítást, árrögzítést, tényleges kasszázást, esti vásárlást, ismételt fizetés kizárását, világítás helyreállítását, fizetésképtelenséget és a csempészcigi másnapi vegyes kosaras eladását ellenőrzi.

Képek: `Builds/EveningPricingRelease/pricing.png`, `night-yard.png`, `night-offer-final.png`. Helyi WebGL-próba: `node Tools/Verify-WebGL.cjs --features --dayend --evening`. Ez az új árazóalkalmazás billentyűzetes megnyitását és árváltoztatását, a napvégi S-sel maradást, az O-val visszatérést és az Enteres napváltást is lefedi.

A helyi WebGL-build sikeres: 0 hiba, 0 figyelmeztetés, 21 004 037 bájt. A Chromium-próba 21 képet mentett, 0 böngészős hibával; az árváltoztatás, esti maradás, visszatérés és a 2. nap képe is ellenőrizve. Jelentés: `Builds/EveningPricingRelease/build-status.json`; képek és konzol: `Builds/Verification/local-*`. Pages-közzététel nem történt.

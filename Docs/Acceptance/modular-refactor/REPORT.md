# Moduláris refaktor — #69–#74

Mind a hat engedélyezett refaktor implementálva, külön commitokban és függő PR-ekben (#100–#105). A játékosi folyamatok és a szerializált scene/prefab hivatkozások megmaradtak. A modulhatárok, készlettulajdon és erőforrás-élettartam részletes leírása a [végrehajtási dokumentumban](../../Architecture/REFACTOR_BATCH.md) található.

| Issue | Eredmény |
|---|---|
| #69 | Mellékhatásmentes beszerzési ajánlat; közös, teljes kosáron validált rendelési alkalmazás. |
| #70 | ShopInventory az egyetlen raktári/szállítmány-mennyiségi tulajdonos, explicit kivét/visszaadás és időzítés. |
| #71 | ShopCheckoutSession: átadott tételek, explicit nyugtahely-tagság, egyszeri fizetési állapot. |
| #72 | ShopCoffeeMachineState: recept, közös alapanyag, főzési idő, szállítás és foglalt kimenet visszatérítése. |
| #73 | ShopDayCycle; közös kamera-pillanatkép, eszközfókusz és visszaállítható menü/editor szünet. |
| #74 | ShopBootstrap, ShopSceneBindings, runtime erőforrás-tulajdon és párosított enable/disable feliratkozások. |

## Friss bizonyíték

- Runtime forrás: c3dfd3d1353914fe1aa7c2829423e3260786d895; [azonosító/lenyomat](source.json). A natív futás az utolsó runtime commit előtt, annak előkészített fájltartalmából történt; a natív jelentés és a buildhez rögzített forráslenyomat megegyezik.
- [Teljes natív checkout profil](unity.json): **852 sikeres állítás, 40 csoport**, friss fordítással. Ebből 78 állítás a hat új refaktor-suite-ban: atomikus beszerzés, szállítás/rollback, átnevezett nyugtahelyek, ismételt termékek, dupla fizetés, főzési állapot, beágyazott fókusz és életciklus.
- [Közös helyi WebGL-build](build.json): **sikeres, 0 hiba, 3 figyelmeztetés**. Assembly/GUID vagy stripping/link.xml változtatás nem kellett.
- [Összesített böngészős eredmény](browser-summary.json): **123 különböző sikeres ellenőrzési felirat, 132 sikeres állításfuttatás, 0 feloldatlan hiba, 0 konzolhiba**, ugyanazon buildfájlokból. Valódi egér/billentyű: készpénz/kártya/érmék, három azonos áru, készlet/kartonmozgatás, folyamatos vevők, ajtó, három kávé, rendelő, szünet/átkötés és WebGL-újratöltés, átrendezés mentése és következő nap.
- A workflow eszköz 15 önellenőrzése, a böngészős runner szintaxisellenőrzése és a dokumentációs linkellenőrzés sikeres.

## A böngészős futtató korrekciói

A [kezdeti futás](browser-1.json) 81 sikeres állítás után a régi kosár-beviteli segédnél állt meg: a mező elejére került kurzor miatt a kezdő 0 megmaradt. A [következő diagnosztika](browser-2.json) az elavult pluszgomb-koordinátát fedte fel. Teljes kijelölés és a valódi kontroll-rectek használata után a [UI-folytatásban](browser-3.json) a rendelő, kávégép és menük sikeresek voltak; a régi fizikai átrendezőgomb-célzásnál megállt. A tényleges Arrange:0 kontroll és explicit kiinduló felbontás használatával a [végső folytatás](browser-4.json) teljesen sikeres.

A nyers jelentések megőrzik ezeket a futtatóhibákat. Mindhárom sikertelen ellenőrzésnek van későbbi sikeres megfelelője; minden futás ugyanazon WebGL-asset hash-eket rögzíti. A sikeres korábbi részeket nem futtattuk újra. Csak a runner változott a build után: [runner-forrás](runner-source.json), 4292182. Runtime kód nem változott, ezért nem indult új teljes natív kör vagy build.

## Vizuális és interakciós ellenőrzés

Reviewer: Codex (implementáló; nem független review). A tényleges Game view képeken a vezérlők megmaradtak és elérhetők; új grafikai tervezés nem történt. A vizuális ellenőrzést a fenti valódi interakciók egészítik ki.

[Készpénz, 720p](1280x720-cash-register-selected.png) · [Kéznézet, 1080p](1920x1080-hand-player-view-large.png) · [Latte, 720p](1280x720-coffee-display-latte.png) · [Rendelés, 1080p](1920x1080-cart-submitted-large.png) · [Átrendezés, 720p](1280x720-arrangement-overview.png) · [Mentett elrendezés, 1080p](1920x1080-arrangement-large.png) · [Következő nap](1280x720-next-day.png).

A meglévő felhasználói anyag-/render-/projektbeállítások és Recovery/Data fájlok érintetlenül megmaradtak; nem kerültek commitba. Jelenetmentés vagy -generálás nem történt. **Pagesre még nincs kiadva.**

# #124 – Partnercsomagok

Az **Üzletkezelés → Partnerségek** oldalon mindhárom csomag 20 000 Ft. Vásárolni nyitás előtt vagy zárás után, üres boltban lehet. A hűtős csomagokhoz az önkiszolgáló üzletbővítés szükséges; a Műszakkezdő a kezdőboltban is megvehető.

| Partner | Berendezés | Új változatok |
|---|---|---|
| Hamster Energy | fekete/zöld, 1,65×0,8 m-es hűtő | Zero, Pacific Mango |
| Loca Cola | azonos méretű piros/krém hűtő | Fantom narancs, Spritz citrus |
| Kevert / Műszakkezdő | fix, pult feletti hat termékhelyes tartó | Szilva, Körte, Vegyes; 5 cl |

A hűtőt az átrendezőben kell elhelyezni; a szerződés és az egyszeri fizetés csak a terv mentésekor lép életbe. Megszakításkor nincs levonás vagy feloldás. A márkás bútor csak a saját családját fogadja. Üres partnerhűtő 9000 Ft-ért eladható, a végleges szerződés megmarad; a csomag egyszer vásárolható. A termékek hagyományos polcokra is feltölthetők.

Az aktív partner családjába tartozó **új rendelések egységköltsége 10%-kal alacsonyabb**, egész forintra lefelé kerekítve. A korábbi készlet és a beállított eladási ár változatlan. A kosár végső fizetése 5 Ft-ra kerekedik; az eltérés külön sorban és a napi eredményben szerepel. Példa: Fantom 180→162 Ft/db, egy darab rendelésénél 160 Ft fizetendő, −2 Ft kerekítés. Ez a felhasználónak jelzett implementációs alapfeltevés, nem külön megerősített árpreferencia.

A hét változat külön rendelési sort, kartont, fizikai modellt, készletet és eladási árat kapott. Az NPC-k a meglévő készletalapú rendelésen át választanak: feloldás és beérkezett készlet nélkül nem kérik az új árut; a várakozók továbbra sem foglalják le előre. A partnerségek, bútorok és készlet napváltáskor megmaradnak, új játék visszaállítja őket. Általános lemezes játékmentés nem része a feladatnak.

## Modulhatárok

- `ShopPartnerCatalog` / `ShopPartnerState`: definíciók, feloldások és tiszta kedvezményszámítás.
- `ShopPartnerships`: szerződés, bútorvásárlási integráció és reset; `ShopPartnerVisuals`: saját low-poly megjelenés és megosztott runtime-anyagok.
- A meglévő beszerzés, készlet, polc, árkezelés és checkout szolgálja ki a változatokat. A szállítás rögzíti az egységköltséget, amely a fizikai termékre és a kasszához is eljut.
- A régi 0–15 termékazonosító és `Count=9` alapcsomag-tartomány változatlan. Új ID-k 16–22; `SupplyCapacity` és `SupplyTypes` külön kezeli a nem folytonos beszerzési kínálatot, a kávé/lottó/csempészáru beszerzésbe keverése nélkül.

[Ellenőrzési jelentés](../../Acceptance/partners/REPORT.md). Pages-publikálás nincs.

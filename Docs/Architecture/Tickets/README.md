# Vonaljegy és gyűjtőjegy – használat

Két fiktív közlekedési jegy a pult mögötti készletből, kézi átadással: **Vonaljegy** (600 Ft) és **Gyűjtőjegy 10 utazás** (5000 Ft). Beszerzés a normál supplier-monitoron (300 / 2800 Ft), tárolás a pult alatti jegyslotokban, eladás a meglévő leolvasó/átadóhely + checkout rendszerrel, készpénzben és kártyával, vegyes kosárban is.

## Kiszolgálás

1. Rendeld meg a B-számítógépen (beszerzés), várd meg a szállítást (8 mp).
2. Vedd ki a dobozból (E / bal kattintás a raktári átadónál), és tedd a pult alatti jegytartó szabad helyére (zöld keret jelzi).
3. A vevő a típust és darabszámot mondja meg; a megfelelő jegyet tedd az átadóhelyre (kézi átadás) vagy olvasd le a leolvasón.
4. Fizesd ki készpénzzel vagy kártyával a szokásos kasszán. Rossz típus nem teljesíti a rendelést (X: nem tudom kiszolgálni).
5. Csak beszerzett típust kérnek; sorban álláskor nincs foglalás, a kifogyás a meglévő hiánykezelést használja.

## Pénzügy és állapot

Nincs külön elszámolás (nem bizományos, mint a lottó): a beszerzés normál `Purchases`, az eladás normál `Revenue`/`CostOfGoods`. Napváltás megőrzi a pult- és raktárkészletet; új játék mindent töröl. Ismételt átadás/megszakítás nem duplikál készletet vagy pénzt.

Terv: [PLAN](PLAN.md). Bizonyíték és a baseline/friss próbák pontos hatóköre: [elfogadási jelentés](../../Acceptance/tickets/REPORT.md).

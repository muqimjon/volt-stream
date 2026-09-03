# O'rnatish (Docker)

Server Docker'da ishlaydi — u yerda .NET o'rnatish shart emas. Filial kompyuterlaridagi WPF ilova esa
Windows'da ishlaydi, u alohida chiqariladi (pastda).

## 1. Serverga nima kerak

- Docker Engine + Compose plugin
- Ochiq port yo'q: tashqariga faqat cloudflared tunneli orqali chiqiladi

## 2. Sozlash

```bash
git clone <repo> voltstream && cd voltstream
cp .env.example .env
```

`.env` ni to'ldiring. `JWT_SECRET` ni tasodifiy yarating:

```bash
openssl rand -base64 48
```

`.env` gitga tushmaydi. Kalit o'zgarsa barcha tokenlar bekor bo'ladi va hamma qayta login qiladi.

## 3. Ishga tushirish

```bash
docker compose up -d --build
docker compose logs -f api
```

Birinchi ishga tushishda migratsiyalar va Excel seeding fonda bajariladi (`DatabaseInitializer`),
shuning uchun API darrov javob beradi, ma'lumot esa bir necha soniyadan keyin to'liq bo'ladi.

Tekshirish:

```bash
curl -f http://localhost:5000/api/health
```

## 4. Mavjud bazani ko'chirish (eng muhim qadam)

Mijozda ishlab turgan baza bo'lsa, uni Docker ichidagi bazaga ko'chirish kerak. **Avval nusxa oling.**

```bash
# eski serverda
pg_dump -U postgres -d voltstream -Fc -f voltstream.dump

# yangi serverda: dump faylni ./backups ga qo'ying (u konteynerga ulangan)
docker compose up -d db
docker compose exec db pg_restore -U $POSTGRES_USER -d $POSTGRES_DB --clean --if-exists /backups/voltstream.dump
docker compose up -d api
```

Yangi sxema (filiallar, ruxsatlar, og'irlik, audit, print job) migratsiya orqali qo'llanadi va
mavjud ma'lumot to'ldiriladi: hamma foydalanuvchi faollashadi, non-admin'lar default filialga
biriktiriladi, roliga qarab ruxsat oladi. **Bu qadamni avval bazaning nusxasida mashq qiling.**

## 5. Tunnel

```bash
cloudflared tunnel --url http://localhost:5000
```

Doimiy tunnel uchun `cloudflared service install` bilan named tunnel qiling. WebSocket o'tadi —
SignalR (chek chiqarish) shu orqali ishlaydi.

## 6. Filial kompyuterlari (WPF)

Desktop ilova Docker'da ishlamaydi. Har bir PC ga .NET o'rnatmaslik uchun **self-contained** chiqaring:

```bash
dotnet publish src/frontend/VoltStream.WPF -c Release -r win-x64 --self-contained true
```

Natijadagi papkani PC ga ko'chiring — runtime ichida keladi, hech narsa o'rnatilmaydi.

Har bir PC da:

1. Sozlamalar → Aloqa: tunnel manzili (https)
2. Sozlamalar → Printer: filialni tanlang, printerni tanlang, yoqing
   (`print.agent` ruxsati kerak, filial tanlanmasa agent ishga tushmaydi)

## 7. Planshet/telefon

Tunnel manzilini brauzerda oching → "Ilovani o'rnatish". PWA sifatida o'rnatiladi.

## 8. Zaxira nusxa

```bash
docker compose exec db pg_dump -U $POSTGRES_USER -d $POSTGRES_DB -Fc -f /backups/voltstream-$(date +%F).dump
```

Buni cron'ga qo'ying va `backups/` ni tashqi diskka ko'chirib turing.

## Yangilash

```bash
git pull
docker compose up -d --build
```

Yangi migratsiya bo'lsa startda qo'llanadi. Har yangilashdan oldin zaxira nusxa oling.

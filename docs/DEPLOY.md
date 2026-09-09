# O'rnatish (Docker)

Server Docker'da ishlaydi — u yerda .NET o'rnatish shart emas. Filial kompyuterlaridagi WPF ilova esa
Windows'da ishlaydi, u alohida chiqariladi (pastda).

## 0. Tayyor fayllar

Mijozga borishdan oldin dasturchi mashinasida bir marta bajariladi:

```
pwsh scripts/build-all.ps1
```

Natija `artifacts/` papkasida: `voltstream-server-<ver>.zip` (server), `voltstream-desktop-<ver>-win-x64.exe` (filial kompyuteri, .NET o'rnatish shart emas), `voltstream-<ver>.apk` (telefon). Zip ichida image, `docker-compose.yml`, `.env.example`, ushbu hujjat va baza ko'chirish skripti bor — mijoz serverida internet orqali image yuklab olish shart emas.

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

Mijozda ishlab turgan baza eski sxemada (2 ta migratsiya). Yangi versiya bitta `InitialMigration` bilan
keladi, shuning uchun EF uni o'zi ko'tara olmaydi — buning uchun tayyor skript bor: `docs/upgrade-from-v1.sql`
(idempotent: sxemani yangilaydi, migratsiya tarixini `InitialMigration` ga almashtiradi, mavjud
foydalanuvchilarni faollashtiradi, default filialga biriktiradi, roliga qarab ruxsat beradi). **Avval nusxa oling.**

```bash
# eski serverda
pg_dump -U postgres -d voltstream -Fc -f voltstream.dump

# yangi serverda: dump va skriptni ./backups ga qo'ying (u konteynerga ulangan)
cp docs/upgrade-from-v1.sql backups/
docker compose up -d db
docker compose exec db pg_restore -U $POSTGRES_USER -d $POSTGRES_DB --clean --if-exists /backups/voltstream.dump
docker compose exec db psql -U $POSTGRES_USER -d $POSTGRES_DB -v ON_ERROR_STOP=1 -f /backups/upgrade-from-v1.sql
docker compose up -d api
```

Skript ichida "transaction in progress" ogohlantirishlari chiqishi normal. Tekshirish: `api` logida
"No migrations were applied" bo'lishi, `admin` login qilishi, eski mijozlar/savdolar joyida turishi kerak.
Bu tartib bazaning nusxasida mashq qilingan: mavjud yozuvlar saqlanadi, birorta qator o'chmaydi.

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

## 8. Telefon (APK)

`src/mobile/VoltStream.Mobile` — yupqa WebView qobiq (native Android, `net10.0-android`). APK ichida web
YO'Q, u har safar serverdan yuklanadi (`/`), shuning uchun web yangilansa APK qayta chiqarilmaydi —
faqat qobiqning o'zi (masalan ikonka, ulanish mantig'i) o'zgarsa kerak bo'ladi.

Bir martalik: release imzo kaliti (yo'q bo'lsa yarating, bor bo'lsa qayta yaratmang — mavjud o'rnatilgan
APK'larni yangilab bo'lmay qoladi):

```bat
"C:\Program Files\Android\openjdk\jdk-21.0.8\bin\keytool.exe" -genkeypair -v ^
  -keystore "%APPDATA%\Ovoza\VoltStream\voltstream.keystore" ^
  -alias voltstream -keyalg RSA -keysize 2048 -validity 10000
```

Parolni xavfsiz joyda saqlang (gitga yozilmaydi). Build:

```bat
set VOLTSTREAM_KEYSTORE_PASS=<parol>
dotnet publish src/mobile/VoltStream.Mobile -f net10.0-android -c Release
```

Imzolangan APK: `src/mobile/VoltStream.Mobile/bin/Release/net10.0-android/publish/uz.ovoza.voltstream-Signed.apk`.
Telefonga o'rnatish:

```bash
adb install uz.ovoza.voltstream-Signed.apk
```

(yoki faylni telefonga ko'chirib qo'lda oching — "Noma'lum manbalardan o'rnatish" ruxsati so'raladi).

Birinchi ochganda Ulanish sahifasi chiqadi: Asosiy manzil (odatda LAN, `http://<pc-ip>:5000`) va Zaxira
manzil (odatda tunnel, `https://<domen>`) — ikkalasi ham ixtiyoriy. Ilova ochilganda avval asosiyni,
bo'lmasa zaxirani tekshiradi (`/api/health`); ikkalasi ham javob bermasa "Server topilmadi" ekrani
chiqadi. Manzilni keyinroq o'zgartirish uchun tezkor sozlamalar tugmasi yo'q (ataylab) — ilova
ma'lumotlarini tozalash (Android sozlamalari → Ilovalar → Volt Stream → Xotirani tozalash) yetarli.

## 9. Zaxira nusxa

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

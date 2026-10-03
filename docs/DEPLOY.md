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

`api` ni skriptdan OLDIN ko'tarmang: EF butun `CREATE TABLE` skriptini qaytadan bajarishga urinib
`relation "AllowedClients" already exists` bilan yiqiladi. Ma'lumot yo'qolmaydi (tranzaksiya qaytariladi),
lekin `/api/health` 200 qaytarib turib login 503 bo'lib qoladi — server tirik ko'rinadi, hech kim kira olmaydi.

```powershell
# eski serverda: dump konteyner ichida yaratilib, keyin ko'chiriladi
#   (PowerShell `>` ikkilik oqimni buzadi, shuning uchun -f va docker cp)
docker exec <db-konteyner> pg_dump -U postgres -d voltstream -Fc -f /tmp/voltstream.dump
docker cp <db-konteyner>:/tmp/voltstream.dump .\voltstream.dump

# yangi serverda: dump va skriptni ./backups ga qo'ying (u konteynerga ulangan)
copy docs\upgrade-from-v1.sql backups\
copy voltstream.dump backups\
docker compose up -d db
docker compose exec db pg_restore -U postgres -d voltstream --clean --if-exists /backups/voltstream.dump
docker compose exec db psql -U postgres -d voltstream -v ON_ERROR_STOP=1 -f /backups/upgrade-from-v1.sql
docker compose up -d api
```

`-U postgres -d voltstream` `.env` dagi `POSTGRES_USER`/`POSTGRES_DB` bilan bir xil bo'lishi shart.
Dump obyektlari `postgres` egaligida, shuning uchun `POSTGRES_USER` boshqa nom bo'lsa restore
`role "postgres" does not exist` bilan to'xtaydi. Dump `-Fc` siz oddiy `.sql` bo'lsa `pg_restore` emas,
`psql -f` ishlatiladi.

Skript ichida "transaction in progress" ogohlantirishlari chiqishi normal. Tekshirish: `api` logida xato
bo'lmasligi, `admin` login qilishi (503 emas), eski mijozlar/savdolar joyida turishi kerak.
Bu tartib productionning 2026-09-14 nusxasida mashq qilingan: 2995 savdo va 13838 qator saqlandi,
natijaviy sxema EF ning toza sxemasi bilan bir xil chiqdi.

## 5. Internet: bitta domen, Cloudflare Tunnel

Hamma narsa bitta manzilda: `https://volt-stream.muqimjon.uz` → Cloudflare named tunnel → serverdagi API
(`http://127.0.0.1:5000`). Web ham API'ning o'zidan beriladi (image ichida), shuning uchun alohida web
hosting (Pages) va alohida `api.` domeni YO'Q — web va API doim bir xil versiya, alohida deploy qilinmaydi.
Telefon zaxira manzili ham, filial kompyuterlari internet orqali ulansa ham shu manzil.

Tunnel dasturchi kompyuterida yaratiladi (`cloudflared tunnel login` qilingan bo'lsin):

```bash
cloudflared tunnel create volt-stream-api
cloudflared tunnel route dns --overwrite-dns volt-stream-api volt-stream.muqimjon.uz
```

Serverda (Windows, administrator) `config.yml` va `<tunnel-id>.json` `C:\ProgramData\cloudflared\volt-stream\`
ga qo'yiladi (papka faqat SYSTEM va Administrators uchun; scp bilan yuklangandan keyin
`icacls "C:\ProgramData\cloudflared\volt-stream\*" /reset` — aks holda fayl faqat yuklagan foydalanuvchiga
ochiq qoladi va xizmat `Access is denied` bilan yiqiladi):

```yaml
tunnel: <tunnel-id>
credentials-file: C:\ProgramData\cloudflared\volt-stream\<tunnel-id>.json
ingress:
  - hostname: volt-stream.muqimjon.uz
    service: http://127.0.0.1:5000
  - service: http_status:404
```

Xizmat `cloudflared-volt-stream` (avtomatik, kechiktirilgan start) `New-Service` bilan yaratiladi:
`"C:\Program Files (x86)\cloudflared\cloudflared.exe" --config "C:\ProgramData\cloudflared\volt-stream\config.yml" --logfile "C:\ProgramData\cloudflared\volt-stream\cloudflared.log" tunnel run`.
WebSocket o'tadi — SignalR (chek chiqarish) shu orqali ishlaydi.

### SSH orqali boshqarish (Windows server)

- Standart shell — Windows PowerShell 5.1: ikkilik chiqishni `>` buzadi (`cmd /c "... > fayl"` ishlating),
  ichida qo'shtirnoq bor argumentlar native buyruqqa buzilib boradi — murakkab skriptni `-EncodedCommand` bilan yuboring.
- SSH sessiyasida Docker'ning Windows Credential Manager yordamchisi ishlamaydi
  (`A specified logon session does not exist`). Image tortish kerak bo'lsa vaqtinchalik `DOCKER_CONFIG`
  papkasidagi `config.json` ga `{"auths":{"localhost:1":{}}}` yozing (bo'sh `auths` yetmaydi — CLI baribir wincred'ni tanlaydi).
  Odatiy yangilashda tortish shart emas: image `docker load` bilan yuklanadi.

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

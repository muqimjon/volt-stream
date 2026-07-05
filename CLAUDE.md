# VoltStream

Kabel zavodi uchun hisob-kitob (savdo, ta'minot, to'lovlar, debitor/kreditor, ombor) dasturi.

## Arxitektura

`.NET 9`. Yechim ikki qismdan iborat (`VoltStream.sln`):

**Backend** (`src/backend`) — Clean Architecture + CQRS (MediatR):
- `VoltStream.Domain` — entitilar, enumlar.
- `VoltStream.Application` — CQRS komandalar/so'rovlar, validatsiya, interfeyslar.
- `VoltStream.Infrastructure` — EF Core (PostgreSQL/Npgsql), `AppDbContext`, JWT, Excel seeding (`ExcelDataSeeder`, `SeedData/*.xlsx` orqali), `DatabaseInitializer` (migratsiya + seeding fon `BackgroundService`).
- `VoltStream.WebApi` — controllerlar, middleware, Scalar API hujjati. Kompozitsiya `WebApiHostBuilder.Build` da; `Program.cs` shuni chaqiradi.

**Frontend** (`src/frontend`) — WPF (MVVM, CommunityToolkit.Mvvm, Mapster):
- `VoltStream.WPF` — oynalar/sahifalar/ViewModel'lar, DI uchun generic `Host`.
- `ApiServices` — Refit klient interfeyslari (`I*Api`) va modellar.

## Ishga tushirish

1. PostgreSQL: `localhost:5432`, baza `voltstream`, `postgres/root` (`appsettings*.json` dagi `ConnectionStrings:DefaultConnection`).
2. Visual Studio: **"Combined"** launch profili (`VoltStream.slnLaunch`) — `VoltStream.WebApi` (https) + `VoltStream.WPF` ni birga ishga tushiradi. Bitta F5 yetarli.
3. CLI:
   ```
   dotnet build VoltStream.sln
   dotnet run --project src/backend/VoltStream.WebApi   # https://localhost:7285 + http://localhost:5123
   src/frontend/VoltStream.WPF/bin/Debug/net9.0-windows/VoltStream.WPF.exe
   ```

WebApi seeding'dan OLDIN tinglay boshlaydi (migratsiya/seeding `DatabaseInitializer` da fonda). Shuning uchun klient va server bir vaqtda ishga tushsa ham klient muzlab qolmaydi.

## Klient ↔ server ulanishi

- Server manzili QO'LDA kiritiladi (host/port/https) va `config/api-connection.json` ga saqlanadi (`ApiConnectionStore`, faqat `Url`). Avto-kashf (discovery/broadcast/skan) va fon qayta-ulanish YO'Q.
- WPF ishga tushganda login oynasini ko'rsatadi. Saqlangan URL bir marta tekshiriladi (`ServerHealth.IsAliveAsync` → `/api/health`); tirik bo'lsa va USB kalit/eslab qolingan parol bo'lsa avto-login.
- Login bosilganda URL bir marta tekshiriladi; ulanmasa `ConnectionRecovery` ulanish sozlamalari oynasini ochadi — foydalanuvchi qo'lda tuzatib davom etadi.
- Ish jarayonida biror API chaqiruvi serverga ulanolmasa (`AuthHeaderHandler`) o'sha sozlamalar oynasi ochiladi (bitta oyna, debounce).
- Portlar: dev `7285` (https), `5123` (http); native production `5000`.
- `/api/health` `[AllowAnonymous]` — ulanish tekshiruvi shu endpointga tayanadi.
- Avto-login: removable USB diskdagi `voltstream.key` (`DevKeyService`). Kalit bo'lmasa — qo'lda login.

## Til / Lokalizatsiya

- 4 til: `uz-Latn` (asos/fallback), `uz-Cyrl`, `ru`, `en`. Tanlov ish vaqtida (restartsiz) almashadi — header'dagi globus tugmasi menyusidan.
- Matnlar `Commons/Localization/lang/<code>.json` (embedded resource) da; kalit format `Area.Name` (masalan `Common.Save`, `Sales.CustomerLabel`).
- XAML: `{loc:Loc <Key>}` (root'da `xmlns:loc="clr-namespace:VoltStream.WPF.Commons.Localization"`). C#: `TranslationSource.T("<Key>")`.
- `TranslationSource` (INotifyPropertyChanged singleton) indeksator orqali jonli yangilanadi; `LocalizationManager` JSON'ni yuklaydi, tanlovni `config/language.json` ga saqlaydi, `LanguageChanged` event beradi (`App.OnStartup` da `Initialize`).
- Yangi matn qo'shsang: 4 JSON'ga bir xil kalit qo'sh (izchillik uchun `Common.*` va glossariyni qayta ishlat), keyin `{loc:Loc}`/`T(...)` bilan bog'la. Brend "Volt Stream" va til menyusi autonimlari tarjima qilinmaydi.

## Tez o'tish (Command palette)

- `Ctrl+K` (yoki header'dagi qidiruv tugmasi) istalgan joyda command palette'ni ochadi — sahifalar bo'yicha tez qidirish va o'tish. `↑↓` tanlash, `↵` ochish, `Esc` yopish.
- Ro'yxat `MainViewModel.paletteItems` da (`PaletteItem`: `Nav.*` kaliti + `IconChar` + navigatsiya). Yangi sahifa qo'shsang shu ro'yxatga ham element qo'sh.
- Sidebar faol holati `ActivePageKey` orqali sinxron (palette bilan o'tganda ham to'g'ri belgi yonadi). Matnlar `Palette.*` kalitlarida (4 tilda).

## Biznes qoidalari (ATAYLAB shunday — buzma)

Bu yerdagi qoidalar "mantiqan g'alati" ko'rinishi mumkin, lekin BUYURTMACHI so'ragani uchun ataylab shunday qilingan. Upgrade/refactor paytida bularni "xato" deb tuzatma — avval shu ro'yxatni tekshir. Yangi shunday qaror qabul qilinsa, shu ro'yxatga qo'sh.

- **Ombor minusga tushishi mumkin (oversell).** Savdoda mahsulot omborda yetarli bo'lmasa ham sotishga ruxsat beriladi; foydalanuvchi ogohlantiriladi va tasdiqlasa, savdo o'tadi va qoldiq minusga hisoblanadi. Backendda `CreateSaleCommand` da qoldiq yetarliligini tekshirib `ConflictException` tashlash YO'Q (olib tashlangan) — qaytarib qo'yma.
- **Rulonni kesish (partial roll).** So'ralgan metr to'plam uzunligiga bo'linmasa, foydalanuvchi tasdig'i bilan rulon kesiladi va qoldiq bo'lak alohida `WarehouseStock` sifatida omborga qaytadi (`HandleResidueAsync`).
- **Raqam kiritish universal.** Kasr ajratgich sifatida ham nuqta (`.`) ham vergul (`,`) qabul qilinadi — interfeys/OS tili qanday bo'lishidan qat'i nazar (`NumericInput.Normalize`). Buni bitta madaniyatga qulflab qo'yma.
- **To'lovda asosiy valyuta kursi qulf.** To'lov sahifasida tanlangan valyuta `IsDefault=true` bo'lsa, kurs maydoni tahrirlanmaydi.
- **IP-allowlist / so'rov-log middleware YO'Q.** Ilgari `ClientIpMiddleware` (IP oq ro'yxat) bor edi — u ulanishni bloklab, sekinlashtirgani uchun buyurtmachi talabi bilan butunlay olib tashlangan. Qaytarib kiritma.

## Kod uslubi

Izohsiz, sodda, kam kod; mavjud arxitektura va fayl uslubiga sodiq; tayyor ishonchli kutubxonalardan foydalanish.

Upgrade topshirilganda: mavjud ishlayotgan xatti-harakatni buzma. O'zgartirishdan oldin "Biznes qoidalari" bo'limini o'qi; mantiqan g'alati ko'ringan kodni "tuzatishdan" oldin buyurtmachi ataylab so'raganmi — tekshir yoki so'ra.

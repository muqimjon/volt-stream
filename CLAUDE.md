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

## Kod uslubi

Izohsiz, sodda, kam kod; mavjud arxitektura va fayl uslubiga sodiq; tayyor ishonchli kutubxonalardan foydalanish.

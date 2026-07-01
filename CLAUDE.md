# VoltStream

Kabel zavodi uchun hisob-kitob (savdo, ta'minot, to'lovlar, debitor/kreditor, ombor) dasturi.

## Arxitektura

`.NET 9`. Yechim ikki qismdan iborat (`VoltStream.sln`):

**Backend** (`src/backend`) — Clean Architecture + CQRS (MediatR):
- `VoltStream.Domain` — entitilar, enumlar.
- `VoltStream.Application` — CQRS komandalar/so'rovlar, validatsiya, interfeyslar.
- `VoltStream.Infrastructure` — EF Core (PostgreSQL/Npgsql), `AppDbContext`, JWT, Excel seeding (`ExcelDataSeeder`, `SeedData/*.xlsx` orqali), `DatabaseInitializer` (migratsiya + seeding fon `BackgroundService`).
- `VoltStream.WebApi` — controllerlar, middleware, Scalar API hujjati, UDP discovery responder (`SimpleDiscoveryResponder`). Kompozitsiya `WebApiHostBuilder.Build` da; `Program.cs` shuni chaqiradi.

**Frontend** (`src/frontend`) — WPF (MVVM, CommunityToolkit.Mvvm, Mapster):
- `VoltStream.WPF` — oynalar/sahifalar/ViewModel'lar, DI uchun generic `Host`, fon `ConnectionMonitor`.
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

- WPF ishga tushganda darrov login oynasini ko'rsatadi; ulanish va (USB kalit bo'lsa) avto-login fonda bo'ladi.
- `ConnectionMonitor` (har 5s) server bilan aloqani tekshiradi va uzilsa `DiscoveryClient` orqali qayta topadi (saqlangan URL → UDP broadcast + LAN skan).
- Server manzili `config/api-connection.json` ga saqlanadi (`ApiConnectionStore`, faqat `Url`/`AutoReconnectEnabled`/… durable maydonlar).
- Portlar: dev `7285` (https), `5123` (http); native production `5000`. Discovery UDP `5001`.
- `/api/health` HTTPS-redirect'dan ozod va `[AllowAnonymous]` — har ikki sxemada 200 qaytaradi.
- Avto-login: removable USB diskdagi `voltstream.key` (`DevKeyService`). Kalit bo'lmasa — qo'lda login.

## Kod uslubi

Izohsiz, sodda, kam kod; mavjud arxitektura va fayl uslubiga sodiq; tayyor ishonchli kutubxonalardan foydalanish.

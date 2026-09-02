# VoltStream

Kabel zavodi uchun hisob-kitob (savdo, ta'minot, to'lovlar, debitor/kreditor, ombor) dasturi.

## Arxitektura

`.NET 9`. Yechim uch qismdan iborat (`VoltStream.sln` + `src/web`):

**Backend** (`src/backend`) — Clean Architecture + CQRS (MediatR):
- `VoltStream.Domain` — entitilar, enumlar.
- `VoltStream.Application` — CQRS komandalar/so'rovlar, interfeyslar, `Commons/Security` (ruxsatlar), `Commons/Behaviors` (MediatR pipeline: `PermissionBehavior`, `AuditLogBehavior`).
- `VoltStream.Infrastructure` — EF Core (PostgreSQL/Npgsql), `AppDbContext`, JWT, Excel seeding (`ExcelDataSeeder`, `SeedData/*.xlsx` orqali), `DatabaseInitializer` (migratsiya + seeding fon `BackgroundService`).
- `VoltStream.WebApi` — controllerlar, middleware, Scalar API hujjati. Kompozitsiya `WebApiHostBuilder.Build` da; `Program.cs` shuni chaqiradi.

**Frontend** (`src/frontend`) — WPF (MVVM, CommunityToolkit.Mvvm, Mapster):
- `VoltStream.WPF` — oynalar/sahifalar/ViewModel'lar, DI uchun generic `Host`.
- `ApiServices` — Refit klient interfeyslari (`I*Api`) va modellar.

**Web** (`src/web/VoltStream.Web`) — Angular 22 + PWA (planshet/telefon uchun: savdo, kirim, sotilganlar, statistika, loglar, profil). Standalone komponentlar, signal'lar, UI kutubxonasiz. Build natijasi `src/backend/VoltStream.WebApi/wwwroot` ga tushadi (gitignored) va WebApi'ning o'zi SPA sifatida xizmat qiladi (`MapFallbackToFile`, `/api`, `/hubs`, `/scalar` bundan mustasno). `dotnet publish` `BuildWeb` target orqali `npm ci && npm run build` ni ishga tushiradi (`-p:BuildWeb=false` bilan o'tkazib yuboriladi). Dev: `npm start` (proxy `/api`, `/hubs` → `https://localhost:7285`). Node 24 LTS (`.node-version`).

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

`Jwt:Secret` (kamida 32 belgi) MAJBURIY — yo'q bo'lsa server ishga tushmaydi. Dev qiymati `appsettings.Development.json` da (gitignored); production'da `appsettings.Production.json` (gitignored) yoki `Jwt__Secret` env. Kalit o'zgarsa barcha tokenlar bekor bo'ladi (WPF qayta login qiladi).

## Migratsiyalar va ma'lumot backfill

- EF migratsiyalari `src/backend/VoltStream.Infrastructure/Persistence/Migrations` da, gitda saqlanadi. Yaratish (WebApi papkasidan): `ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add <Name> --project ../VoltStream.Infrastructure --startup-project . --output-dir Persistence/Migrations` (`dotnet-ef` 9.0.10 lokal manifestda; 10.x net9 bilan ishlamaydi).
- **Generatsiya fayllariga (migratsiya, snapshot) qo'l bilan tegilmaydi.** Kerakli natija model/konfiguratsiya orqali olinadi: default satr → `HasData`, ustun defaulti → `HasDefaultValue` (keyin olib tashlab ikkinchi migratsiya), shartli `UPDATE` → `DatabaseInitializer.dataMigrations` (migratsiya ID'siga bog'langan SQL, `__DataMigrationsHistory` jadvali orqali bir marta va qayta uriniladigan).

## Filiallar, ruxsatlar, audit

- `Branch` — filial; har filialda aynan bitta `Warehouse` (birga yaratiladi). `Sale`, `Supply`, `Warehouse`, `PrintJob` da `BranchId`; `User.BranchId` null = admin (hammasi). Default filial "Asosiy filial" (`HasData`, Id=1).
- Filial serverda `BranchContext` orqali aniqlanadi: non-admin uchun DOIM tokendagi `branch` claim (klient yuborgan `BranchId` e'tiborsiz); admin (yoki `branches.all`) so'rovda `BranchId?` yuboradi, yubormasa savdo/kirimda default filial, hisobotlarda hamma filial. WPF'da admin header'dagi combo orqali tanlaydi (`BranchContext`, `%APPDATA%\Ovoza\VoltStream\branch.json`).
- Ruxsatlar: katalog `Application/Commons/Security/Permissions.cs` (`sales.*`, `supplies.*`, `settings.*`, `print.request`, `print.agent`, ...). Har foydalanuvchida `User.Permissions` (`text[]`); `UserRole` faqat shablon (`RolePresets`), Admin doim hammasi. `print.agent` va `branches.all` presetda YO'Q — faqat aniq beriladi (filial PC hisobiga `print.agent`). Non-admin foydalanuvchiga filial MAJBURIY (`BranchId` null bo'lsa yaratilmaydi, mavjud bo'lsa 403 "Filial biriktirilmagan"). Faqat Admin admin yaratadi/tahrirlaydi va o'z rolini/ruxsatini o'zgartira olmaydi; oxirgi faol admin o'chirilmaydi.
- Token: 7 kun, lekin har so'rovda `OnTokenValidated` foydalanuvchi `IsActive`ligini tekshiradi (60 s kesh) — o'chirilgan foydalanuvchi bir daqiqa ichida 401 oladi. Ruxsat/filial o'zgarsa qayta login kerak (claim'lar tokenda). Login 10 ta/daqiqa (IP bo'yicha, `X-Forwarded-For` orqali — cloudflared orqasida ham). Server tayyor bo'lmaguncha (`AppReadiness.Ready`) login 503.
- Narx maskasi (`PriceMask`): `sales.view_prices` bo'lmasa DTO'larda narx/summa 0 yoki null, `Stocks`/`Accounts` null, va `FilteringRequest`dagi narx maydonlari bo'yicha filtr/sort hamda `include:*` qiymatlari `Sanitize` bilan olib tashlanadi (oracle hujumi oldini olish). Bunday so'rovlar uchun kerakli include'lar handler'da qattiq yozilgan.
- Filial izolyatsiyasi: `BranchContext.EnsureAccess(branchId)` update/delete/get-by-id handler'larida; `PrintJob` claim/complete/fail faqat o'z filiali va o'z agenti. Server tomonda `[RequirePermission("...")]` command/query record'ida + `PermissionBehavior` (MediatR pipeline). JWT'da `permission` claim'lari, `AuthResponse.permissions`. WPF: `session.Has(key)`, XAML'da `utils:Permission.Require="key"` (yashiradi) / `.Enable`; Sozlamalar tablari shu orqali ko'rinadi (parolli qoplama YO'Q — olib tashlangan). Web: `*appHasPermission`, marshrut guard'lari.
- Yangi ruxsat qo'shsang: `Permissions.cs` + `RolePresets` + WPF `Permissions.<key>` loc kalitlari (4 til) + web i18n.
- `sales.view_prices` bo'lmasa: web/desktop narx maydonlarini ko'rsatmaydi; server savdoda narxni `WarehouseStock`dan oladi va javoblarda narxlarni 0 qiladi (`PriceMask`).
- Audit log: `AuditLogBehavior` har `*Command` ni (eng tashqisini) `AuditLogs` jadvaliga yozadi (`Password*` maydonlari `***`), alohida `AppDbContext` bilan, xatosi command'ni buzmaydi. `X-Client` header (Desktop/Web/Agent) → `Source`. Ko'rish: Sozlamalar → Loglar (`settings.audit`), web'da o'z loglari (`logs.view_own`). `Auditable.CreatedByUserId` interceptor orqali to'ldiriladi.

## Masofaviy chop etish (print-agent)

- Oqim: web/desktop `POST /api/print-jobs {saleId}` → `PrintJob` (Pending) → SignalR `PrintHub` (`/hubs/print`, JWT `access_token` query) `PrintJobCreated` filial agentlariga → agent (WPF `PrintAgentService`) `claim` (409 bo'lsa boshqa agent olgan) → `GET /api/sales/{id}/receipt` → `ReceiptDocumentBuilder` (Og'irlik rejimi) → printer → `complete`/`fail` → `PrintJobStatusChanged` so'rovchiga.
- Bir savdoga Pending/Printing job bo'lsa yangi so'rov 409 (`force` bilan qayta). `User.PrintPolicy` Auto/Ask/Deny: Deny → 403 serverda; Ask → agentda Ha/Yo'q dialog.
- Agent sozlamalari lokal: Sozlamalar → Printer (`settings.printer`), `%APPDATA%\Ovoza\VoltStream\printer.json`; ishlashi uchun `print.agent` ruxsati. Qayta ulanganda `GET /api/print-jobs/pending` bilan o'tkazib yuborilgan joblar chop etiladi.
- Chek (`Receipt*`): og'irlik + miqdor bilan, narxsiz; desktop preview'da `sales.view_prices` bo'lsa Pul/Og'irlik toggle. Og'irlik: `Product.IsWeightTracked` + `WeightPerUnit` (kg/birlik), `SaleItem.Weight` savdo paytida saqlanadi (keyin o'zgarmaydi).

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
- **Narxsiz savdo (web).** `sales.view_prices` bo'lmagan hodim savdo qilsa server narxni ombordagi `WarehouseStock.UnitPrice/DiscountRate` dan oladi va summani o'zi hisoblaydi; ofis keyin SaleEdit'da to'g'rilaydi. Bu "narx 0 bo'lib qoladi" xatosi emas.
- **Narxsiz kirim stock narxini buzmaydi.** `CreateSupplyCommand.UnitPrice` null bo'lsa mavjud stock narxi ustidan yozilmaydi (ilgari 0 yozib yuborardi).
- **Bir savdoga bitta faol print job.** Pending/Printing job bo'lsa yangi so'rov 409 — qog'oz isrofini oldini olish uchun ataylab. `force` faqat ongli qayta chop etish uchun.
- **Chekda narx yo'q (og'irlik + miqdor).** Mahsulot bir necha qo'ldan o'tadi, har qo'l o'z narxini qo'yadi — shuning uchun zavod cheki narxsiz. Desktopda faqat ruxsat bilan Pul rejimi.
- **Katalog umumiy, qoldiq filial bo'yicha.** `Product` filialga bog'lanmaydi; `WarehouseStock` filial omboriga bog'lanadi.

## Kod uslubi

Izohsiz, sodda, kam kod; mavjud arxitektura va fayl uslubiga sodiq; tayyor ishonchli kutubxonalardan foydalanish.

Upgrade topshirilganda: mavjud ishlayotgan xatti-harakatni buzma. O'zgartirishdan oldin "Biznes qoidalari" bo'limini o'qi; mantiqan g'alati ko'ringan kodni "tuzatishdan" oldin buyurtmachi ataylab so'raganmi — tekshir yoki so'ra.

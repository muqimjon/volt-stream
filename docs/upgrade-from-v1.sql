-- VoltStream: mijozning eski bazasini (2 ta migratsiya: InitialMigration + UpdateDeleteBehaviors)
-- yangi sxemaga ko'tarish. AVVAL pg_dump nusxasi olingan holda ishlatiladi:
--   psql -U postgres -d voltstream -v ON_ERROR_STOP=1 -f upgrade-from-v1.sql
-- Ko'chirilgan bazada qayta ishga tushirilsa hech narsa qilmaydi.

SELECT EXISTS (SELECT 1 FROM "__EFMigrationsHistory"
               WHERE "MigrationId" = '20260903213355_InitialMigration') AS ko_chirilgan \gset
\if :ko_chirilgan
\echo 'Baza allaqachon yangi sxemada — hech narsa qilinmadi.'
\q
\endif

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "WarehouseStocks" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Warehouses" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Users" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Supplies" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Sales" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "SaleItems" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Products" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Payments" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Customers" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "CustomerOperations" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Currencies" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Categories" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Cashes" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "AllowedClients" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    ALTER TABLE "Accounts" ADD "CreatedByUserId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902114838_AddCreatedByUserId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902114838_AddCreatedByUserId', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Warehouses" ADD "BranchId" bigint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Users" ADD "BranchId" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Supplies" ADD "BranchId" bigint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Sales" ADD "BranchId" bigint NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    CREATE TABLE "Branches" (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "Name" text NOT NULL,
        "NormalizedName" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "IsDeleted" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" bigint,
        CONSTRAINT "PK_Branches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    INSERT INTO "Branches" ("Id", "CreatedAt", "CreatedByUserId", "IsActive", "IsDeleted", "Name", "NormalizedName", "UpdatedAt")
    VALUES (1, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', NULL, TRUE, FALSE, 'Asosiy filial', 'ASOSIY FILIAL', TIMESTAMPTZ '1970-01-01T00:00:00+00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    CREATE INDEX "IX_Warehouses_BranchId" ON "Warehouses" ("BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    CREATE INDEX "IX_Users_BranchId" ON "Users" ("BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    CREATE INDEX "IX_Supplies_BranchId" ON "Supplies" ("BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    CREATE INDEX "IX_Sales_BranchId" ON "Sales" ("BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Sales" ADD CONSTRAINT "FK_Sales_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Supplies" ADD CONSTRAINT "FK_Supplies_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    ALTER TABLE "Warehouses" ADD CONSTRAINT "FK_Warehouses_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    PERFORM setval(
        pg_get_serial_sequence('"Branches"', 'Id'),
        GREATEST(
            (SELECT MAX("Id") FROM "Branches") + 1,
            nextval(pg_get_serial_sequence('"Branches"', 'Id'))),
        false);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120211_AddBranches') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902120211_AddBranches', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120238_DropBranchIdDefaults') THEN
    ALTER TABLE "Warehouses" ALTER COLUMN "BranchId" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120238_DropBranchIdDefaults') THEN
    ALTER TABLE "Supplies" ALTER COLUMN "BranchId" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120238_DropBranchIdDefaults') THEN
    ALTER TABLE "Sales" ALTER COLUMN "BranchId" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902120238_DropBranchIdDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902120238_DropBranchIdDefaults', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121445_AddUserPermissions') THEN
    ALTER TABLE "Users" ADD "Permissions" text[] NOT NULL DEFAULT ('{}'::text[]);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121445_AddUserPermissions') THEN
    ALTER TABLE "Users" ADD "PrintPolicy" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121445_AddUserPermissions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902121445_AddUserPermissions', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121652_AddProductWeight') THEN
    ALTER TABLE "SaleItems" ADD "Weight" numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121652_AddProductWeight') THEN
    ALTER TABLE "Products" ADD "IsWeightTracked" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121652_AddProductWeight') THEN
    ALTER TABLE "Products" ADD "WeightPerUnit" numeric;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902121652_AddProductWeight') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902121652_AddProductWeight', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144250_AddAuditLogs') THEN
    CREATE TABLE "AuditLogs" (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "Timestamp" timestamp with time zone NOT NULL,
        "UserId" bigint,
        "Username" text,
        "BranchId" bigint,
        "Action" text NOT NULL,
        "EntityName" text,
        "EntityId" bigint,
        "Details" jsonb,
        "Source" integer NOT NULL,
        "Success" boolean NOT NULL,
        "Error" text,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144250_AddAuditLogs') THEN
    CREATE INDEX "IX_AuditLogs_BranchId" ON "AuditLogs" ("BranchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144250_AddAuditLogs') THEN
    CREATE INDEX "IX_AuditLogs_Timestamp" ON "AuditLogs" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144250_AddAuditLogs') THEN
    CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144250_AddAuditLogs') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902144250_AddAuditLogs', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144737_AddPrintJobs') THEN
    CREATE TABLE "PrintJobs" (
        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
        "SaleId" bigint NOT NULL,
        "BranchId" bigint NOT NULL,
        "RequestedByUserId" bigint NOT NULL,
        "Status" integer NOT NULL,
        "AgentUserId" bigint,
        "PrinterName" text,
        "Copies" integer NOT NULL,
        "Attempts" integer NOT NULL,
        "Error" text,
        "ClaimedAt" timestamp with time zone,
        "FinishedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" bigint,
        CONSTRAINT "PK_PrintJobs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PrintJobs_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144737_AddPrintJobs') THEN
    CREATE INDEX "IX_PrintJobs_BranchId_Status" ON "PrintJobs" ("BranchId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144737_AddPrintJobs') THEN
    CREATE INDEX "IX_PrintJobs_SaleId_Status" ON "PrintJobs" ("SaleId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902144737_AddPrintJobs') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902144737_AddPrintJobs', '10.0.11');
    END IF;
END $EF$;
COMMIT;

-- ==== VoltStream: eski migratsiya tarixini yangi InitialMigration ga almashtirish ====
-- ==== va mavjud ma'lumotni to'ldirish (eski DatabaseInitializer.dataMigrations) ====
DO $UP$
BEGIN
    IF EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260903213355_InitialMigration') THEN
        RETURN;
    END IF;

    DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" IN (
        '20260902114838_AddCreatedByUserId','20260902120211_AddBranches','20260902120238_DropBranchIdDefaults',
        '20260902121445_AddUserPermissions','20260902121652_AddProductWeight','20260902144250_AddAuditLogs','20260902144737_AddPrintJobs',
        '20251229052754_InitialMigration','20251230125200_UpdateDeleteBehaviors');
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260903213355_InitialMigration', '10.0.11');

    UPDATE "Users" SET "IsActive" = true;
    UPDATE "Users" SET "BranchId" = (SELECT MIN("Id") FROM "Branches") WHERE "Role" <> 0 AND "BranchId" IS NULL;
    UPDATE "Users" SET "Permissions" = ARRAY['sales.create','sales.view','sales.view_prices','sales.edit','supplies.create','supplies.view','supplies.edit','stock.view','payments.manage','customers.manage','reports.view','settings.currencies','settings.categories','settings.products','settings.customers','settings.branches','settings.users','settings.printer','settings.audit','print.request','print.agent','logs.view_own','branches.all']::text[] WHERE "Role" = 0 AND cardinality("Permissions") = 0;
    UPDATE "Users" SET "Permissions" = ARRAY['sales.create','sales.view','sales.view_prices','sales.edit','supplies.create','supplies.view','supplies.edit','stock.view','payments.manage','customers.manage','reports.view','print.request','logs.view_own']::text[] WHERE "Role" = 1 AND cardinality("Permissions") = 0;
    UPDATE "Users" SET "Permissions" = ARRAY['sales.view','sales.view_prices','payments.manage','customers.manage','stock.view','reports.view','print.request']::text[] WHERE "Role" = 2 AND cardinality("Permissions") = 0;
    UPDATE "Users" SET "Permissions" = ARRAY['sales.create','sales.view','supplies.create','supplies.view','stock.view','customers.manage','print.request']::text[] WHERE "Role" = 3 AND cardinality("Permissions") = 0;
END $UP$;

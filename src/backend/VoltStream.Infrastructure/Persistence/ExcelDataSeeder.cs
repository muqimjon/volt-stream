namespace VoltStream.Infrastructure.Persistence;

using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Currencies.Commands;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.Domain.Enums;

public class ExcelDataSeeder(
    IAppDbContext context,
    IMediator mediator,
    ILogger<ExcelDataSeeder> logger)
{
    public async Task SeedAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning("SeedData papkasi topilmadi: {Path}", folderPath);
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.xlsx");
        foreach (var file in files)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                using var workbook = new XLWorkbook(file);
                var sheet = workbook.Worksheet(1);
                var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
                if (rows == null) continue;
                var headers = sheet.Row(1);

                logger.LogInformation("{File} fayli o'qilmoqda...", fileName);

                foreach (var row in rows)
                {
                    await (fileName.ToLower() switch
                    {
                        "users" => HandleUserSeed(row, headers),
                        "currencies" => HandleCurrencySeed(row, headers),
                        _ => HandleUniversalSeed(fileName, row, headers)
                    });
                }
                await context.SaveAsync(default);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{File} faylini qayta ishlashda xatolik!", file);
            }
        }
    }
    private async Task HandleUserSeed(IXLRangeRow row, IXLRow headers)
    {
        var username = GetValue(row, headers, "Username");
        if (string.IsNullOrEmpty(username)) return;

        try
        {
            if (await context.Users.AnyAsync(u => u.Username == username)) return;

            var command = new CreateUserCommand(
                Username: username,
                Password: GetValue(row, headers, "Password") ?? "123",
                Name: GetValue(row, headers, "Name"),
                Role: Enum.TryParse<UserRole>(GetValue(row, headers, "Role"), out var role) ? role : UserRole.Seller,
                Phone: GetValue(row, headers, "Phone"),
                Email: GetValue(row, headers, "Email"),
                Address: GetValue(row, headers, "Address"),
                DateOfBirth: DateTime.TryParse(GetValue(row, headers, "DateOfBirth"), out var dob) ? dob : null
            );

            await mediator.Send(command);
            logger.LogInformation("Foydalanuvchi muvaffaqiyatli qo'shildi: {Username}", username);
        }
        catch (Exception ex)
        {
            logger.LogError("Foydalanuvchi ({Username}) qo'shishda xatolik: {Message}", username, ex.Message);
        }
    }

    private async Task HandleCurrencySeed(IXLRangeRow row, IXLRow headers)
    {
        var name = GetValue(row, headers, "Name");
        var code = GetValue(row, headers, "Code");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code)) return;
        if (await context.Currencies.AnyAsync(c => c.Code == code)) return;

        try
        {
            var command = new CreateCurrencyCommand(
                Name: name,
                Code: code,
                Symbol: GetValue(row, headers, "Symbol") ?? "",
                IsDefault: bool.TryParse(GetValue(row, headers, "IsDefault"), out var isDef) && isDef,
                IsActive: !bool.TryParse(GetValue(row, headers, "IsActive"), out var isActive) || isActive,
                IsEditable: !bool.TryParse(GetValue(row, headers, "IsEditable"), out var isEd) || isEd,
                Position: int.TryParse(GetValue(row, headers, "Position"), out var pos) ? pos : 0,
                IsCash: bool.TryParse(GetValue(row, headers, "IsCash"), out var isCash) && isCash,
                ExchangeRate: decimal.TryParse(GetValue(row, headers, "ExchangeRate"), out var rate) ? rate : 1.0m
            );

            await mediator.Send(command);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Currency seed qilinmadi: {Name}. Xatolik: {Msg}", name, ex.Message);
        }
    }

    private async Task HandleUniversalSeed(string fileName, IXLRangeRow row, IXLRow headers)
    {
        var dbSetProp = context.GetType().GetProperties()
            .FirstOrDefault(p => p.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        if (dbSetProp == null) return;

        var entityType = dbSetProp.PropertyType.GetGenericArguments()[0];

        var dbSet = dbSetProp.GetValue(context);
        var anyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 1).MakeGenericMethod(entityType);
        if ((bool)anyMethod.Invoke(null, [dbSet])!) return;

        var obj = Activator.CreateInstance(entityType)!;
        foreach (var prop in entityType.GetProperties())
        {
            var cellValue = GetValue(row, headers, prop.Name);
            if (cellValue != null)
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(obj, Convert.ChangeType(cellValue, targetType));
            }
        }
        context.GetType().GetMethod("Add")?.Invoke(context, [obj]);
    }

    private static string? GetValue(IXLRangeRow row, IXLRow headers, string colName)
    {
        var cell = row.Cells().FirstOrDefault(c =>
            headers.Cell(c.Address.ColumnNumber).Value.ToString().Trim().Equals(colName, StringComparison.OrdinalIgnoreCase));
        return cell?.Value.ToString();
    }
}
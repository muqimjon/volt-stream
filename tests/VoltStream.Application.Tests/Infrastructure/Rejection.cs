namespace VoltStream.Application.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using VoltStream.Application.Commons.Exceptions;

// Klientga boradigan HTTP kod bo'yicha tekshiradi (400/403/404/409), aniq istisno turi bo'yicha emas
public static class Rejection
{
    public static async Task With(HttpStatusCode status, Func<Task> action)
    {
        var exception = await Assert.ThrowsAnyAsync<AppException>(action);

        Assert.Equal(status, exception.StatusCode);
    }

    // Bog'langan yozuvni o'chirish 409 beradi: handler ConflictException tashlaydimi yoki FK buzilishi (23503) — middleware ikkisini ham 409 qiladi
    public static async Task Conflicting(Func<Task> action)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(action);

        Assert.True(
            exception is AppException { StatusCode: HttpStatusCode.Conflict }
                or DbUpdateException { InnerException: PostgresException { SqlState: "23503" or "23001" } },
            exception.ToString());
    }
}

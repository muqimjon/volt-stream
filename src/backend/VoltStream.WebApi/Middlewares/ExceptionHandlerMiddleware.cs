namespace VoltStream.WebApi.Middlewares;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Net;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.WebApi.Models;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await HandleExceptionAsync(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Ma'lumotlar bazasi xatosi");
            var (statusCode, message) = MapDatabaseException(ex);
            await HandleExceptionAsync(context, statusCode, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kutilmagan xatolik");
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "Tizimda kutilmagan xatolik yuz berdi.");
        }
    }

    private static (HttpStatusCode code, string message) MapDatabaseException(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState switch
            {
                "23503" or "23001" => (HttpStatusCode.Conflict,
                    "Ushbu ma'lumot boshqa hujjatlarga bog'langanligi sababli uni o'chirish imkonsiz."),

                "23505" => (HttpStatusCode.Conflict,
                    "Bunday ma'lumot tizimda allaqachon mavjud. Iltimos, boshqa qiymat kiriting."),

                "23502" => (HttpStatusCode.BadRequest,
                    "Majburiy maydonlar to'ldirilmagan. Iltimos, barcha ma'lumotlarni tekshiring."),

                "23514" => (HttpStatusCode.BadRequest,
                    "Kiritilgan ma'lumotlar tizim qoidalariga mos kelmadi (Check Constraint)."),

                "22001" => (HttpStatusCode.BadRequest,
                    "Kiritilgan ma'lumot haddan tashqari uzun. Iltimos, qisqaroq matn kiriting."),

                _ => (HttpStatusCode.InternalServerError, "Ma'lumotlar bazasida kutilmagan xatolik yuz berdi.")
            };
        }

        return (HttpStatusCode.InternalServerError, "Ma'lumotni saqlashda xatolik yuz berdi.");
    }

    private static Task HandleExceptionAsync(HttpContext context, HttpStatusCode code, string message)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";

        var response = new Response
        {
            StatusCode = (int)code,
            Message = message
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
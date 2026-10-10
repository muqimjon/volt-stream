namespace VoltStream.Application.Commons.Extensions;

using VoltStream.Application.Commons.Exceptions;

public static class PriceValidation
{
    public static void Ensure(decimal? unitPrice, decimal? discountRate)
    {
        if (unitPrice < 0)
            throw new AppException("Narx manfiy bo'lishi mumkin emas.");

        if (discountRate is < 0 or > 100)
            throw new AppException("Chegirma foizi 0 dan 100 gacha bo'lishi kerak.");
    }
}

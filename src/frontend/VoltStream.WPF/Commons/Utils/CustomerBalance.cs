namespace VoltStream.WPF.Commons.Utils;

// Savdo va Savdoni tahrirlash sahifalari mijoz qarzini aynan shu formula bilan hisoblaydi — sahifalar orasida farq bo'lmasin.
public static class CustomerBalance
{
    public static decimal Current(IEnumerable<decimal> accountBalances)
        => Math.Round(accountBalances.Sum(), 0, MidpointRounding.AwayFromZero);

    public static decimal InBase(decimal amount, decimal exchangeRate)
        => amount * (exchangeRate == 0 ? 1 : exchangeRate);

    public static decimal After(decimal current, decimal saleAmount, decimal exchangeRate)
        => current - InBase(saleAmount, exchangeRate);
}

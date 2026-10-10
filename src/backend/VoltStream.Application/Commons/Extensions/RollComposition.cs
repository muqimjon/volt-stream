namespace VoltStream.Application.Commons.Extensions;

using System.Globalization;

public readonly record struct RollGroup(decimal Length, int Count);

public static class RollComposition
{
    // Kesilgan bo'lak = TotalLength − (RollCount − 1) × LengthPerRoll musbat va to'liq rulondan oshmasligi kerak,
    // aks holda chekdagi tarkib manfiy yoki ortiqcha bo'lak ko'rsatadi. Chegaralar 3 xonada solishtiriladi:
    // klientlar uzunlikni shunday yaxlitlaydi (33,3333 × 2 → 66,667).
    public static bool IsConsistent(int rollCount, decimal lengthPerRoll, decimal totalLength)
    {
        var length = Math.Round(totalLength, 3, MidpointRounding.AwayFromZero);

        return rollCount > 0
            && lengthPerRoll > 0
            && length > Math.Round((rollCount - 1) * lengthPerRoll, 3, MidpointRounding.AwayFromZero)
            && length <= Math.Round(rollCount * lengthPerRoll, 3, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<RollGroup> Of(int rollCount, decimal lengthPerRoll, decimal totalLength)
    {
        if (rollCount <= 0 || lengthPerRoll <= 0)
            return [];

        var perRoll = Math.Round(lengthPerRoll, 3);
        var cut = Math.Round(totalLength - (rollCount - 1) * lengthPerRoll, 3);
        if (cut == perRoll || cut <= 0)
            return [new RollGroup(perRoll, rollCount)];

        List<RollGroup> groups = [];
        if (rollCount > 1)
            groups.Add(new RollGroup(perRoll, rollCount - 1));
        groups.Add(new RollGroup(cut, 1));
        return groups;
    }

    public static IReadOnlyList<RollGroup> Merge(IEnumerable<RollGroup> groups) =>
        [.. groups.GroupBy(g => g.Length).Select(g => new RollGroup(g.Key, g.Sum(x => x.Count)))];

    public static string Format(IEnumerable<RollGroup> groups) =>
        string.Join(" + ", groups.Select(g => $"{g.Count} × {g.Length.ToString("0.###", CultureInfo.InvariantCulture)}"));
}

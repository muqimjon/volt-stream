namespace VoltStream.Application.Commons.Extensions;

using System.Globalization;

public readonly record struct RollGroup(decimal Length, int Count);

public static class RollComposition
{
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

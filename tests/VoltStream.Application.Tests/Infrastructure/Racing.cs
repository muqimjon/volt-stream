namespace VoltStream.Application.Tests.Infrastructure;

public sealed record Outcome(Func<Task> Operation, Exception? Error)
{
    public bool Won => Error is null;
}

public static class Racing
{
    public static Task<Outcome[]> Race(Func<Task> operation, int times)
        => Race(Enumerable.Repeat(operation, times));

    public static Task<Outcome[]> Race(IEnumerable<Func<Task>> operations)
        => Task.WhenAll(operations.Select(async operation =>
        {
            try
            {
                await operation();

                return new Outcome(operation, null);
            }
            catch (Exception ex)
            {
                return new Outcome(operation, ex);
            }
        }));

    public static Outcome[] Lost(this Outcome[] outcomes) => [.. outcomes.Where(outcome => !outcome.Won)];

    public static int Won(this Outcome[] outcomes) => outcomes.Count(outcome => outcome.Won);
}

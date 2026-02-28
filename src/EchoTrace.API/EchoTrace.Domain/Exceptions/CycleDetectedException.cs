namespace EchoTrace.Domain.Exceptions;

public class CycleDetectedException : Exception
{
    public string CyclePath { get; }

    public CycleDetectedException(string cyclePath)
        : base($"Adding this edge would create a circular dependency: {cyclePath}")
    {
        CyclePath = cyclePath;
    }
}

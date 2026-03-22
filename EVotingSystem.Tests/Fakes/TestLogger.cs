using Microsoft.Extensions.Logging;

namespace EVotingSystem.Tests.Fakes;

public sealed class TestLogger<T> : ILogger<T>, IDisposable
{
    public List<string> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => this;

    public void Dispose()
    {
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(formatter(state, exception));
    }
}

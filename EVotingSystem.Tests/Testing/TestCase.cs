namespace EVotingSystem.Tests.Testing;

public sealed class TestCase(string name, Func<Task> runAsync)
{
    public string Name { get; } = name;
    public Func<Task> RunAsync { get; } = runAsync;
}

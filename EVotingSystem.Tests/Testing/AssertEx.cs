namespace EVotingSystem.Tests.Testing;

public static class AssertEx
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null) =>
        True(!condition, message ?? "Expected condition to be false.");

    public static void Equal<T>(T expected, T actual, string? message = null)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message ?? $"Expected '{expected}' but found '{actual}'.");
        }
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null)
        {
            throw new InvalidOperationException(message ?? "Expected value to be non-null.");
        }
    }

    public static T IsType<T>(object? value, string? message = null)
    {
        if (value is not T typed)
        {
            throw new InvalidOperationException(message ?? $"Expected value of type {typeof(T).Name}.");
        }

        return typed;
    }

    public static void Contains(string expectedSubstring, string actual, string? message = null)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message ?? $"Expected '{actual}' to contain '{expectedSubstring}'.");
        }
    }
}

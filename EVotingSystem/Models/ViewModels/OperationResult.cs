namespace EVotingSystem.Models.ViewModels;

public class OperationResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }

    public static OperationResult Success(string message, string? userId = null, string? email = null, string? fullName = null) =>
        new()
        {
            Succeeded = true,
            Message = message,
            UserId = userId,
            Email = email,
            FullName = fullName
        };

    public static OperationResult Failure(string message) => new() { Message = message };
}

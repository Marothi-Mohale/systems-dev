namespace EVotingSystem.Infrastructure.Firestore;

public class FirestoreException : Exception
{
    public FirestoreException(string message)
        : base(message)
    {
    }

    public FirestoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

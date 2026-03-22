using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.Firestore;

public abstract class FirestoreDocumentBase
{
    [JsonPropertyName("id")]
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    [Range(1, int.MaxValue)]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

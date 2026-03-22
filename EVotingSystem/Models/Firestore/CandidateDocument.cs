using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EVotingSystem.Models.Firestore;

public class CandidateDocument : FirestoreDocumentBase
{
    [JsonPropertyName("electionId")]
    [Required]
    [StringLength(64)]
    public string ElectionId { get; set; } = "default-election";

    [JsonPropertyName("name")]
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("party")]
    [Required]
    [StringLength(120)]
    public string Party { get; set; } = string.Empty;

    [JsonPropertyName("slogan")]
    [StringLength(160)]
    public string Slogan { get; set; } = string.Empty;

    [JsonPropertyName("biography")]
    [StringLength(2000)]
    public string Biography { get; set; } = string.Empty;

    [JsonPropertyName("voteCount")]
    [Range(0, int.MaxValue)]
    public int VoteCount { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("displayOrder")]
    [Range(0, 10_000)]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("provinceCode")]
    [StringLength(16)]
    public string? ProvinceCode { get; set; }

    [JsonPropertyName("provinceName")]
    [StringLength(120)]
    public string? ProvinceName { get; set; }
}

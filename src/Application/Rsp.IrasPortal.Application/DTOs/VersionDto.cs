namespace Rsp.Portal.Application.DTOs;

public record VersionDto
{
    public string VersionId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
}
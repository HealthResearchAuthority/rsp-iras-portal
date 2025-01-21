namespace Rsp.IrasPortal.Application.DTOs;

public record QuestionSetDto
{
    public VersionDto Version { get; set; } = null!;
    public IEnumerable<CategoryDto> Categories { get; set; } = [];
    public IEnumerable<QuestionDto> Questions { get; set; } = [];
}
namespace Rsp.IrasPortal.Application.DTOs;

public class NavigationDto
{
    public string PreviousCategory { get; set; } = null!;
    public string PreviousStage { get; set; } = null!;
    public string CurrentCategory { get; set; } = null!;
    public string CurrentStage { get; set; } = null!;
    public string NextCategory { get; set; } = null!;
    public string NextStage { get; set; } = null!;

    public QuestionSectionsResponse? PreviousSection { get; set; }
    public QuestionSectionsResponse? CurrentSection { get; set; }
    public QuestionSectionsResponse? NextSection { get; set; }
}
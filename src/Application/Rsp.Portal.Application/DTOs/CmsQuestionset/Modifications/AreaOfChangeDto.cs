namespace Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;

public class AreaOfChangeDto : AnswerModel
{
    public List<AnswerModel> SpecificAreasOfChange { get; set; } = [];
}
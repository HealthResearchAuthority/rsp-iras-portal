namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class RelatedQuestionsRequest
{
    public List<string> AnswerIds { get; set; } = [];
    public string QuestionId { get; set; } = string.Empty;
}
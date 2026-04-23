namespace Rsp.Portal.Application.DTOs.Responses;

public class ModificationRfiResponseResponse
{
    public Guid ModificationId { get; set; }
    public List<RfiResponsesDTO> RfiResponses { get; set; } = [];
}

public class RfiResponsesDTO
{
    public List<string> InitialResponse { get; set; } = [];
    public List<string> RequestRevisions { get; set; } = [];
    public List<string> ReviseAndAuthorise { get; set; } = [];
    public List<string> ReasonForReviseAndAuthorise { get; set; } = [];
}
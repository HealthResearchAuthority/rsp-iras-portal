namespace Rsp.Portal.Application.DTOs.Responses;

public class ModificationRfiResponseResponse
{
    public Guid ModificationId { get; set; }
    public List<RfiResponsesDTO> RfiResponses { get; set; } = [];
    public bool? IsLastSponsorRequestRevisionsDraft { get; set; }
    public bool? IsLastSponsorReasonForReviseAndAuthoriseDraft { get; set; }
}

public class RfiResponsesDTO
{
    public List<string> InitialResponse { get; set; } = [];
    public List<string> RequestRevisionsByApplicant { get; set; } = [];
    public List<string> RequestRevisionsBySponsor { get; set; } = [];
    public List<string> ReviseAndAuthorise { get; set; } = [];
    public List<string> ReasonForReviseAndAuthorise { get; set; } = [];
}
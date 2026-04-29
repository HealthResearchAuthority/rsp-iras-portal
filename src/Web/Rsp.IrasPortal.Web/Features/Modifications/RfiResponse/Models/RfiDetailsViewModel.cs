using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;

public class RfiDetailsViewModel
{
    public IList<string> RfiReasons { get; set; } = [];
    public List<RfiResponsesDTO> RfiResponses { get; set; } = [];
    public bool? IsLastSponsorRequestRevisionsDraft { get; set; }
    public bool? IsLastSponsorReasonForReviseAndAuthoriseDraft { get; set; }
}
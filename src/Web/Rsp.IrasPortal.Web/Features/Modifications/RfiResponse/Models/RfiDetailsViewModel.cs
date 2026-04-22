using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;

public class RfiDetailsViewModel : ModificationDetailsViewModel
{
    public string? DateSubmitted { get; set; }
    public IList<string> RfiReasons { get; set; } = [];
    public List<RfiResponsesDTO> RfiResponses { get; set; } = [];
}
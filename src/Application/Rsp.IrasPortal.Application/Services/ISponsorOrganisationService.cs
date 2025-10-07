using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.Logging.Interceptors;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
///     ISponsorOrganisationService interface. Marked as IInterceptable to enable
///     the start/end logging for all methods.
/// </summary>
public interface ISponsorOrganisationService : IInterceptable
{
    Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(SponsorOrganisationSearchRequest? searchQuery = null, int pageNumber = 1, int pageSize = 20, string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName), string? sortDirection = SortDirections.Ascending);

   
}
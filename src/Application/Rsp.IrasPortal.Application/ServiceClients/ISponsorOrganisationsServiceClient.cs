using Refit;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

/// <summary>
///     Interface to interact with Applications microservice
/// </summary>
public interface ISponsorOrganisationsServiceClient
{
    /// <summary>
    /// Gets all Sponsor Organisations
    /// </summary>
    [Post("/sponsororganisations/all")]
    public Task<IApiResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(int pageNumber = 1,
        int pageSize = 20, string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName),
        string? sortDirection = SortDirections.Ascending, SponsorOrganisationSearchRequest? searchQuery = null);
}
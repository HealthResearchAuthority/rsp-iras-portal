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

    /// <summary>
    /// Gets a Sponsor Organisation by RTS ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}")]
    Task<IApiResponse<AllSponsorOrganisationsResponse>> GetSponsorOrganisationByRtsId(string rtsId);

    /// <summary>
    /// Creates a Sponsor Organisation
    /// </summary>
    [Post("/sponsororganisations/create")]
    public Task<IApiResponse<SponsorOrganisationDto>> CreateSponsorOrganisation([Body] SponsorOrganisationDto sponsorOrganisationDto);

    [Post("/sponsororganisations/adduser")]
    public Task<IApiResponse<SponsorOrganisationUserDto>> AddUserToSponsorOrganisation([Body] SponsorOrganisationUserDto sponsorOrganisationUserDto);

    /// <summary>
    /// Gets a Sponsor Organisation by RTS ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/{userId}")]
    Task<IApiResponse<SponsorOrganisationUserDto>> GetUserInSponsorOrganisation(string rtsId, Guid userId);

}
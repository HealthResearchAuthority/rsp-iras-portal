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
    /// Gets a Sponsor Organisation user by RTS ID and User ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/user/{userId}")]
    Task<IApiResponse<SponsorOrganisationUserDto>> GetUserInSponsorOrganisation(
        string rtsId,
         Guid userId);

    /// <summary>
    /// Enable a Sponsor Organisation user by RTS ID and User ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/user/{userId}/enable")]
    Task<IApiResponse<SponsorOrganisationUserDto>> EnableUserInSponsorOrganisation(
        string rtsId,
        Guid userId);

    /// <summary>
    /// Disable a Sponsor Organisation user by RTS ID and User ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/user/{userId}/disable")]
    Task<IApiResponse<SponsorOrganisationUserDto>> DisableUserInSponsorOrganisation(
        string rtsId,
        Guid userId);

    /// <summary>
    /// Disable a Sponsor Organisation by RTS ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/enable")]
    Task<IApiResponse<SponsorOrganisationDto>> EnableSponsorOrganisation(string rtsId);

    /// <summary>
    /// Disable a Sponsor Organisation by RTS ID
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/disable")]
    Task<IApiResponse<SponsorOrganisationDto>> DisableSponsorOrganisation(string rtsId);

    /// <summary>
    /// Gets review bodies by Id
    /// </summary>
    [Get("/sponsororganisations/{rtsId}/audittrail")]
    public Task<IApiResponse<SponsorOrganisationAuditTrailResponse>> GetSponsorOrganisationAuditTrail(string rtsId, int pageNumber, int pageSize, string sortField, string sortDirection);
}
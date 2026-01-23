using Rsp.Logging.Interceptors;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

/// <summary>
///     ISponsorOrganisationService interface. Marked as IInterceptable to enable
///     the start/end logging for all methods.
/// </summary>
public interface ISponsorOrganisationService : IInterceptable
{
    Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(
        SponsorOrganisationSearchRequest? searchQuery = null, int pageNumber = 1, int pageSize = 20,
        string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName),
        string? sortDirection = SortDirections.Ascending);

    Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetSponsorOrganisationByRtsId(string rtsId);

    Task<ServiceResponse<SponsorOrganisationDto>> CreateSponsorOrganisation(
        SponsorOrganisationDto sponsorOrganisationDto);

    Task<ServiceResponse<SponsorOrganisationUserDto>> AddUserToSponsorOrganisation(SponsorOrganisationUserDto sponsorOrganisationUserDto);

    Task<ServiceResponse<SponsorOrganisationUserDto>> GetUserInSponsorOrganisation(string rtsId, Guid userId);

    Task<ServiceResponse<SponsorOrganisationUserDto>> EnableUserInSponsorOrganisation(string rtsId, Guid userId);

    Task<ServiceResponse<SponsorOrganisationUserDto>> DisableUserInSponsorOrganisation(string rtsId, Guid userId);

    Task<ServiceResponse<SponsorOrganisationDto>> DisableSponsorOrganisation(string rtsId);

    Task<ServiceResponse<SponsorOrganisationDto>> EnableSponsorOrganisation(string rtsId);

    Task<ServiceResponse<SponsorOrganisationAuditTrailResponse>> SponsorOrganisationAuditTrail(string rtsId, int pageNumber, int pageSize, string sortField, string sortDirection);

    Task<ServiceResponse<IEnumerable<SponsorOrganisationDto>>> GetAllActiveSponsorOrganisationsForEnabledUser(Guid userId);

    Task<ServiceResponse<SponsorOrganisationUserDto>> UpdateSponsorOrganisationUser(SponsorOrganisationUserDto user);

    Task<ServiceResponse<SponsorOrganisationUserDto>> GetSponsorOrganisationUser(Guid sponsorOrgUserId);
}
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
}
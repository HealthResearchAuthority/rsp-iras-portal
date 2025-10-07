using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class SponsorOrganisationService(ISponsorOrganisationsServiceClient client) : ISponsorOrganisationService
{
    public async Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(SponsorOrganisationSearchRequest? searchQuery = null, int pageNumber = 1,
        int pageSize = 20, string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName), string? sortDirection = SortDirections.Ascending)
    {
        var apiResponse = await client.GetAllSponsorOrganisations(pageNumber, pageSize, sortField, sortDirection, searchQuery);

        return apiResponse.ToServiceResponse();
    }
}
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class SponsorOrganisationService(ISponsorOrganisationsServiceClient client, IRtsService rtsService)
    : ISponsorOrganisationService
{
    public async Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(
        SponsorOrganisationSearchRequest? searchQuery = null, int pageNumber = 1,
        int pageSize = 20, string? sortField = "name",
        string? sortDirection = "asc")
    {
        if (searchQuery?.SearchQuery != null)
        {
            var rtsNameSearch =
                await rtsService.GetOrganisationsByName(searchQuery.SearchQuery, null, 1, int.MaxValue);

            if (rtsNameSearch.IsSuccessStatusCode)
            {
                searchQuery.RtsIds = rtsNameSearch.Content.Organisations
                    .Select(x => x.Id.ToString())
                    .ToList();
            }
        }

        var apiResponse =
            await client.GetAllSponsorOrganisations(pageNumber, pageSize, sortField, sortDirection, searchQuery);

        if (apiResponse.IsSuccessStatusCode && apiResponse.Content.SponsorOrganisations.Any())
        {
            foreach (var sponsorOrganisation in apiResponse.Content.SponsorOrganisations)
            {
                var organisation = await rtsService.GetOrganisation(sponsorOrganisation.RtsId);

                if (organisation.IsSuccessStatusCode)
                {
                    sponsorOrganisation.SponsorOrganisationName = organisation.Content.Name;
                    sponsorOrganisation.Countries = [organisation.Content.CountryName];
                }
            }
        }


        return apiResponse.ToServiceResponse();
    }
}
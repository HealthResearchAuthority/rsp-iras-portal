using System.Net;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class SponsorOrganisationService(ISponsorOrganisationsServiceClient client, IRtsService rtsService)
    : ISponsorOrganisationService
{
    public async Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetAllSponsorOrganisations(
        SponsorOrganisationSearchRequest? searchQuery = null, int pageNumber = 1,
        int pageSize = 20, string? sortField = nameof(SponsorOrganisationDto.SponsorOrganisationName),
        string? sortDirection = SortDirections.Ascending)
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

            if (searchQuery?.RtsIds.Count == 0)
            {
                return new ServiceResponse<AllSponsorOrganisationsResponse>
                {
                    Content = new AllSponsorOrganisationsResponse
                    {
                        SponsorOrganisations = new List<SponsorOrganisationDto>()
                    },
                    StatusCode = HttpStatusCode.OK
                };
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

    public async Task<ServiceResponse<AllSponsorOrganisationsResponse>> GetSponsorOrganisationByRtsId(string rtsId)
    {
        var apiResponse = await client.GetSponsorOrganisationByRtsId(rtsId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationDto>> CreateSponsorOrganisation(
        SponsorOrganisationDto sponsorOrganisationDto)
    {
        var apiResponse = await client.CreateSponsorOrganisation(sponsorOrganisationDto);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationUserDto>> AddUserToSponsorOrganisation(
        SponsorOrganisationUserDto sponsorOrganisationUserDto)
    {
        var apiResponse = await client.AddUserToSponsorOrganisation(sponsorOrganisationUserDto);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationUserDto>> GetUserInSponsorOrganisation(string rtsId,
        Guid userId)
    {
        var apiResponse = await client.GetUserInSponsorOrganisation(rtsId, userId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationUserDto>> EnableUserInSponsorOrganisation(string rtsId,
        Guid userId)
    {
        var apiResponse = await client.EnableUserInSponsorOrganisation(rtsId, userId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationUserDto>> DisableUserInSponsorOrganisation(string rtsId,
        Guid userId)
    {
        var apiResponse = await client.DisableUserInSponsorOrganisation(rtsId, userId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationDto>> DisableSponsorOrganisation(string rtsId)
    {
        var apiResponse = await client.DisableSponsorOrganisation(rtsId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<SponsorOrganisationDto>> EnableSponsorOrganisation(string rtsId)
    {
        var apiResponse = await client.DisableSponsorOrganisation(rtsId);
        return apiResponse.ToServiceResponse();
    }
}
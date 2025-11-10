using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModificationsBySponsorOrganisationUserIdTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Should_Return_Success_Response_When_Client_Returns_Success(
        Guid sponsorOrganisationUserId,
        SponsorAuthorisationsSearchRequest searchQuery,
        GetModificationsResponse modificationsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<GetModificationsResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            modificationsResponse,
            new());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModificationsBySponsorOrganisationUserId(
                sponsorOrganisationUserId,
                searchQuery,
                1,
                20,
                nameof(ModificationsDto.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationsBySponsorOrganisationUserId(sponsorOrganisationUserId, searchQuery);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GetModificationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(modificationsResponse);

        // Verify
        projectModificationsServiceClient.Verify(c => c.GetModificationsBySponsorOrganisationUserId(
            sponsorOrganisationUserId,
            searchQuery,
            1,
            20,
            nameof(ModificationsDto.SentToSponsorDate),
            SortDirections.Descending), Times.Once());
    }
}
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModificationsBySponsorOrganisationUserIdTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Should_Return_Success_Response_When_Client_Returns_Success(
        Guid sponsorOrganisationUserId,
        SponsorAuthorisationsModificationsSearchRequest searchQuery,
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
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectClosuresServiceTests
{
    public class GetProjectClosuresBySponsorOrganisationUserIdTests
        : TestServiceBase<ProjectClosuresService>
    {
        [Theory, AutoData]
        public async Task Should_Return_Success_Response_When_Client_Returns_Success(
            Guid sponsorOrganisationUserId,
            string rtsId,
            ProjectClosuresSearchRequest searchQuery,
            ProjectClosuresSearchResponse closuresResponse)
        {
            // Arrange
            var apiResponse = new ApiResponse<ProjectClosuresSearchResponse>(
                new HttpResponseMessage(HttpStatusCode.OK),
                closuresResponse,
                new());

            var clientMock = Mocker.GetMock<IProjectClosuresServiceClient>();

            clientMock
                .Setup(c => c.GetProjectClosuresBySponsorOrganisationUserId(
                    sponsorOrganisationUserId,
                    searchQuery,
                    rtsId,
                    1,
                    20,
                    nameof(ProjectClosuresDto.SentToSponsorDate),
                    SortDirections.Descending))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await Sut.GetProjectClosuresBySponsorOrganisationUserId(
                sponsorOrganisationUserId,
                searchQuery, rtsId);

            // Assert
            result.ShouldBeOfType<ServiceResponse<ProjectClosuresSearchResponse>>();
            result.IsSuccessStatusCode.ShouldBeTrue();
            result.StatusCode.ShouldBe(HttpStatusCode.OK);
            result.Content.ShouldBe(closuresResponse);

            // Verify
            clientMock.Verify(c => c.GetProjectClosuresBySponsorOrganisationUserId(
                sponsorOrganisationUserId,
                searchQuery,
                rtsId,
                1,
                20,
                nameof(ProjectClosuresDto.SentToSponsorDate),
                SortDirections.Descending), Times.Once);
        }
    }
}
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectClosuresServiceTests
{
    public class GetProjectClosuresBySponsorOrganisationUserIdWithoutPagingTests
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
                .Setup(c => c.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                    sponsorOrganisationUserId,
                    rtsId,
                    searchQuery))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await Sut.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                sponsorOrganisationUserId,
                searchQuery,
                rtsId);

            // Assert
            result.ShouldBeOfType<ServiceResponse<ProjectClosuresSearchResponse>>();
            result.IsSuccessStatusCode.ShouldBeTrue();
            result.StatusCode.ShouldBe(HttpStatusCode.OK);
            result.Content.ShouldBe(closuresResponse);

            // Verify
            clientMock.Verify(c => c.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                sponsorOrganisationUserId, rtsId, searchQuery), Times.Once);
        }
    }
}
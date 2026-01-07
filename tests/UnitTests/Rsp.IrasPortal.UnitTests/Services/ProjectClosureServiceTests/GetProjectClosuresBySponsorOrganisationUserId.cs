using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectClosuresServiceTests
{
    public class GetProjectClosuresBySponsorOrganisationUserIdTests
        : TestServiceBase<ProjectClosuresService>
    {
        [Theory, AutoData]
        public async Task Should_Return_Success_Response_When_Client_Returns_Success(
            Guid sponsorOrganisationUserId,
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
                    1,
                    20,
                    nameof(ProjectClosuresDto.SentToSponsorDate),
                    SortDirections.Descending))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await Sut.GetProjectClosuresBySponsorOrganisationUserId(
                sponsorOrganisationUserId,
                searchQuery);

            // Assert
            result.ShouldBeOfType<ServiceResponse<ProjectClosuresSearchResponse>>();
            result.IsSuccessStatusCode.ShouldBeTrue();
            result.StatusCode.ShouldBe(HttpStatusCode.OK);
            result.Content.ShouldBe(closuresResponse);

            // Verify
            clientMock.Verify(c => c.GetProjectClosuresBySponsorOrganisationUserId(
                sponsorOrganisationUserId,
                searchQuery,
                1,
                20,
                nameof(ProjectClosuresDto.SentToSponsorDate),
                SortDirections.Descending), Times.Once);
        }
    }
}
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationsForProject : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Paged_Response(string projectRecordId)
    {
        // Arrange
        var request = new ModificationSearchRequest();
        var response = new GetModificationsResponse
        {
            Modifications = [new() { Id = Guid.NewGuid().ToString(), ModificationId = "MID" }],
            TotalCount = 1
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationsForProject(projectRecordId, request, 1, 20, nameof(ModificationsDto.ModificationId), SortDirections.Descending))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModificationsForProject(projectRecordId, request, 1, 20, nameof(ModificationsDto.ModificationId), SortDirections.Descending);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}
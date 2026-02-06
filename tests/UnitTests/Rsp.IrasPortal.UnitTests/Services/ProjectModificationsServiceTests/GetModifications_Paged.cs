using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModifications_Paged : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Paged_Response()
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
            .Setup(c => c.GetModifications(request, 1, 20, nameof(ModificationsDto.ModificationId), SortDirections.Descending))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModifications(request, 1, 20, nameof(ModificationsDto.ModificationId), SortDirections.Descending);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.TotalCount.ShouldBe(1);
    }
}
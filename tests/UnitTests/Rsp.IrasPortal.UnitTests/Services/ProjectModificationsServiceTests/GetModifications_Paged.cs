using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModifications_Paged : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Paged_Response()
    {
        // Arrange
        var request = new ModificationSearchRequest();
        var response = new Application.DTOs.Responses.GetModificationsResponse
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
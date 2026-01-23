using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class CreateModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Created_Change(ProjectModificationChangeRequest request, ProjectModificationChangeResponse response)
    {
        // Arrange
        response.Status = ModificationStatus.InDraft;

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.CreateModificationChange(request))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.CreateModificationChange(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Status.ShouldBe(ModificationStatus.InDraft);
    }
}
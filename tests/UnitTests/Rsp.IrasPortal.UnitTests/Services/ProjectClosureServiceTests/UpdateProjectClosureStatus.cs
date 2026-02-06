using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectClosuresServiceTests
{
    public class UpdateProjectClosureStatusTests : TestServiceBase<ProjectClosuresService>
    {
        [Fact]
        public async Task Returns_Success_On_200()
        {
            // Arrange
            var apiResponse = ApiResponseFactory.Success();

            Mocker
                .GetMock<IProjectClosuresServiceClient>()
                .Setup(c => c.UpdateProjectClosureStatus(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await Sut.UpdateProjectClosureStatus("PR-1", "Authorised");

            // Assert
            result.IsSuccessStatusCode.ShouldBeTrue();
        }
    }
}
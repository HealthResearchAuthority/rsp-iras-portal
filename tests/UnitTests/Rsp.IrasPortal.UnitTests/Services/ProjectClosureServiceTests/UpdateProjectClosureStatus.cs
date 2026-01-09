using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectClosuresServiceTests
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
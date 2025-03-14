using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.QuestionSetServiceTests;

public class PublishVersionTests : TestServiceBase<QuestionSetService>
{
    [Theory, AutoData]
    public async Task PublishVersion_Should_Return_Success_Response_When_Client_Returns_Success(string versionId)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);

        Mocker
            .GetMock<IQuestionSetServiceClient>()
            .Setup(client => client.PublishVersion(It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.PublishVersion(versionId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task PublishVersion_Should_Return_Error_Response_When_Client_Returns_Error(string versionId)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.BadRequest);

        Mocker
            .GetMock<IQuestionSetServiceClient>()
            .Setup(client => client.PublishVersion(It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.PublishVersion(versionId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
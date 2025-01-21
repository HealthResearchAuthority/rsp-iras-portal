using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.QuestionSetServiceTests;

public class GetVersionTests : TestServiceBase<QuestionSetService>
{
    private readonly Mock<IQuestionSetServiceClient> _questionSetServiceClient;

    public GetVersionTests()
    {
        _questionSetServiceClient = Mocker.GetMock<IQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetVersions_Should_Return_Success_Response_When_Client_Returns_Success(List<VersionDto> versions)
    {
        var apiResponse = new ApiResponse<IEnumerable<VersionDto>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            versions,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetVersions())
            .ReturnsAsync(apiResponse);

        var result = await Sut.GetVersions();

        result.ShouldNotBeNull();
        result.Content.ShouldBe(versions);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetVersions_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        var apiResponse = new ApiResponse<IEnumerable<VersionDto>>
        (
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            null,
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetVersions())
            .ReturnsAsync(apiResponse);

        var result = await Sut.GetVersions();

        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetModificationNextQuestionSectionTests : TestServiceBase<CmsQuestionsetService>
{
    [Fact]
    public async Task GetModificationNextQuestionSection_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = new ApiResponse<QuestionSectionsResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new QuestionSectionsResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationNextQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationNextQuestionSection(It.IsAny<string>());

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetModificationNextQuestionSection_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationNextQuestionSection(It.IsAny<string>()))
            .ThrowsAsync(new Exception("API failure"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => Sut.GetModificationNextQuestionSection(It.IsAny<string>()));
    }

    [Fact]
    public async Task GetModificationPreviousQuestionSection_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = new ApiResponse<QuestionSectionsResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new QuestionSectionsResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationPreviousQuestionSection(It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationPreviousQuestionSection(It.IsAny<string>());

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetModificationPreviousQuestionSection_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationPreviousQuestionSection(It.IsAny<string>()))
            .ThrowsAsync(new Exception("API failure"));

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => Sut.GetModificationPreviousQuestionSection(It.IsAny<string>()));
    }

    [Fact]
    public async Task GetModificationQuestionSet_ShouldReturnSuccess_WhenApiReturnsOk()
    {
        // Arrange
        var apiResponse = new ApiResponse<CmsQuestionSetResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new CmsQuestionSetResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(c => c.GetModificationQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationQuestionSet(It.IsAny<string>(), It.IsAny<string>());

        // Assert
        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsQuestionsetServiceTests;

public class GetQuestionCategoriesTests : TestServiceBase<CmsQuestionsetService>
{
    private readonly Mock<ICmsQuestionSetServiceClient> _questionSetServiceClient;

    public GetQuestionCategoriesTests()
    {
        _questionSetServiceClient = Mocker.GetMock<ICmsQuestionSetServiceClient>();
    }

    [Theory, AutoData]
    public async Task GetQuestionCategories_ShouldReturnSuccess_WhenApiReturnsOk(
        IEnumerable<CategoryDto> expectedResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<CategoryDto>>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new List<CategoryDto>(),
            new()
        );

        _questionSetServiceClient
            .Setup(c => c.GetQuestionCategories(false))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetQuestionCategories(false);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQuestionCategories_ShouldThrow_WhenApiFails()
    {
        _questionSetServiceClient
            .Setup(c => c.GetQuestionCategories(false))
            .ThrowsAsync(new Exception("Error fetching categories"));

        await Should.ThrowAsync<Exception>(() => Sut.GetQuestionCategories(false));
    }
}
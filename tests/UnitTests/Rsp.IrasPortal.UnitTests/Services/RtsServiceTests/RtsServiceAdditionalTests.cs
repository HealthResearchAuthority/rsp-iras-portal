using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.RtsServiceTests;

public class RtsServiceAdditionalTests : TestServiceBase<RtsService>
{
    [Fact]
    public async Task SearchOrganisations_DelegatesToClient_AndReturnsMappedResult()
    {
        // Arrange
        var request = new OrganisationsSearchRequest { SearchNameTerm = "org" };
        var apiResponse = new ApiResponse<OrganisationSearchResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            new OrganisationSearchResponse
            {
                Organisations = [new() { Id = "1", Name = "Org 1" }],
                TotalCount = 1
            },
            new());

        Mocker.GetMock<IRtsServiceClient>()
            .Setup(c => c.SearchOrganisations(request, 1, 10, "asc", "name"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SearchOrganisations(request, 1, 10, "asc", "name");

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.TotalCount.ShouldBe(1);
        Mocker.GetMock<IRtsServiceClient>()
            .Verify(c => c.SearchOrganisations(request, 1, 10, "asc", "name"), Times.Once);
    }

    [Fact]
    public async Task GetOrganisationsByName_SendsLowercaseName_ToClient()
    {
        // Arrange
        string? capturedName = null;

        Mocker.GetMock<IRtsServiceClient>()
            .Setup(c => c.GetOrganisationsByName(It.IsAny<string>(), null, 1, 10, null, "asc", "name"))
            .Callback<string, string?, int, int?, IEnumerable<string>?, string, string>((name, _, _, _, _, _, _) => capturedName = name)
            .ReturnsAsync(new ApiResponse<OrganisationSearchResponse>(
                new HttpResponseMessage(HttpStatusCode.OK),
                new OrganisationSearchResponse(),
                new()));

        // Act
        await Sut.GetOrganisationsByName("My ORG", null, 1, 10, null, "asc", "name");

        // Assert
        capturedName.ShouldBe("my org");
    }
}
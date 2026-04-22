using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationAuditTrail : TestServiceBase<ProjectModificationsService>
{
    [Theory]
    [AutoData]
    public async Task GetModificationAuditTrail_ShouldReturnAuditTrailResponse_When_Ok_Response_And_No_Items(
        Guid modificationId)
    {
        // Arrange
        var expectedResponse = new ProjectModificationAuditTrailResponse
        {
            Items = null
        };

        Mocker.GetMock<IProjectModificationsServiceClient>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(expectedResponse);
    }

    [Theory]
    [AutoData]
    public async Task
        GetModificationAuditTrail_ShouldReturnAuditTrailResponse_When_Ok_Response_And_Description_Has_No_Tokens(
            Guid modificationId,
            ProjectModificationAuditTrailDto item)
    {
        // Arrange
        item = item with { Description = "No organisation tokens here" };

        var expectedResponse = new ProjectModificationAuditTrailResponse
        {
            Items = new List<ProjectModificationAuditTrailDto> { item }
        };

        Mocker.GetMock<IProjectModificationsServiceClient>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content.Items.ShouldHaveSingleItem();
        result.Content.Items.ToList()[0].Description.ShouldBe("No organisation tokens here");

        Mocker.GetMock<IRtsService>()
            .Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task GetModificationAuditTrail_ShouldReplace_Organisation_Tokens_With_Names_When_Found(
        Guid modificationId,
        ProjectModificationAuditTrailDto item)
    {
        // Arrange
        item = item with { Description = "Added organisations [[RTS:123,456]] to the modification" };

        var expectedResponse = new ProjectModificationAuditTrailResponse
        {
            Items = new List<ProjectModificationAuditTrailDto> { item }
        };

        Mocker.GetMock<IProjectModificationsServiceClient>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        Mocker.GetMock<IRtsService>()
            .Setup(x => x.GetOrganisation("123"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto
                {
                    Name = "Org One"
                }
            });

        Mocker.GetMock<IRtsService>()
            .Setup(x => x.GetOrganisation("456"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto
                {
                    Name = "Org Two"
                }
            });

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content.Items.ShouldHaveSingleItem();
        result.Content.Items.ToList()[0].Description
            .ShouldBe("Added organisations Org One, Org Two to the modification");
    }

    [Theory]
    [AutoData]
    public async Task GetModificationAuditTrail_ShouldFallback_To_Id_When_Organisation_Lookup_Fails(
        Guid modificationId,
        ProjectModificationAuditTrailDto item)
    {
        // Arrange
        item = item with { Description = "Added organisations [[RTS:123,456]] to the modification" };

        var expectedResponse = new ProjectModificationAuditTrailResponse
        {
            Items = new List<ProjectModificationAuditTrailDto> { item }
        };

        Mocker.GetMock<IProjectModificationsServiceClient>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        Mocker.GetMock<IRtsService>()
            .Setup(x => x.GetOrganisation("123"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto
                {
                    Name = "Org One"
                }
            });

        Mocker.GetMock<IRtsService>()
            .Setup(x => x.GetOrganisation("456"))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content.Items.ShouldHaveSingleItem();
        result.Content.Items.ToList()[0].Description.ShouldBe("Added organisations Org One, 456 to the modification");
    }

    [Theory]
    [AutoData]
    public async Task GetModificationAuditTrail_ShouldLeave_Empty_Description_Unchanged(
        Guid modificationId,
        ProjectModificationAuditTrailDto item)
    {
        // Arrange
        item = item with { Description = string.Empty };

        var expectedResponse = new ProjectModificationAuditTrailResponse
        {
            Items = new List<ProjectModificationAuditTrailDto> { item }
        };

        Mocker.GetMock<IProjectModificationsServiceClient>()
            .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content.Items.ShouldHaveSingleItem();
        result.Content.Items.ToList()[0].Description.ShouldBe(string.Empty);

        Mocker.GetMock<IRtsService>()
            .Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.Never);
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class CheckSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Mock<ITempDataProvider> _mockTempDataProvider;

    public CheckSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        _mockTempDataProvider = new Mock<ITempDataProvider>();

        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(_http, _mockTempDataProvider.Object);
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithEmptyModel()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel();

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = null;

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SetupSponsorOrganisation");

        var returnedModel = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSetupViewModel>();
        returnedModel.ShouldBe(model);

        // The controller clears this TempData key
        Sut.TempData[TempDataKeys.ShowNoResultsFound].ShouldBeNull();
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithEmptyModel_RtsError()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = null;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithModalState()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "te"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = null;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithEmptyModel_RtsSuccessNoResults()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = true;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SetupSponsorOrganisation");

        var returnedModel = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSetupViewModel>();
        returnedModel.ShouldBe(model);

        // The controller clears this TempData key
        Sut.TempData[TempDataKeys.ShowNoResultsFound].ShouldBe(true);
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithModel_RtsSuccessResults()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = true;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = new List<OrganisationDto>
                    {
                        new()
                        {
                            Id = "123",
                            Name = "test",
                        }
                    }
                }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new AllSponsorOrganisationsResponse
                {
                    SponsorOrganisations = new List<SponsorOrganisationDto>
                    {
                        new()
                        {
                            SponsorOrganisationName = "test",
                            RtsId = "123"
                        }
                    },
                    TotalCount = 1
                }
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SetupSponsorOrganisation");

        var returnedModel = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSetupViewModel>();
        returnedModel.ShouldBe(model);
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnConfirmView_WithEmptyModel_RtsSuccessResults()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = true;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = new List<OrganisationDto>
                    {
                        new()
                        {
                            Id = "123",
                            Name = "test",
                        }
                    }
                }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new AllSponsorOrganisationsResponse
                {
                    SponsorOrganisations = new List<SponsorOrganisationDto>
                    {
                        new()
                  
                    },
                    TotalCount = 0
                }
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();

 
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithEmptyModel_RtsSuccessNoResultsOrg()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = true;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = new List<OrganisationDto>
                    {
                        new()
                        {
                            Id = "123"
                        }
                    }
                }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new AllSponsorOrganisationsResponse
                {
                    SponsorOrganisations = new List<SponsorOrganisationDto>(),
                    TotalCount = 0
                }
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CheckSponsorOrganisation_ShouldReturnSetupView_WithEmptyModel_RtsSuccessNoResultsOrgError()
    {
        // Arrange
        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrganisation = "test"
        };

        // Set an initial TempData value (simulates what controller clears)
        Sut.TempData[TempDataKeys.ShowNoResultsFound] = true;


        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = new List<OrganisationDto>
                    {
                        new()
                        {
                            Id = "123",
                            Name = "test",
                        }
                    }
                }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.CheckSponsorOrganisation(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }
}
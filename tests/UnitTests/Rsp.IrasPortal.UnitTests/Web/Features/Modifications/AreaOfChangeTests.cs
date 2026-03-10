using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications;

public class AreaOfChangeTests : TestServiceBase<ModificationsController>
{
    [Theory, AutoData]
    public async Task AreaOfChange_RedirectsToAreaOfChange_WhenSuccessful
    (
        string projectRecordId,
        int irasId,
        Guid projectModificationId,
        IrasApplicationResponse irasApplicationResponse
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Set all required TempData keys with correct namespacing and types
        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        var modificationResponse = new List<GetAreaOfChangesResponse>
        {
            new() {
                Id = "1",
                Name = "Test Area of Change",
                ModificationSpecificAreaOfChanges =
                [
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = "1",
                        Name = "Specific Area 1",
                        JourneyType = "specific area 1",
                        ModificationAreaOfChangeId = 1
                    },
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = "2",
                        Name = "Specific Area 2",
                        JourneyType = "specific area 2",
                        ModificationAreaOfChangeId = 1
                    }
                ]
            }
        };

        tempData[TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(modificationResponse);

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = irasApplicationResponse });

        // Act
        var result = await Sut.AreaOfChange(projectModificationId, projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }

    [Theory, AutoData]
    public async Task AreaOfChange_RedirectsToAreaOfChange_WhenContentIsNullOrEmpty
   (
       string projectRecordId,
       int irasId,
       Guid projectModificationId
   )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Set all required TempData keys with correct namespacing and types
        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = null });

        // Act
        var result = await Sut.AreaOfChange(projectModificationId, string.Empty);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }

    [Theory, AutoData]
    public async Task AreaOfChange_RemovesFreeTextOption_WhenParticipatingOrganisationsEnabled
    (
        string projectRecordId,
        int irasId,
        Guid projectModificationId
    )
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();

        Sut.TempData = tempData;

        var respondent = new { GivenName = "John", FamilyName = "Doe" };
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var startingQuestions = new StartingQuestionsDto
        {
            AreasOfChange =
            [
                new AreaOfChangeDto { AutoGeneratedId = AreasOfChange.ParticipatingOrgsWithFreeText, OptionName = "Free Text" },
                new AreaOfChangeDto { AutoGeneratedId = AreasOfChange.ParticipatingOrgsWithSearch, OptionName = "Search" }
            ]
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = startingQuestions
            });

        Mocker
            .GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.ParticipatingOrganisations))
            .ReturnsAsync(true);
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = null });

        var result = await Sut.AreaOfChange(projectModificationId, string.Empty);

        result.ShouldBeOfType<ViewResult>();
        var storedAreas = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(
            Sut.TempData[TempDataKeys.ProjectModification.AreaOfChanges] as string ?? string.Empty);

        storedAreas.ShouldNotBeNull();
        storedAreas.Any(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithFreeText).ShouldBeFalse();
        storedAreas.Any(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithSearch).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task AreaOfChange_RemovesSearchOption_WhenParticipatingOrganisationsDisabled
    (
        string projectRecordId,
        int irasId,
        Guid projectModificationId
    )
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();

        Sut.TempData = tempData;

        var respondent = new { GivenName = "John", FamilyName = "Doe" };
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var startingQuestions = new StartingQuestionsDto
        {
            AreasOfChange =
            [
                new AreaOfChangeDto { AutoGeneratedId = AreasOfChange.ParticipatingOrgsWithFreeText, OptionName = "Free Text" },
                new AreaOfChangeDto { AutoGeneratedId = AreasOfChange.ParticipatingOrgsWithSearch, OptionName = "Search" }
            ]
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = startingQuestions
            });

        Mocker
            .GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.ParticipatingOrganisations))
            .ReturnsAsync(false);
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = null });

        var result = await Sut.AreaOfChange(projectModificationId, string.Empty);

        result.ShouldBeOfType<ViewResult>();
        var storedAreas = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(
            Sut.TempData[TempDataKeys.ProjectModification.AreaOfChanges] as string ?? string.Empty);

        storedAreas.ShouldNotBeNull();
        storedAreas.Any(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithSearch).ShouldBeFalse();
        storedAreas.Any(a => a.AutoGeneratedId == AreasOfChange.ParticipatingOrgsWithFreeText).ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task AreaOfChange_StopOrRestart_WhenSuccessful
    (
        string projectRecordId,
        int irasId,
        Guid projectModificationId
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Set all required TempData keys with correct namespacing and types
        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();

        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        var modificationResponse = new List<GetAreaOfChangesResponse>
        {
            new() {
                Id = "1",
                Name = "Stop or restart",
                ModificationSpecificAreaOfChanges =
                [
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = "1",
                        Name = "Temporary halt to a project",
                        JourneyType = "Temporary halt to a project",
                        ModificationAreaOfChangeId = 1
                    },
                    new ModificationSpecificAreaOfChangeDto
                    {
                        Id = "2",
                        Name = "Project restart following temporary halt",
                        JourneyType = "Project restart following temporary halt",
                        ModificationAreaOfChangeId = 1
                    }
                ]
            }
        };

        tempData[TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(modificationResponse);

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = null });

        // Act
        var result = await Sut.AreaOfChange(projectModificationId, "Project halt");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }

    [Theory, AutoData]
    public async Task GetProjectRecordStatusAsync_ShouldReturnBadRequest_WhenServiceResponseSucceeds(
         string projectRecordId,
        int irasId,
        Guid projectModificationId,
        IrasApplicationResponse irasApplicationResponse
        )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Set all required TempData keys with correct namespacing and types
        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();
        irasApplicationResponse.Status = "Project halt";
        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.BadRequest, Content = irasApplicationResponse });

        // Act
        var result = await Sut.AreaOfChange(projectModificationId, "Project halt");

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory, AutoData]
    public async Task AreaOfChange_ShouldVerify_RemoveDropDownOptions(
         string projectRecordId,
        int irasId,
        Guid projectModificationId,
        IrasApplicationResponse irasApplicationResponse
        )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Set all required TempData keys with correct namespacing and types
        tempData[TempDataKeys.IrasId] = irasId.ToString();
        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-123";
        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();
        irasApplicationResponse.Status = "Project halt";
        // Mock GetRespondentFromContext extension
        var respondent = new { GivenName = "John", FamilyName = "Doe" };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { ["Respondent"] = respondent }
            }
        };

        var serviceResponse = new ServiceResponse<IEnumerable<GetAreaOfChangesResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });
        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = irasApplicationResponse });

        // Act
        var result = await Sut.AreaOfChange(projectModificationId, "Project halt");

        // Assert
        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }
}
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class AreaOfChangeTests : TestServiceBase<ModificationsController>
{
    [Theory, AutoData]
    public async Task AreaOfChange_RedirectsToAreaOfChange_WhenSuccessful
    (
        string projectRecordId,
        int irasId
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
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetAreaOfChanges())
            .ReturnsAsync(serviceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });

        // Act
        var result = await Sut.AreaOfChange();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }

    [Theory, AutoData]
    public async Task AreaOfChange_RedirectsToAreaOfChange_WhenContentIsNullOrEmpty
   (
       string projectRecordId,
       int irasId
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
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetAreaOfChanges())
            .ReturnsAsync(serviceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto()
            });

        // Act
        var result = await Sut.AreaOfChange();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
    }
}
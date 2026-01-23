using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.ProjectRecord.Controllers;

[Authorize(Policy = Workspaces.MyResearch)]
[Route("[controller]/[action]", Name = "prc:[action]")]
public class ProjectRecordController
(
    IApplicationsService applicationsService,
    IRespondentService respondentService,
    ICmsQuestionSetServiceClient cmsService
) : ProjectRecordControllerBase(respondentService)
{
    [ExcludeFromCodeCoverage]
    public IActionResult ProjectRecordExists()
    {
        return View();
    }

    [ExcludeFromCodeCoverage]
    public IActionResult ProjectNotEligible()
    {
        return View();
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
    [Route("/[controller]", Name = "prc:projectrecord")]
    public async Task<IActionResult> ProjectRecord(string sectionId)
    {
        // get the validated harp project record request from TempData
        var harpProjectRecord = TempData.Peek(TempDataKeys.ProjectRecord) as string;

        if (string.IsNullOrWhiteSpace(harpProjectRecord))
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Project record request data is missing."
            };

            return this.ServiceError(serviceResponse);
        }

        // Load the question set for project details
        var questionSetResponse = await cmsService.GetQuestionSet(sectionId);

        // if we have sections then grab the first section
        var sections = questionSetResponse.Content?.Sections ?? [];

        // section shouldn't be null here, this is a defensive
        // check
        if (sections is { Count: 0 })
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Unable to load questionnaire for project details",
            });
        }

        var section = sections[0];

        // build the questionnaire from the question set response
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetResponse.Content!);

        var projectRecord = JsonSerializer.Deserialize<ProjectRecordDto>(harpProjectRecord)!;

        // project details view model inherits from questionnaire view model it can adapt to it
        var projectRecordViewModel = questionnaire.Adapt<ProjectRecordViewModel>();

        // map the project details properties
        projectRecordViewModel.IrasId = projectRecord.IrasId!.Value;
        projectRecordViewModel.ShortProjectTitle = projectRecord.ShortProjectTitle!;
        projectRecordViewModel.FullProjectTitle = projectRecord.LongProjectTitle!;

        projectRecordViewModel.SectionId = section.Id;

        return View(projectRecordViewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
    [HttpPost]
    public async Task<IActionResult> ConfirmProjectRecord(ProjectRecordViewModel model)
    {
        // Retrieve all existing applications
        var applicationsResponse = await applicationsService.GetApplications();

        if (!applicationsResponse.IsSuccessStatusCode || applicationsResponse.Content == null)
        {
            // Return a generic service error view if retrieval fails
            return this.ServiceError(applicationsResponse);
        }

        // Check if an application with the same IRAS ID already exists
        bool irasIdExists = applicationsResponse.Content.Any(app => app.IrasId == model.IrasId);

        if (irasIdExists)
        {
            // Redirect to project already exists page
            return RedirectToRoute("prc:projectrecordexists");
        }

        // Get the respondent information from the current context
        var userId = this.GetUserIdFromContext();

        // Create a new application request object
        var projectRecordRequest = new IrasApplicationRequest
        {
            ShortProjectTitle = model.ShortProjectTitle,
            FullProjectTitle = model.FullProjectTitle,
            CreatedBy = userId,
            UpdatedBy = userId,
            StartDate = DateTime.Now,
            UserId = userId,
            IrasId = model.IrasId,
        };

        // Call the service to create the new application
        var createResponse = await applicationsService.CreateApplication(projectRecordRequest);

        if (!createResponse.IsSuccessStatusCode || createResponse.Content == null)
        {
            // Return a generic service error view if creation fails
            return this.ServiceError(createResponse);
        }

        var irasApplication = createResponse.Content;

        // Save the newly created application to the session
        HttpContext.Session.SetString(SessionKeys.ProjectRecord, JsonSerializer.Serialize(irasApplication));

        // Load the question sections to determine the next section to continue answering questions
        // to complete the project record
        var questionSections = await cmsService.GetQuestionSections();

        if
        (
            !questionSections.IsSuccessStatusCode ||
            questionSections.Content == null ||
            questionSections.Content.FirstOrDefault(section => section.SectionId != model.SectionId) is not QuestionSectionsResponse section
        )
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Unable to load questionnaire sections.",
            });
        }

        // save the submitted answers
        await SaveProjectRecordAnswers(irasApplication.Id, model.Questions);

        TempData[TempDataKeys.CategoryId] = section.QuestionCategoryId;
        TempData[TempDataKeys.ProjectRecordId] = irasApplication.Id;
        TempData[TempDataKeys.IrasId] = irasApplication.IrasId;

        // get the next section to continue the application process
        var nextSection = questionSections.Content
            .FirstOrDefault(s => s.SectionId != model.SectionId);

        // clear the TempData entry for project record
        TempData.Remove(TempDataKeys.ProjectRecord);

        // Redirect to the Questionnaire Resume action to continue the application process
        return RedirectToAction(nameof(QuestionnaireController.Resume), "Questionnaire", new
        {
            sectionId = nextSection?.SectionId,
            categoryId = section.QuestionCategoryId,
            projectRecordId = irasApplication.Id,
            ignorePreviousSection = true
        });
    }
}
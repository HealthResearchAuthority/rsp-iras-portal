using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Authorize(Policy = "IsReviewer")]
[Route("[controller]", Name = "arc:[action]")]
public class ApplicationsReviewController(ILogger<ApplicationsReviewController> logger, IApplicationsService applicationsService) : Controller
{
    public async Task<IActionResult> PendingApplications()
    {
        logger.LogInformationHp("called");

        // get the pending applications
        var applicationsServiceResponse = await applicationsService.GetApplicationsByStatus("pending");

        // return the view if successfull
        if (applicationsServiceResponse.IsSuccessStatusCode)
        {
            return View(applicationsServiceResponse.Content);
        }

        // return the generic error page
        return this.ServiceError(applicationsServiceResponse);
    }

    [Route("{applicationId}", Name = "arc:GetApplication")]
    public async Task<IActionResult> GetApplication(string applicationId)
    {
        logger.LogInformationHp("called");

        // if the ModelState is invalid, return the view
        // with the null model. The view shouldn't display any
        // data as model is null
        if (!ModelState.IsValid)
        {
            return View("ApplicationReview");
        }

        // get the pending application by id
        var applicationsServiceResponse = await applicationsService.GetApplicationByStatus(applicationId, "pending");

        // return the view if successfull
        if (applicationsServiceResponse.IsSuccessStatusCode)
        {
            return View("ApplicationReview", applicationsServiceResponse.Content);
        }

        // return the generic error page
        return this.ServiceError(applicationsServiceResponse);
    }
}
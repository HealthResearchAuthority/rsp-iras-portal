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
        logger.LogMethodStarted(LogLevel.Information);

        // get the pending applications
        var response = await applicationsService.GetApplicationsByStatus("pending");

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return View(result.Value);
        }

        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return result.StatusCode switch
        {
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => View("Error", result.Value)
        };
    }

    [Route("{applicationId}", Name = "arc:GetApplication")]
    public async Task<IActionResult> GetApplication(string applicationId)
    {
        logger.LogMethodStarted(LogLevel.Information);

        // if the ModelState is invalid, return the view
        // with the null model. The view shouldn't display any
        // data as model is null
        if (!ModelState.IsValid)
        {
            return View("ApplicationReview");
        }

        // get the pending application by id
        var response = await applicationsService.GetApplicationByStatus(applicationId, "pending");

        // convert the service response to ObjectResult
        var result = this.ServiceResult(response);

        // return the view if successfull
        if (response.IsSuccessStatusCode)
        {
            return View("ApplicationReview", result.Value);
        }

        // if status is forbidden or not found
        // return the appropriate response otherwise
        // return the generic error page
        return result.StatusCode switch
        {
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => View("Error", result.Value)
        };
    }
}
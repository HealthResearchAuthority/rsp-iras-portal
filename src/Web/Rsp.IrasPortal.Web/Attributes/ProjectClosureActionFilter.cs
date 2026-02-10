using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;

namespace Rsp.IrasPortal.Web.Attributes;

[ExcludeFromCodeCoverage]
public class ProjectClosureActionFilter : IAsyncActionFilter
{
    private readonly IApplicationsService _applicationsService;

    /// <summary>
    /// Constructor to initialse service
    /// </summary>
    /// <param name="applicationsService"></param>
    public ProjectClosureActionFilter(IApplicationsService applicationsService)
    {
        _applicationsService = applicationsService;
    }

    /// <summary>
    /// This action called before the actual controller action method invoke to validate the status of the project record
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var projectRecordId = context.HttpContext.Request.Query["ProjectRecordId"].ToString();

        if (!string.IsNullOrEmpty(projectRecordId))
        {
            var projectRecord = await _applicationsService.GetProjectRecord(projectRecordId);

            if (projectRecord?.Content?.Status is ProjectRecordStatus.PendingClosure or ProjectRecordStatus.Closed)
            {
                // Redirect to a closeproject
                context.Result = new RedirectToActionResult
                (
                    actionName: "closeproject",
                    controllerName: "application",
                    routeValues: new { id = projectRecordId, status = projectRecord?.Content?.Status }
                );
                return;
            }
        }
        await next();
    }
}
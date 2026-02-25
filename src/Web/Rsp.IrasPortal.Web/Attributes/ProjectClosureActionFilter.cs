using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Attributes;

[ExcludeFromCodeCoverage]
public class ProjectClosureActionFilter : IAsyncActionFilter
{
    private readonly IApplicationsService _applicationsService;

    /// <summary>
    /// Constructor to initialse service
    /// </summary>
    /// <param name="applicationsService"></param>
    public ProjectClosureActionFilter(IApplicationsService applicationsService, IProjectModificationsService projectModificationsService)
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
                context.Result = new RedirectToActionResult
                (
                    actionName: nameof(ApplicationController.ValidateProjectClosure),
                    controllerName: "application",
                    routeValues: new ProjectClosuresModel
                    {
                        Id = Guid.NewGuid(),
                        Status = projectRecord.Content.Status,
                        ProjectRecordId = projectRecord.Content.Id,
                        ShortProjectTitle = projectRecord.Content.ShortProjectTitle,
                        IrasId = projectRecord?.Content?.IrasId
                    }
                );
                return;
            }
        }
        await next();
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.Constants;

namespace Rsp.IrasPortal.Web.Attributes;

/// <summary>
/// Authorization filter that validates a user has the required access level to a project.
/// Checks the user's collaborator access against the specified access level for a given project.
/// Returns 403 Forbidden if the user lacks the required access.
/// </summary>
/// <param name="accessLevel">The required access level (e.g., "Read", "Write", "Admin")</param>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequireCollaboratorAccessAttribute(string accessLevel) : ActionFilterAttribute
{
    /// <summary>
    /// Executes the authorization check before the action method runs.
    /// Extracts the project identifier, retrieves collaborator projects from session,
    /// and validates the user has the required access level.
    /// </summary>
    /// <param name="context">The action executing context containing request and action details</param>
    /// <param name="next">The delegate to execute the next filter or action method</param>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.IsInRole(Roles.SystemAdministrator))
        {
            await next();
            return;
        }

        // Attempt to retrieve projectRecordId from temp data
        var controller = context.Controller as Controller;

        // Deny access if projectRecordId couldn't be resolved
        if (controller?.TempData.Peek(TempDataKeys.ProjectRecordId) is not string projectRecordId)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Retrieve the user's collaborator projects from session
        var projects = context.HttpContext.Session.GetString(SessionKeys.CollaboratorProjects);

        // Deny access if no collaborator projects exist in session
        if (string.IsNullOrWhiteSpace(projects))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Deserialize the collaborator projects and find the matching project
        var collaboratorProjects = JsonSerializer.Deserialize<List<CollaboratorProjectResponse>>(projects);

        var access = collaboratorProjects?.FirstOrDefault(p => p.ProjectRecordId == projectRecordId)?.ProjectAccessLevel;

        // Deny access if the project is not found or the access level doesn't match
        if (access == null || access != accessLevel)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Access granted - continue to the action method
        await next();
    }
}
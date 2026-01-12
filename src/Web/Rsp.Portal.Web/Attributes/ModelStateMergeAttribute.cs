using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Attributes;

/// <summary>
/// An action filter attribute that merges ModelState from TempData into the current ModelState
/// before the action executes. This is useful for preserving validation errors across redirects.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ModelStateMergeAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Called before the action method is invoked.
    /// Merges ModelState from TempData if available.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Ensure the controller is of type Controller to access TempData
        if (context.Controller is not Controller controller)
        {
            base.OnActionExecuting(context);
            return;
        }

        // If TempData does not contain ModelState, do nothing
        if (!controller.TempData.ContainsKey(TempDataKeys.ModelState))
        {
            return;
        }

        // Merge ModelState from TempData into the current ModelState
        context.ModelState.FromTempData(controller.TempData, TempDataKeys.ModelState);
    }
}
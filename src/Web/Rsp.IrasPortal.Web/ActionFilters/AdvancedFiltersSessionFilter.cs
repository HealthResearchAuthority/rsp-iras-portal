using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Rsp.IrasPortal.Web.ActionFilters;

public sealed class AdvancedFiltersSessionFilter : IActionFilter
{
    private readonly Dictionary<string, string[]> _controllerSessionMap;

    /// <summary>
    ///     Clears advanced filter session keys when navigating outside mapped controllers.
    /// </summary>
    public AdvancedFiltersSessionFilter(Dictionary<string, string[]> controllerSessionMap)
    {
        _controllerSessionMap = controllerSessionMap ?? [];
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.ActionDescriptor is ControllerActionDescriptor cad
            ? cad.ControllerName
            : context.RouteData.Values["controller"]?.ToString();

        if (controller == null)
        {
            return;
        }

        // If the current controller has mappings, do nothing
        if (_controllerSessionMap.ContainsKey(controller))
        {
            return;
        }

        // Otherwise, clear all registered filter session keys
        foreach (var kv in _controllerSessionMap.Values)
        {
            foreach (var key in kv)
            {
                context.HttpContext.Session.Remove(key);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
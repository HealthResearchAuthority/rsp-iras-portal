using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Rsp.IrasPortal.Configuration.FeatureFolders;

public class FeatureViewLocationExpander(FeatureFolderOptions options) : IViewLocationExpander
{
    private readonly string _placeholder = options.FeatureNamePlaceholder;

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // Parameter validation separated from iterator logic (Sonar rule S4456 compliance)
        ValidateExpandViewLocationsArgs(context, viewLocations);

        return ExpandViewLocationsCore(context!, viewLocations!);
    }

    private static void ValidateExpandViewLocationsArgs(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(viewLocations);
    }

    private IEnumerable<string> ExpandViewLocationsCore(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        if
        (
            context.ActionContext.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor ||
            !controllerActionDescriptor.Properties.TryGetValue("feature", out var featureProperty) ||
            string.IsNullOrEmpty(featureProperty as string)
        )
        {
            // If we cannot determine the controller descriptor, just return the original locations.
            foreach (var location in viewLocations)
            {
                yield return location;
            }

            yield break;
        }

        var featureName = featureProperty as string;

        var pathSegments = featureName!.Split(["\\\\", "\\"], StringSplitOptions.RemoveEmptyEntries);

        var expandedViewLocations = new List<string>(viewLocations);

        if (pathSegments.Length >= 2)
        {
            var featureParent = pathSegments[1].Replace(options.FeatureFolderName, string.Empty);

            if (!string.IsNullOrEmpty(featureParent))
            {
                var viewsPath = Path.Combine("/", options.FeatureFolderName, featureParent, "Views", "{0}.cshtml");
                var sharedPath = Path.Combine("/", options.FeatureFolderName, featureParent, "Shared", "{0}.cshtml");

                expandedViewLocations.AddRange(viewsPath, sharedPath);
            }
        }

        foreach (var location in expandedViewLocations)
        {
            // Replace placeholder {3} (conventionally used for area/feature) with the resolved feature name.
            yield return location.Replace(_placeholder, featureName);
        }
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActionContext?.ActionDescriptor is ControllerActionDescriptor)
        {
            // Contribute to the cache key so different features cache separately.
            context.Values["action_displayname"] = context.ActionContext.ActionDescriptor.DisplayName;
        }
    }
}
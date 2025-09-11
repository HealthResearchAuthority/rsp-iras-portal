using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Rsp.IrasPortal.Configuration.FeatureFolders;

/// <summary>
/// If feature path has at least 2 segments (root + parent):
///    - Derive 'featureParent' (remove leading configured feature folder name).
///    - Append feature-specific Views and Shared folders: /Features/{Parent}/Views/{0}.cshtml, /Features/{Parent}/Shared/{0}.cshtml
///    - For non-Default component views, also add direct component view path under feature parent.
/// Always (for non-Default component views) add a global feature Components fallback: /Features/Components/{ComponentName}.cshtml
/// For every accumulated location pattern, replace the configured placeholder (e.g. {Feature}) with the resolved feature path.
/// Contribute a cache key value in PopulateValues so view discovery caching differentiates across features.
/// </summary>
public class FeatureViewLocationExpander(FeatureFolderOptions options) : IViewLocationExpander
{
    /// <summary>
    /// Placeholder token (e.g. "{Feature}") that will be replaced with the
    /// derived feature path in each candidate view location.
    /// </summary>
    private readonly string _placeholder = options.FeatureNamePlaceholder;

    /// <summary>
    /// Public entry point invoked by the Razor engine to expand search locations.
    /// Performs argument validation separately to keep iterator method clean.
    /// </summary>
    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // Parameter validation separated from iterator logic (Sonar rule S4456 compliance)
        ValidateExpandViewLocationsArgs(context, viewLocations);

        return ExpandViewLocationsCore(context!, viewLocations!);
    }

    /// <summary>
    /// Validates required parameters for the view expansion process.
    /// </summary>
    private static void ValidateExpandViewLocationsArgs(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(viewLocations);
    }

    /// <summary>
    /// Core iterator that yields expanded view search paths based on feature metadata and
    /// optional ViewComponent resolution.
    /// </summary>
    private IEnumerable<string> ExpandViewLocationsCore(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        if
        (
            context.ActionContext.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor ||
            !controllerActionDescriptor.Properties.TryGetValue("feature", out var featureProperty) ||
            string.IsNullOrEmpty(featureProperty as string)
        )
        {
            // If we cannot determine the controller descriptor or feature metadata, fall back to the original locations unchanged.
            foreach (var location in viewLocations)
            {
                yield return location;
            }

            yield break;
        }

        // Resolved feature path string (e.g. Features\Admin\ManageUsers)
        var featureName = featureProperty as string;

        // Split using both escaped and normal backslashes to be resilient to differing construction styles.
        var pathSegments = featureName!.Split(["\\\\", "\\"], StringSplitOptions.RemoveEmptyEntries);

        // Start with the existing framework-provided view locations to preserve defaults.
        var expandedViewLocations = new List<string>(viewLocations);

        // Standard Razor placeholder pattern: {0} replaced by view name later in pipeline.
        var viewExtension = "{0}" + RazorViewEngine.ViewExtension;

        // Will hold the ViewComponent name if applicable (e.g. "BackNavigation").
        var componentName = string.Empty;

        // Detect ViewComponent partial view discovery: ViewName will begin with "Components/..."
        if (context.ViewName.StartsWith("Components", StringComparison.OrdinalIgnoreCase))
        {
            var start = context.ViewName.LastIndexOf('/');

            componentName = context.ViewName[(start + 1)..];
        }

        // Only proceed if we have at least two path segments: e.g. ["Features", "Admin", ...]
        if (pathSegments.Length >= 2)
        {
            // Remove the configured root feature folder name from the second segment to derive the parent scope.
            var featureParent = pathSegments[1].Replace(options.FeatureFolderName, string.Empty);

            if (!string.IsNullOrEmpty(featureParent))
            {
                // Feature-scoped Views and Shared locations patterns.
                var viewsPath = Path.Combine("/", options.FeatureFolderName, featureParent, "Views", viewExtension);
                var sharedPath = Path.Combine("/", options.FeatureFolderName, featureParent, "Shared", viewExtension);

                expandedViewLocations.AddRange(viewsPath, sharedPath);

                // For non-Default view components, add a direct component view file candidate inside the feature.
                // Example: /Features/Modifications/Components/BackNavigation.cshtml
                if (!string.IsNullOrWhiteSpace(componentName) && componentName != "Default")
                {
                    string[] componentViewPaths =
                    [
                        Path.Combine("/",  options.FeatureFolderName, featureParent, "Components", componentName + RazorViewEngine.ViewExtension)
                    ];

                    expandedViewLocations.AddRange(componentViewPaths);
                }
            }
        }

        // Global (feature-root) component fallback for non-Default component views.
        if (!string.IsNullOrWhiteSpace(componentName) && componentName != "Default")
        {
            expandedViewLocations.Add(Path.Combine("/", options.FeatureFolderName, "Components", componentName + RazorViewEngine.ViewExtension));
        }

        expandedViewLocations.Add("/Features/Modifications/Shared/{0}.cshtml");
        expandedViewLocations.Add("/Features/Modifications/Views/{0}.cshtml");

        foreach (var location in expandedViewLocations)
        {
            // Replace placeholder (e.g. {Feature}) with resolved feature path to finalize each candidate location.
            yield return location.Replace(_placeholder, featureName);
        }
    }

    /// <summary>
    /// Contributes values to the cache key. Ensures views from different features do not collide in caching.
    /// </summary>
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActionContext?.ActionDescriptor is ControllerActionDescriptor)
        {
            // Using DisplayName differentiates actions across features for the view location cache.
            context.Values["action_displayname"] = context.ActionContext.ActionDescriptor.DisplayName;
        }
    }
}
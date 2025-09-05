using Microsoft.AspNetCore.Mvc.Razor;

namespace Rsp.IrasPortal.Configuration.FeatureFolders;

public static class FeatureFoldersConfiguration
{
    public static void ConfigureFeatureFolders(this RazorViewEngineOptions options, FeatureFolderOptions featureFolderOptions)
    {
        // {0} - Action Name
        // {1} - Controller Name
        // {2} - Area Name
        // {3} - Feature Name
        options.ViewLocationFormats.Add("/" + featureFolderOptions.FeatureNamePlaceholder + "/{0}.cshtml");
        options.ViewLocationFormats.Add("/" + featureFolderOptions.FeatureNamePlaceholder + "/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/" + featureFolderOptions.FeatureNamePlaceholder + "/Shared/{0}.cshtml");
        options.ViewLocationFormats.Add("/" + featureFolderOptions.FeatureFolderName + "/Shared/{0}.cshtml");

        options.ViewLocationExpanders.Add(new FeatureViewLocationExpander(featureFolderOptions));
    }
}
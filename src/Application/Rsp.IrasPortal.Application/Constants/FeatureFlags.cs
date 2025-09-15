﻿namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// Defines constants for feature names.
/// </summary>
public static class FeatureFlags
{
    // Enhanced user experience when javascript is enabled
    public const string ProgressiveEnhancement = "UX.ProgressiveEnhancement";

    // Show/Hide Admin menu
    public const string Admin = "Navigation.Admin";

    // Intercepts the start/end of method calls if enabled
    public const string InterceptedLogging = "Logging.InterceptedLogging";

    // Use OneLogin authentication
    public const string OneLogin = "Auth.UseOneLogin";

    // Show projects added to new service
    public const string MyResearchPage = "UX.MyResearchPage";

    // Use Azure Front Door for web application routing
    public const string UseFrontDoor = "WebApp.UseFrontDoor";
}
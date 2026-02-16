namespace Rsp.Portal.Application.Constants;

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

    // Show sponsor management area in the sponsor workspace
    public const string SponsorManagementWorkspace = "UX.SponsorManagementWorkspace";

    // Use Azure Front Door for web application routing
    public const string UseFrontDoor = "WebApp.UseFrontDoor";

    // Allows revision request or revise and authorise modifications
    public const string RevisionAndAuthorisation = "Modifications.RevisionAndAuthorisation";

    // Allows adding / removing participating organisations via modification journey
    public const string ParticipatingOrganisations = "Modifications.ParticipatingOrganisations";

    // Allows downloading modification pack in modification journey
    public const string DownloadPack = "Modifications.DownloadPack";

    // Allows documents to be superseded in modification journey
    public const string SupersedingDocuments = "Modifications.SupersedingDocuments";

    // Allows withdrawing modification
    public const string WithdrawModification = "Modifications.Withdraw";

    // Allows requesting for information in modification journey
    public const string RequestForInformation = "Modifications.RequestForInformation";

    // Show back button in project overview page
    public const string BackButton = "ProjectOverview.BackButton";

    //All to enter reason for not authorising the modification
    public const string NotAuthorisedReason = "Modifications.NotAuthorisedReason";
}
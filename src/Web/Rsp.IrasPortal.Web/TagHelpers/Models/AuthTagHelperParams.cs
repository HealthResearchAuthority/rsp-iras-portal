namespace Rsp.IrasPortal.Web.TagHelpers.Models;

public enum RolesProcessing
{
    Or,
    And
}

/// <summary>
/// Parameters for toggling the visibility of the contents
/// </summary>
/// <param name="ShowWhenAuthenticated">Default: true, if set to false will render the contents for non-authenticated users</param>
/// <param name="Roles">
///     List of comma seperated roles that an authenticated user should be in.
///     <see cref="RolesLogic"/> Or:User should be in one of the roles. And: User should be in all of the roles.
/// </param>
/// <param name="RolesLogic">Default: Or. Defines if the <see cref="Roles"/> should be evaluated as OR or AND</param>
public record AuthTagHelperParams(bool ShowWhenAuthenticated = true, string? Roles = null, RolesProcessing RolesLogic = RolesProcessing.Or);
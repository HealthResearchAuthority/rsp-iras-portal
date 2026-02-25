using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;

namespace Rsp.Portal.Web.Helpers;

public static class SponsorOrganisationUsersHelper
{
    public static async Task<IEnumerable<string>> HandleDisableOrganisationUserRole(
        ISponsorOrganisationService sponsorOrganisationService,
        Guid userId,
        IUserManagementService userService)
    {
        var otherOrgs = await sponsorOrganisationService.GetAllActiveSponsorOrganisationsForEnabledUser(userId);

        if (otherOrgs.IsSuccessStatusCode &&
            otherOrgs.Content != null)
        {
            var organisations = otherOrgs.Content;

            // check if user is a sponsor or org admin in any other organisations
            var sponsorOrgs = organisations
            .Where(x => x.Users != null)
                .Where(x => x.Users!.Any(x => x.UserId == userId && x.SponsorRole == Roles.Sponsor));

            var organisationAdminOrgs = organisations
            .Where(x => x.Users != null)
                .Where(x => x.Users!.Any(x => x.UserId == userId && x.SponsorRole == Roles.OrganisationAdministrator));

            string rolesToRemove = string.Empty;

            if (!organisations.Any())
            {
                // user is not in any other organisations so remove sponsor and org admin roles
                rolesToRemove = string.Join(",", Roles.Sponsor, Roles.OrganisationAdministrator);
            }
            else if (!sponsorOrgs.Any())
            {
                // user is not a sponsor in any other organisations so just remove sponsor role
                rolesToRemove = Roles.Sponsor;
            }
            else if (!organisationAdminOrgs.Any())
            {
                // user is not an organisation admin in any other organisations so just remove organisation admin role
                rolesToRemove = Roles.OrganisationAdministrator;
            }

            if (!string.IsNullOrEmpty(rolesToRemove))
            {
                var user = await userService.GetUser(userId.ToString(), null);
                if (user.IsSuccessStatusCode && user.Content != null)
                {
                    await userService.UpdateRoles(user.Content.User.Email, rolesToRemove, string.Empty);
                }
            }

            var userObject = await userService.GetUser(userId.ToString(), null);
            if (userObject.IsSuccessStatusCode && userObject.Content != null)
            {
                return userObject.Content.Roles;
            }
        }
        return new List<string>();
    }
}
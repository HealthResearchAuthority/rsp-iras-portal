using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Web.Areas.Admin.Models;

public class UserViewModel
{
    [HiddenInput]
    public string? Id { get; set; } = null;

    [HiddenInput]
    public string? OriginalEmail { get; set; }

    public string? Title { get; set; } = null;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Telephone { get; set; } = null!;

    public string? Organisation { get; set; } = null;

    public string? JobTitle { get; set; } = null;

    public IList<UserRoleViewModel> UserRoles { get; set; } = [];
    public IList<UserReviewBodyViewModel> ReviewBodies { get; set; } = [];

    public IList<string>? Country { get; set; } = null;

    public DateTime? LastUpdated { get; set; } = null;

    public string? IdentityProviderId { get; set; }

    private string? _status;

    public string Status
    {
        get
        {
            if (_status != null)
            {
                // always cast status string to sentence case for displaying
                var textInfo = new CultureInfo("en-GB", false).TextInfo;

                return textInfo.ToTitleCase(_status!);
            }
            else
            {
                return string.Empty;
            }
        }
        set => _status = string.IsNullOrEmpty(value) ? IrasUserStatus.Active : value;
    }

    public DateTime? LastLogin { get; set; } = null;
    public DateTime? CurrentLogin { get; set; } = null;

    public UserViewModel()
    { }

    public UserViewModel(UserResponse identityUserResponse)
    {
        var user = identityUserResponse?.User;
        var roles = identityUserResponse?.Roles;
        //var accessRequired = identityUserResponse?.AccessRequired;

        if (user != null)
        {
            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            Id = user.Id;
            GivenName = user.GivenName;
            FamilyName = user.FamilyName;
            Email = user.Email;
            Telephone = user.Telephone;
            Country = !string.IsNullOrEmpty(user.Country) ? user.Country.Split(',') : null;
            Title = user.Title;
            JobTitle = user.JobTitle;
            Organisation = user.Organisation;
            UserRoles = roles != null ? roles.Select(role => new UserRoleViewModel { Name = role, IsSelected = true }).ToList() : [];
            LastUpdated = user.LastUpdated;
            OriginalEmail = user.Email;
            Status = user.Status;
            CurrentLogin = user.CurrentLogin.HasValue ? TimeZoneInfo.ConvertTimeFromUtc((DateTime)user.CurrentLogin, ukTimeZone) : null;
            IdentityProviderId = user.IdentityProviderId;
        }
    }

    public UserViewModel(User user)
    {
        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        Id = user.Id;
        GivenName = user.GivenName;
        FamilyName = user.FamilyName;
        Email = user.Email;
        Status = user.Status;
        LastLogin = user.LastLogin;
        CurrentLogin = user.CurrentLogin.HasValue ? TimeZoneInfo.ConvertTimeFromUtc((DateTime)user.CurrentLogin, ukTimeZone) : null;
        IdentityProviderId = user.IdentityProviderId;
    }

    public void Deconstruct(out string givenName, out string familyName, out string email)
    {
        givenName = GivenName;
        familyName = FamilyName;
        email = Email;
    }

    public void Deconstruct(out string id, out string originalEmail, out string givenName, out string familyName, out string email)
    {
        id = Id!;
        originalEmail = OriginalEmail!;
        givenName = GivenName;
        familyName = FamilyName;
        email = Email;
    }
}
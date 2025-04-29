using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Domain.Identity;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserViewModel
{
    [HiddenInput]
    public string? Id { get; set; } = null;

    [HiddenInput]
    public string? OriginalEmail { get; set; }

    public string? Title { get; set; } = null;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Telephone { get; set; } = null!;

    public string? Organisation { get; set; } = null;

    public string? JobTitle { get; set; } = null;

    public IList<UserRoleViewModel> UserRoles { get; set; } = [];

    public IList<string>? Country { get; set; } = null;

    public IList<string>? AccessRequired { get; set; } = null;

    public DateTime? LastUpdated { get; set; } = null;

    private string? _status;

    public string Status
    {
        get => _status!;
        set => _status = string.IsNullOrEmpty(value) ? IrasUserStatus.Active : value;
    }

    public DateTime? LastLogin { get; set; } = null;

    public UserViewModel()
    { }

    public UserViewModel(UserResponse identityUserResponse)
    {
        var user = identityUserResponse?.User;
        var roles = identityUserResponse?.Roles;

        if (user != null)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
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
        }
    }

    public UserViewModel(User user)
    {
        Id = user.Id;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Email = user.Email;
        Status = user.Status;
        LastLogin = user.LastLogin;
    }

    public void Deconstruct(out string firstName, out string lastName, out string email)
    {
        firstName = FirstName;
        lastName = LastName;
        email = Email;
    }

    public void Deconstruct(out string id, out string originalEmail, out string firstName, out string lastName, out string email)
    {
        id = Id!;
        originalEmail = OriginalEmail!;
        firstName = FirstName;
        lastName = LastName;
        email = Email;
    }
}
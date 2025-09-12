using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class UserSearchModel
{
    public string? SearchQuery { get; set; }
    public List<string> Country { get; set; } = [];
    public IList<UserRoleViewModel> UserRoles { get; set; } = [];
    public IList<UserReviewBodyViewModel> ReviewBodies { get; set; } = [];
    public bool? Status { get; set; }
    public DateTime? FromDate => DateTimeExtensions.ParseDateValidation(FromDay, FromMonth, FromYear);
    public DateTime? ToDate => DateTimeExtensions.ParseDateValidation(ToDay, ToMonth, ToYear);

    public string? FromDay { get; set; }
    public string? FromMonth { get; set; }
    public string? FromYear { get; set; }

    public string? ToDay { get; set; }
    public string? ToMonth { get; set; }
    public string? ToYear { get; set; }

    public Dictionary<string, string> Filters
    {
        get
        {
            var filters = new Dictionary<string, string>();

            if (Country?.Count > 0)
            {
                filters.Add(UsersSearch.CountryKey, string.Join(", ", Country));
            }

            if (ReviewBodies?.Any(rb => rb.IsSelected) == true)
            {
                var selectedReviewBodies = ReviewBodies
                    .Where(rb => rb.IsSelected)
                    .Select(rb => rb.RegulatoryBodyName.ToString()); // or rb.RegulatoryBodyName if you prefer

                filters.Add(UsersSearch.ReviewBodyKey, string.Join(", ", selectedReviewBodies));
            }

            if (UserRoles?.Any(r => r.IsSelected) == true)
            {
                var selectedRoles = UserRoles
                    .Where(r => r.IsSelected)
                    .Select(r => r.DisplayName?.ToString()); // or r.Name if you prefer

                filters.Add(UsersSearch.RoleKey, string.Join(", ", selectedRoles));
            }

            if (FromDate.HasValue)
            {
                filters.Add(UsersSearch.FromDateKey, FromDate.Value.ToString("d MMM yyyy"));
            }

            if (ToDate.HasValue)
            {
                filters.Add(UsersSearch.ToDateKey, ToDate.Value.ToString("d MMM yyyy"));
            }

            switch (Status)
            {
                case true:
                    filters.Add(UsersSearch.StatusKey, "Active");
                    break;
                case false:
                    filters.Add(UsersSearch.StatusKey, "Disabled");
                    break;
            }

            return filters;
        }
    }


}
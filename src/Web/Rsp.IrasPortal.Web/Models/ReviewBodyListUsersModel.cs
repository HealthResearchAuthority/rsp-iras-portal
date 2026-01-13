using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ReviewBodyListUsersModel
{
    public IEnumerable<UserViewModel> Users { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
    public AddUpdateReviewBodyModel ReviewBody { get; set; } = null!;
}
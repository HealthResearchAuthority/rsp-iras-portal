using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

public class ConfirmAddRemoveReviewBodyUserModel
{
    public AddUpdateReviewBodyModel ReviewBody { get; set; } = null!;
    public UserViewModel User { get; set; } = null!;
    public bool IsRemove { get; set; } = false;
}
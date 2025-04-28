using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

public class ConfirmAddReviewBodyUserModel
{
    public AddUpdateReviewBodyModel ReviewBody { get; set; } = null!;
    public UserViewModel User { get; set; } = null!;
}
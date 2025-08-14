using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserReviewBodyViewModel 
{
    public Guid Id { get; set; }
    public string RegulatoryBodyName { get; set; } = null!;
    public string? DisplayName => RegulatoryBodyName?.Replace("_", " ")?.ToSentenceCase();
    public bool IsSelected { get; set; }

}
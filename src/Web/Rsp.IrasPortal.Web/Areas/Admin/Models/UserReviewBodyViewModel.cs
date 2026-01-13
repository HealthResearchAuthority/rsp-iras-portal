using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.Web.Areas.Admin.Models;

public class UserReviewBodyViewModel 
{
    public Guid Id { get; set; }
    public string RegulatoryBodyName { get; set; } = null!;
    public string? DisplayName => RegulatoryBodyName?.Replace("_", " ")?.ToSentenceCase();
    public bool IsSelected { get; set; }

}
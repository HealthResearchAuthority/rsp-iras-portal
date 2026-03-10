using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class UserNotificationResponse
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string? TargetUrl { get; set; }
    public string Type { get; set; } = null!;
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime DateTimeCreated { get; set; }
    public DateTime? DateTimeSeen { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ReviewBodyAuditTrailResponse
{
    public IEnumerable<ReviewBodyAuditTrailDto> Items { get; set; } = Enumerable.Empty<ReviewBodyAuditTrailDto>();
    public int TotalCount { get; set; }
}
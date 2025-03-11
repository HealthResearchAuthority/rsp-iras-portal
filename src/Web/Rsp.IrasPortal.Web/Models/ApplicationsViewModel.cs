using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class ApplicationsViewModel
{
    public IEnumerable<IrasApplicationResponse> Applications { get; set; } = new List<IrasApplicationResponse>();
    public IEnumerable<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
}
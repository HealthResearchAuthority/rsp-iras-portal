using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class QuestionSetViewModel
{
    public IFormFile? Upload { get; set; }

    public QuestionSetDto? QuestionSetDto { get; set; }

    public List<VersionDto> Versions { get; set; } = [];
}
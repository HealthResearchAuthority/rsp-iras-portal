using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class QuestionSetFileModel
{
    public IFormFile? Upload { get; set; }

    public List<QuestionDto> QuestionDtos { get; set; } = [];
}
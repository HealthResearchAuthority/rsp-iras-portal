using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class QuestionSetFileModel
{
    public IFormFile? Upload { get; set; }

    public List<QuestionDto> QuestionDtos { get; set; } = [];

    public List<CategoryDto> CategoryDtos { get; set; } = [];

    public List<SectionDto> SectionDtos { get; set; } = [];

    public List<AnswerOptionDto> AnswerOptionDtos { get; set; } = [];
}
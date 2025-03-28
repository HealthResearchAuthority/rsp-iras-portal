namespace Rsp.IrasPortal.Web.Models;

using Rsp.IrasPortal.Application.DTOs;

public class TemplateModel
{
    public IEnumerable<TemplateDTO> Templates { get; set; } = [];
}
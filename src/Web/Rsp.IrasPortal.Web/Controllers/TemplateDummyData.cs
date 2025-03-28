using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public class TemplateDummyData
{
    public static TemplateModel GetDummyData()
    {
        var templatemodel = new TemplateModel()
        {
            Templates = new List<TemplateDTO>
            {
                new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "ASSESS-meso",
                        IrasID = "220360",
                        ProjectLink = "https://google.com"
                    },
                    Category = "C",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                  new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "STOLEN",
                        IrasID = "169644",
                        ProjectLink = "https://google.com"
                    },
                    Category = "A",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                    new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "Pre-treatment drops or spray for managing earwax",
                        IrasID = "298113",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "SIGNAL 2.0",
                        IrasID = " 118906",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                 new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "ASSESS-dir",
                        IrasID = "136009",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "Prevalent and incident atrial fibrillation and stroke",
                        IrasID = "211776",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "NEW"
                },
                 new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "ASSESS-meso",
                        IrasID = "220360",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
                new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "STOLEN",
                        IrasID = "169644",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
                 new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "Pre-treatment drops or spray for managing earwax",
                        IrasID = " 298113",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
               new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "SIGNAL 2.0",
                        IrasID = " 118906",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
                 new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "ASSESS-dir",
                        IrasID = "136009",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
                new TemplateDTO
                {
                    ProjectInfo = new ProjectInfoDTO
                    {
                        ProjectName = "Prevalent and incident atrial fibrillation and stroke",
                        IrasID = "211776",
                        ProjectLink = "https://google.com"
                    },
                    Category = "B",
                    DateSubmitted = new DateTime(2024, 08, 14),
                    DateLeftOnTheClock = "35 Days",
                    Status = "IN PROGRESS"
                },
            }
        };
        return templatemodel;
    }
}
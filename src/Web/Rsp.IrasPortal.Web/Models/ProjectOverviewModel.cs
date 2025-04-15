namespace Rsp.IrasPortal.Web.Models;

public class ProjectOverviewModel
{
    public string ProjectId { get; set; }

    public string ProjectTitle { get; set; }

    public ProjectOverviewModel(string projectTitle)
    {
        // add more here or repurpose existing? proof of concept
        ProjectTitle = projectTitle;
    }
}
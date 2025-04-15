namespace Rsp.IrasPortal.Web.Models;

public class ProjectOverviewModel
{
    public string ProjectTitle { get; set; }

    public string CategoryId { get; set; }

    public string ApplicationId { get; set; }

    public ProjectOverviewModel(string projectTitle, string categoryId, string applicationId)
    {
        ProjectTitle = projectTitle;
        CategoryId = categoryId;
        ApplicationId = applicationId;
    }
}
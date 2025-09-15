namespace Rsp.IrasPortal.Application.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CmsContentActionAttribute : Attribute
{
    /// <summary>
    /// Specify the methid name that can ge used to retrieve the CMS content (usually the main GET method in this controller)
    /// </summary>
    /// <param name="actionName"></param>
    public CmsContentActionAttribute(string actionName)
    {
        ActionName = actionName;
    }

    public string ActionName { get; }
}
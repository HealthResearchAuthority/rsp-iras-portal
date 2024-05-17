using Rsp.IrasPortal.Application;

namespace Rsp.IrasPortal.Web.Extensions;

public static class SessionExtension
{
    public static void RemoveAllSessionValues(this ISession session)
    {
        session.Remove(SessionConstants.Id);
        session.Remove(SessionConstants.Title);
        session.Remove(SessionConstants.Country);
        session.Remove(SessionConstants.ApplicationType);
        session.Remove(SessionConstants.ProjectCategory);
        session.Remove(SessionConstants.StartDate);
    }
}
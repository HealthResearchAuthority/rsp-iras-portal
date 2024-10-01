using Rsp.IrasPortal.Application;

namespace Rsp.IrasPortal.Web.Extensions;

public static class SessionExtension
{
    public static void RemoveAllSessionValues(this ISession session)
    {
        session.Remove(SessionConstants.Application);
    }
}
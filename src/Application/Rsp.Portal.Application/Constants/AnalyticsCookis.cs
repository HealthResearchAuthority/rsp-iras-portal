namespace Rsp.Portal.Application.Constants;

public static class AnalyticsCookies
{
    /// <summary>
    /// Prefix for analytics cookies.
    /// Covers GoogleAnalytics and Microsoft Clarity cookies
    /// </summary>
    public static readonly string[] AnalyticsCookiePrefixes =
    {
        "_ga",        // GA4 + older GA
        "_gid",
        "_gat",
        "_gac_",
        "_cl",        // covers _clck, _clsk, _cltk, etc.
        "CLID",
        "ANONCHK",
        "MUID"
    };
}
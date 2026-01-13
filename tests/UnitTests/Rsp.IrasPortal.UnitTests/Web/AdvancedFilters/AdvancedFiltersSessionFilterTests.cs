using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Rsp.Portal.Web.ActionFilters;

namespace Rsp.Portal.UnitTests.Web.AdvancedFilters;

public class AdvancedFiltersSessionFilterTests
{
    private static ActionExecutingContext MakeContext(string? controllerName, ISession session)
    {
        var http = new DefaultHttpContext { Session = session };

        var descriptor =
            controllerName is null
                ? new ActionDescriptor() // no controller info; will hit controller == null branch
                : new ControllerActionDescriptor { ControllerName = controllerName };

        var actionContext = new ActionContext(http, new RouteData(), descriptor);
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null
        );
    }

    [Fact]
    public void Clears_registered_keys_when_navigating_outside_mapped_controllers()
    {
        // arrange
        var map = new Dictionary<string, string[]>
        {
            ["Approvals"] = new[] { "session:ApprovalsSearch", "session:Tasklist" },
            ["Users"] = new[] { "session:UserSearch" }
        };

        var session = new InMemorySession();
        session.SetString("session:ApprovalsSearch", "x");
        session.SetString("session:Tasklist", "y");
        session.SetString("session:UserSearch", "z");
        session.SetString("session:Unrelated", "keep"); // should never be cleared

        var ctx = MakeContext("Home", session); // "Home" is NOT in map => should clear

        var filter = new AdvancedFiltersSessionFilter(map);

        // act
        filter.OnActionExecuting(ctx);

        // assert
        session.Keys.ShouldNotContain("session:ApprovalsSearch");
        session.Keys.ShouldNotContain("session:Tasklist");
        session.Keys.ShouldNotContain("session:UserSearch");
        session.Keys.ShouldContain("session:Unrelated"); // untouched
    }

    [Fact]
    public void Does_not_clear_when_current_controller_is_mapped()
    {
        // arrange
        var map = new Dictionary<string, string[]>
        {
            ["Approvals"] = new[] { "session:ApprovalsSearch", "session:Tasklist" }
        };

        var session = new InMemorySession();
        session.SetString("session:ApprovalsSearch", "x");
        session.SetString("session:Tasklist", "y");

        var ctx = MakeContext("Approvals", session); // mapped controller

        var filter = new AdvancedFiltersSessionFilter(map);

        // act
        filter.OnActionExecuting(ctx);

        // assert (nothing cleared)
        session.Keys.ShouldContain("session:ApprovalsSearch");
        session.Keys.ShouldContain("session:Tasklist");
    }

    [Fact]
    public void Noop_when_controller_is_unknown()
    {
        // arrange
        var map = new Dictionary<string, string[]>
        {
            ["Approvals"] = new[] { "session:ApprovalsSearch" }
        };

        var session = new InMemorySession();
        session.SetString("session:ApprovalsSearch", "x");

        var ctx = MakeContext(null, session); // ActionDescriptor without controller info

        var filter = new AdvancedFiltersSessionFilter(map);

        // act
        filter.OnActionExecuting(ctx);

        // assert (unchanged)
        session.Keys.ShouldContain("session:ApprovalsSearch");
    }
}
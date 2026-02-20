using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Filters;

namespace Rsp.IrasPortal.Web.Attributes;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public class ModificationAuthoriseAttribute : TypeFilterAttribute
{
    public ModificationAuthoriseAttribute(string permission) : base(typeof(ModificationAuthoriseFilter))
    {
        Arguments = new[] { permission };
    }
}
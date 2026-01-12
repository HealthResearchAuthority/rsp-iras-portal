using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rsp.Portal.Application.Constants;

public static class OrganisationTypes
{
    public static readonly List<string> Types = [
        "NHS or HSC organisations",
        "Independent primary care contractors providing NHS or HSC services",
        "Local councils",
        "Other NHS or HSC setting",
        "University"];
}
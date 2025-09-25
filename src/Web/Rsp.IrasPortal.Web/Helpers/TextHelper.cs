using System.Text.RegularExpressions;

namespace Rsp.IrasPortal.Web.Helpers;

public static class TextHelper
{
    private static readonly string[] Acronyms =
    {
        "ABPI","AE","AHP","AR","ARCs","ARSAC","ATIMP","ATMP","CA","CI","COPI","CPMS","CRF","CRFs",
        "CRN","CRO","CSP","CTA","CT","CTIMP","CTU","DA","DH","DHSC","DMC","DSUR","EC","EMA","EMEA",
        "EVCTM","FDA","GAfREC","GCP","GDPR","GMP","GTAC","HRA","HS (COPI) Regs","HS (CPI) Regs",
        "IB","IC","ICH","ICMJE","IMP","IMPD","IP","IRAS","IRB","ISF","ISRCTN","MA","MCA","MHRA",
        "MRC","NIGB","NICE","NIHR","NIMP","NRES","PIS","PIC","PICs","PIL","PI","PPI","QA","QC",
        "QP","RCT","RDB","REC","RTB","SAE","SmPC","SOPs","SPHR","SRMRC","SUSAR","TMG","TMF",
        "TSC","USM","WHO"
    };

    public static string ToSentenceCaseWithAcronyms(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var lowered = input.ToLowerInvariant();

        foreach (var acronym in Acronyms)
        {
            lowered = Regex.Replace(
                lowered,
                $@"\b{Regex.Escape(acronym.ToLowerInvariant())}\b",
                acronym,
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(200) // Prevent runaway regex
             );
        }

        return lowered;
    }
}
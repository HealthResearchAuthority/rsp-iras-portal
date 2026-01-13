namespace Rsp.Portal.Application.Constants;

public static class Ranking
{
    public const string NotAvailable = "Not available";

    public struct ModificationTypes
    {
        public const string MinorModification = "Minor modification";
        public const string ModificationOfAnImportantDetail = "Modification of an important detail";
        public const string NonNotifiable = "Non-notifiable";
        public const string Substantial = "Substantial";

        public static List<string> AllOptions => [
            MinorModification,
            ModificationOfAnImportantDetail,
            NonNotifiable,
            Substantial
        ];
    }

    public struct CategoryTypes
    {
        public const string A = "A";
        public const string B = "B";
        public const string BC = "B/C";
        public const string C = "C";
        public const string NewSite = "New Site";
        public const string NA = "N/A";

        public static List<string> AllOptions => [
            A,
            B,
            BC,
            C,
            NewSite,
            NA
        ];
    }

    public struct ReviewTypes
    {
        public const string NoReviewRequired = "No review required";
        public const string ReviewRequired = "Review required";

        public static List<string> AllOptions => [
            NoReviewRequired,
            ReviewRequired
        ];
    }
}
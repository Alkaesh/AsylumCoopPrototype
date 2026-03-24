namespace AsylumHorror.UI
{
    public static class RoundPresentationMemory
    {
        public static string LastHeadline { get; private set; }
        public static string LastSummary { get; private set; }

        public static bool HasSummary =>
            !string.IsNullOrWhiteSpace(LastHeadline) ||
            !string.IsNullOrWhiteSpace(LastSummary);

        public static void Store(string headline, string summary)
        {
            LastHeadline = headline;
            LastSummary = summary;
        }
    }
}

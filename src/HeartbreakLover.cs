namespace HuniepopEndless
{
    /// <summary>
    /// "This girl likes broken hearts" — in HP2 terms, a girl whose baggage turns a
    /// BROKEN match into something positive (a MatchModifier replace step, or the
    /// broken-protection ailment family). Detection is wired in with the power-up
    /// catalog; stubbed to false for now.
    /// </summary>
    internal static class HeartbreakLover
    {
        internal static bool IsLover(GirlDefinition girl)
        {
            return girl != null && EndlessRun.HeartbreakLovers.Contains(girl);
        }
    }
}

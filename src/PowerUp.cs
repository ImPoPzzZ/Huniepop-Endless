using System;

namespace HuniepopEndless
{
    internal enum PowerTone { Boon, Mixed, Heavy }

    /// <summary>One draftable card. <see cref="Apply"/> does the work; if the card needs
    /// the player to choose a girl it opens its own picker and calls <paramref name="done"/>
    /// when finished (the draft has already closed by then).</summary>
    internal sealed class PowerUp
    {
        internal readonly string Id;
        internal readonly string Title;
        internal readonly string Desc;
        internal readonly PowerTone Tone;
        internal readonly int Weight;
        internal readonly Func<bool> CanOffer;
        internal readonly Action<PuzzleStatus, Action> Apply;

        internal PowerUp(string id, string title, string desc, PowerTone tone, int weight,
            Action<PuzzleStatus, Action> apply, Func<bool> canOffer = null)
        {
            Id = id; Title = title; Desc = desc; Tone = tone; Weight = weight;
            Apply = apply; CanOffer = canOffer ?? (() => true);
        }
    }
}

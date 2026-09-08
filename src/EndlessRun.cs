using System.Collections.Generic;

namespace HuniepopEndless
{
    /// <summary>
    /// Whole-run state for one Endless run. Everything here is process memory only —
    /// nothing is ever serialised. Cleared by <see cref="Reset"/> when the run ends.
    /// </summary>
    internal static class EndlessRun
    {
        /// <summary>True from the moment the player confirms "Start" on the launch
        /// screen until they are back on the title screen. Gates the save-suppression
        /// patches and every run hook.</summary>
        internal static bool Active;

        /// <summary>Set the instant "Start" is pressed, consumed by RunBootstrap when
        /// MainScene finishes loading. Distinguishes an Endless launch from anything
        /// else that could load MainScene.</summary>
        internal static bool PendingLaunch;

        /// <summary>Save slot index we scribble the synthetic PlayerFile into. Slot 3
        /// is the last of the 4 real slots; we restore whatever was there on cleanup
        /// (it is only a copy in memory anyway).</summary>
        internal const int ScratchSlot = 3;

        /// <summary>Number of dates fully cleared this run. Drives the move formula and
        /// the daytime/location cycle (date N happens at daytime slot N % 4).</summary>
        internal static int DatesCompleted;

        /// <summary>Difficulty the player picked on the launch screen.</summary>
        internal static SettingDifficulty Difficulty = SettingDifficulty.NORMAL;

        /// <summary>Set by the NextRound hook when a new date's puzzle is set up but not
        /// yet playable; consumed by the draft pump in <see cref="EndlessPlugin.Update"/>.</summary>
        internal static bool PendingDraft;

        /// <summary>True while the power-up draft (or any modal) owns the screen.</summary>
        internal static bool ModalOpen;

        /// <summary>Set when a run ends; tells the TitleScene load to silence the date
        /// music that carried over on the persistent camera. Not cleared by Reset —
        /// cleared by the scene handler itself.</summary>
        internal static bool ReturningFromRun;

        /// <summary>Per-run modifier accumulator (boons + banes from drafted power-ups).</summary>
        internal static RunModifiers Mods = new RunModifiers();

        /// <summary>Girls flagged as "likes broken hearts" for this run (detected from
        /// their baggage definitions at roster build time).</summary>
        internal static readonly HashSet<GirlDefinition> HeartbreakLovers = new HashSet<GirlDefinition>();

        /// <summary>Permanent baggage inflicted on SPECIFIC girls by perma-baggage
        /// power-ups. Keyed by the girl; re-applied every time she appears on a date.
        /// Drawn from the whole game-wide baggage pool, not her own native set.</summary>
        internal static readonly Dictionary<GirlDefinition, List<AilmentDefinition>> PermaBaggage =
            new Dictionary<GirlDefinition, List<AilmentDefinition>>();

        /// <summary>Permanent passion-meter cap penalty per girl (10 per overflow
        /// perma-baggage once she's at the baggage cap). Passion clamps to 100 - this.</summary>
        internal static readonly Dictionary<GirlDefinition, int> PassionPenalty =
            new Dictionary<GirlDefinition, int>();

        /// <summary>Date gifts permanently attached to a specific girl by a gift power-up.
        /// Re-added to her tray every date she appears on.</summary>
        internal static readonly Dictionary<GirlDefinition, List<ItemDefinition>> PermaGifts =
            new Dictionary<GirlDefinition, List<ItemDefinition>>();

        internal static void Reset()
        {
            Active = false;
            PendingLaunch = false;
            DatesCompleted = 0;
            Difficulty = SettingDifficulty.NORMAL;
            Mods = new RunModifiers();
            HeartbreakLovers.Clear();
            PermaBaggage.Clear();
            PassionPenalty.Clear();
            PermaGifts.Clear();
            PendingDraft = false;
            ModalOpen = false;
        }
    }
}

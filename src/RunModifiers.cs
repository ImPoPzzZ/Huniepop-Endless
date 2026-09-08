using System.Collections.Generic;

namespace HuniepopEndless
{
    /// <summary>
    /// Accumulated whole-run effects from drafted power-ups. One instance lives on
    /// <see cref="EndlessRun.Mods"/> for the duration of a run. The values here are
    /// read by the Harmony hooks in <see cref="RunHooks"/> and applied at the right
    /// points in the date lifecycle.
    ///
    /// "(date)" one-shot effects are NOT stored here — they are applied immediately by
    /// <see cref="PowerUpCatalog"/> when the card resolves and pushed onto
    /// <see cref="PendingDateEffects"/> only when they need to run at puzzle start.
    /// </summary>
    internal sealed class RunModifiers
    {
        // ---- Moves -------------------------------------------------------------
        /// <summary>Flat moves added at the start of every future date (mv_run, b_mv_run).</summary>
        internal int MovesPerDate;
        /// <summary>Extra moves per already-cleared date, retroactive (mv_perdate).</summary>
        internal int MovesPerClearedDate;
        /// <summary>One-shot move penalty to subtract from the next date only (b_mv_here,
        /// moves_debt). Reset to 0 once applied.</summary>
        internal int MoveDebtThisDate;
        /// <summary>One-shot move bonus to add to the next date only (mv_here). Reset after use.</summary>
        internal int MoveBonusThisDate;

        // ---- Goal ------------------------------------------------------------
        /// <summary>Percent added to every future affection goal (b_goal_up). 0.10 = +10%.</summary>
        internal float GoalPercentAdd;

        /// <summary>How many power-token boon cards have been taken (cap 3).</summary>
        internal int PowerTokenBoons;

        // ---- Token board weights (percent, applied at puzzle start) -----------
        internal float PassionWeightPct;   // pass_tokens (+), b_pass_down (-)
        internal float SentimentWeightPct; // sent_weight
        internal float BrokenWeightPct;    // b_broken

        // ---- Built-in puzzle offsets (re-asserted each date) -----------------
        internal int PowerTokenChance;     // power_chance
        internal int PowerTokenMultiplier; // power_mult
        internal int ExhaustedRecovery;    // stam_recovery
        internal int DateGiftUseCost;      // gift_free (-), b_gift_cost (+)

        // ---- Stamina --------------------------------------------------------
        /// <summary>Girls start every future date at this stamina instead of 4 (b_stam_lowstart).</summary>
        internal int StartStaminaOverride = -1;
        /// <summary>Focused girl loses 1 stamina every N moves for the run (b_stam_drain). 0 = off.</summary>
        internal int StaminaDrainEveryMoves;

        // (Permanent baggage now lives on EndlessRun.PermaBaggage — inflicted on
        //  specific girls, not a global count. See BaggageService.)

        // ---- Recurring date gifts ------------------------------------------
        /// <summary>Date gifts to (re)grant a specific girl at the start of every date.</summary>
        internal readonly List<RecurringGift> RecurringGifts = new List<RecurringGift>();

        // ---- One-shot effects queued for the next puzzle start --------------
        internal readonly List<System.Action> PendingDateEffects = new List<System.Action>();

        /// <summary>Human-readable list of every run-long effect the player has picked up,
        /// shown in the corner HUD. Card code appends a short line when it resolves.</summary>
        internal readonly List<string> ActiveEffects = new List<string>();

        internal void LogEffect(string line)
        {
            ActiveEffects.Add(line);
            EndlessPlugin.Log.LogInfo("[effect] " + line);
        }

        /// <summary>Token-weight shifts that only last the current date — reverted at the
        /// start of the next date. (resourceType, affectionType, signedPercent).</summary>
        internal readonly List<TokenShift> DateTokenReverts = new List<TokenShift>();

        internal struct TokenShift
        {
            internal PuzzleResourceType Rt;
            internal PuzzleAffectionType Aff;
            internal float Pct;      // positive = was increased, so revert = decrease
        }

        internal struct RecurringGift
        {
            internal GirlDefinition Girl;
            internal ItemDefinition Item;
        }
    }
}

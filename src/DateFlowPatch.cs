using HarmonyLib;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>
    /// The per-date lifecycle: count cleared dates, apply the goal ramp, rewrite the
    /// move total, cycle the location, fire the power-up draft, and send a failed run
    /// back to the title instead of the (non-existent) hub.
    /// </summary>
    [HarmonyPatch]
    internal static class DateFlowPatch
    {
        // ---- Straight-to-date: on the run's first arrival, swap whatever location the
        //      synthetic file had for a real cyclable Morning date location. This is
        //      also what keeps us OUT of the tutorial date ("Airplane Bathroom"), which
        //      is a DATE-type location but forces tutorial mode. -----------------------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocationManager), nameof(LocationManager.Arrive))]
        private static void Arrive_Prefix(LocationManager __instance, ref LocationDefinition locationDef)
        {
            if (!EndlessRun.Active || !EndlessRun.PendingLaunch) return;
            EndlessRun.PendingLaunch = false;
            var loc = RunBootstrap.CyclableDateLocation(__instance, 0);
            if (loc != null)
            {
                locationDef = loc;
                Game.Persistence.playerFile.locationDefinition = loc;
                Game.Persistence.playerFile.daytimeElapsed = 0;
                EndlessPlugin.Log.LogInfo("First arrival redirected to date location: " + loc.name);
            }
        }

        // ---- HP2's round-clear check is `affection == goal` (exact). If any of our
        //      cards or the goal ramp ever leaves affection sitting ABOVE the goal, that
        //      equality can never be true and a won date reads as a loss. Snap it down. -
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "CheckRoundOver")]
        private static void CheckRoundOver_Prefix(UiPuzzleGrid __instance)
        {
            if (!EndlessRun.Active) return;
            try
            {
                var st = Reflect.Get<PuzzleStatus>(__instance, "_status");
                if (st != null && !Game.Session.Puzzle.dateForfeited
                    && st.affection >= st.affectionGoal && st.affection != st.affectionGoal)
                    Reflect.Set(st, "_affection", st.affectionGoal);
            }
            catch { }
        }

        // ---- Failure: Non-Stop EndPuzzle normally departs to the hub. --------------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PuzzleManager), "EndPuzzle")]
        private static bool EndPuzzle_Prefix(PuzzleManager __instance)
        {
            if (!EndlessRun.Active) return true;
            try
            {
                var grid = __instance.puzzleGrid;
                var st = __instance.puzzleStatus;
                EndlessPlugin.Log.LogInfo($"[END] EndPuzzle — roundState={grid?.roundState} forfeited={__instance.dateForfeited} " +
                    $"affection={st?.affection}/{st?.affectionGoal} moves={st?.movesRemaining} " +
                    $"staL={st?.girlStatusLeft?.stamina} staR={st?.girlStatusRight?.stamina} " +
                    $"exhL={st?.girlStatusLeft?.exhausted} exhR={st?.girlStatusRight?.exhausted} gameOver={st?.gameOver}");
            }
            catch { }
            RunBootstrap.EndRun();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UiPuzzleGrid), "CheckRoundOver")]
        private static void CheckRoundOver_Postfix(UiPuzzleGrid __instance)
        {
            if (!EndlessRun.Active) return;
            try
            {
                if (!__instance.roundOver) return;
                var st = Reflect.Get<PuzzleStatus>(__instance, "_status");
                EndlessPlugin.Log.LogInfo($"[round] CheckRoundOver -> {__instance.roundState}  aff={st?.affection}/{st?.affectionGoal} moves={st?.movesRemaining}");
            }
            catch { }
        }

        // ---- New round = a new date. NextRound runs once from Reset (date 1) and once
        //      per cleared date after. roundIndex == dates cleared. -------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PuzzleStatus), nameof(PuzzleStatus.NextRound))]
        private static void NextRound_Postfix(PuzzleStatus __instance)
        {
            if (!EndlessRun.Active) return;
            try
            {
                var gl = __instance.girlStatusLeft?.girlDefinition?.name ?? "?";
                var gr = __instance.girlStatusRight?.girlDefinition?.name ?? "?";
                EndlessPlugin.Log.LogInfo($"[date] NextRound status={__instance.statusType} round={__instance.roundIndex} bonus={__instance.bonusRound} moves(before)={__instance.movesRemaining} girls={gl}+{gr}");
                if (__instance.statusType != PuzzleStatusType.NONSTOP) return;
                if (__instance.bonusRound) return;

                int roundIndex = Mathf.Max(0, __instance.roundIndex);
                EndlessRun.DatesCompleted = roundIndex;

                // Replace HP2's native Non-Stop goal (250 + 250·N, brutal from date 1
                // with our not-maxed girls) with a gentler curve, then apply card ramp.
                Reflect.Set(__instance, "_affectionGoal", BaseGoal(roundIndex));
                ApplyGoalRamp(__instance);
                RunHooks.OnDateSetup(__instance);

                // Moves: leave HP2's native Non-Stop economy alone (10 base + carry-over
                // + move-token gains). Only add the small permanent per-date bonus from
                // drafted cards, capped.
                int perDate = Mathf.Clamp(EndlessRun.Mods.MovesPerDate, -8, MovesPerDateCap);
                if (perDate != 0) __instance.AddResourceValue(PuzzleResourceType.MOVES, perDate, false);

                if (roundIndex > 0)
                    RunBootstrap.AdvanceLocationForRound(roundIndex);

                EndlessRun.PendingDraft = true;
            }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogError("NextRound_Postfix: " + e);
            }
        }

        /// <summary>Hard cap on the total permanent "+N moves every date" bonus.</summary>
        internal const int MovesPerDateCap = 5;

        /// <summary>Affection goal for date N (0-based). Climbs, then accelerates:
        /// 115, 190, 271, 358, 451, 550, 655, 766 …</summary>
        internal static int BaseGoal(int n) => Mathf.RoundToInt(115 + 72 * n + 3f * n * n);

        private static void ApplyGoalRamp(PuzzleStatus status)
        {
            float pct = EndlessRun.Mods.GoalPercentAdd;
            if (pct <= 0f) return;
            int goal = status.affectionGoal;
            Reflect.Set(status, "_affectionGoal", Mathf.RoundToInt(goal * (1f + pct)));
        }

        /// <summary>Called by PowerUpDraft once the chosen card has been applied.</summary>
        internal static void OnDraftResolved()
        {
            var status = Hp2.Status;
            var m = EndlessRun.Mods;

            // Apply this-date move deltas live, on top of whatever HP2 gave.
            int delta = m.MoveBonusThisDate - m.MoveDebtThisDate;
            if (delta != 0 && status != null)
                status.AddResourceValue(PuzzleResourceType.MOVES, delta, false);
            m.MoveBonusThisDate = 0;
            m.MoveDebtThisDate = 0;

            // Cards that touch passion / sentiment / stamina / affection set the data but
            // HP2 only repaints the meters on its next CheckChanges — force it now so the
            // effect shows immediately instead of "after the first move".
            try
            {
                status?.CheckChanges();
                Game.Session.gameCanvas.header.Refresh(hard: true);
            }
            catch { }

            try { Game.Session.gameCanvas.puzzleGrid.dateGiftsContainer.PopulateSlots(); } catch { }
            EndlessPlugin.Log.LogInfo($"Date {EndlessRun.DatesCompleted}: moves = {status?.movesRemaining}, goal = {status?.affectionGoal}");
        }
    }
}

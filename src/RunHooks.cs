using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>
    /// Enforces whole-run modifiers that HuniePop 2 rebuilds every round and would
    /// otherwise wipe: permanent baggage and recurring date gifts. Everything that
    /// naturally persists across Non-Stop rounds (token weights, puzzle offsets,
    /// PlayerFile levels) is applied once at draft time and not touched here.
    /// </summary>
    [HarmonyPatch]
    internal static class RunHooks
    {
        /// <summary>Called from the NextRound postfix once the new date's girls exist.</summary>
        internal static void OnDateSetup(PuzzleStatus status)
        {
            var m = EndlessRun.Mods;

            if (m.StartStaminaOverride >= 0)
            {
                SetStamina(status.girlStatusLeft, m.StartStaminaOverride);
                SetStamina(status.girlStatusRight, m.StartStaminaOverride);
            }

            // Revert last date's "this date only" token-weight shifts.
            foreach (var sh in m.DateTokenReverts)
                Hp2.ShiftTokenWeight(status, sh.Rt, sh.Aff, Mathf.Abs(sh.Pct), decrease: sh.Pct > 0f);
            m.DateTokenReverts.Clear();

            // Queued one-shot "(date)" effects from the last draft.
            foreach (var act in m.PendingDateEffects)
            {
                try { act(); } catch (System.Exception e) { EndlessPlugin.Log.LogError("PendingDateEffect: " + e); }
            }
            m.PendingDateEffects.Clear();
        }

        private static void SetStamina(PuzzleStatusGirl g, int value)
        {
            if (g == null) return;
            g.stamina = Mathf.Clamp(value, 0, 6);
        }

        // ---- Permanent baggage: whenever a girl's ailments are rebuilt for a date,
        //      re-apply whatever perma-baggage has been inflicted on HER specifically. --
        [HarmonyPatch(typeof(PuzzleStatusGirl), nameof(PuzzleStatusGirl.PopulateAilments))]
        [HarmonyPostfix]
        private static void PopulateAilments_Postfix(PuzzleStatusGirl __instance)
        {
            if (!EndlessRun.Active) return;
            try
            {
                var def = __instance.girlDefinition;
                bool has = def != null && EndlessRun.PermaBaggage.TryGetValue(def, out var l) && l.Count > 0;
                EndlessPlugin.Log.LogInfo($"[baggage] PopulateAilments {def?.name}: native+active={__instance.ailments.Count} inflicted={(has ? EndlessRun.PermaBaggage[def].Count : 0)}");
                BaggageService.ReapplyFor(__instance);
                if (has) Hp2.RefreshAilmentTray();
            }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogError("PopulateAilments_Postfix: " + e);
            }
        }

        // ---- Permanent passion-cap penalty (baggage overflow). --------------------
        [HarmonyPatch(typeof(PuzzleStatusGirl), "set_passion")]
        [HarmonyPostfix]
        private static void SetPassion_Postfix(PuzzleStatusGirl __instance)
        {
            if (!EndlessRun.Active) return;
            var def = __instance.girlDefinition;
            if (def == null || !EndlessRun.PassionPenalty.TryGetValue(def, out int pen) || pen <= 0) return;
            int cap = Mathf.Max(0, 100 - pen);
            if (__instance.passion > cap)
                Reflect.Set(__instance, "_passion", cap);
        }

        // ---- Permanent date gifts: re-add each girl's attached gifts every date. ----
        [HarmonyPatch(typeof(PuzzleStatus), nameof(PuzzleStatus.PopulateDateGifts))]
        [HarmonyPostfix]
        private static void PopulateDateGifts_Postfix(PuzzleStatus __instance)
        {
            if (!EndlessRun.Active) return;
            try
            {
                foreach (var g in Hp2.ActiveGirls(__instance))
                {
                    if (!EndlessRun.PermaGifts.TryGetValue(g.girlDefinition, out var gifts) || gifts.Count == 0) continue;
                    int added = 0;
                    foreach (var item in gifts)
                    {
                        if (g.dateGifts.Count >= 4) break;
                        g.dateGifts.Add(new PuzzleStatusDateGift(item));
                        added++;
                    }
                    EndlessPlugin.Log.LogInfo($"[gift] re-applied {added} permanent gift(s) to {g.girlDefinition.name}");
                }
            }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogError("PopulateDateGifts_Postfix: " + e);
            }
        }
    }
}

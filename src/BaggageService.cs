using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>
    /// Permanent baggage inflicted by power-ups. When you draft a perma-baggage card,
    /// the TWO girls currently on the date each get +1 baggage — a random one from the
    /// whole game-wide baggage pool — and it's remembered against those specific girls
    /// so it comes back every time they reappear. A girl caps at 4 baggage; past that,
    /// further perma-baggage becomes a permanent −10 passion-meter penalty for her.
    /// </summary>
    internal static class BaggageService
    {
        internal const int Cap = 3;

        private static List<AilmentDefinition> _pool;
        private static List<AilmentDefinition> Pool
        {
            get
            {
                if (_pool != null) return _pool;
                _pool = new List<AilmentDefinition>();
                foreach (var it in Game.Data.Items.GetAllOfType(ItemType.BAGGAGE))
                    if (it != null && it.ailmentDefinition != null && !_pool.Contains(it.ailmentDefinition))
                        _pool.Add(it.ailmentDefinition);
                EndlessPlugin.Log.LogInfo("Baggage pool size = " + _pool.Count);
                return _pool;
            }
        }

        /// <summary>Called when a perma-baggage power-up resolves: hit the current pair.</summary>
        internal static void InflictOnCurrentPair()
        {
            var status = Hp2.Status;
            if (status == null) { EndlessPlugin.Log.LogWarning("InflictOnCurrentPair: no puzzle status"); return; }

            foreach (var g in Hp2.ActiveGirls(status))
                InflictOn(g);

            Hp2.RefreshAilmentTray();
        }

        private static void InflictOn(PuzzleStatusGirl g)
        {
            var def = g.girlDefinition;
            if (def == null) return;

            var have = EndlessRun.PermaBaggage.TryGetValue(def, out var list) ? list : (EndlessRun.PermaBaggage[def] = new List<AilmentDefinition>());

            // Cap is on baggage we've INFLICTED (the girl's own native list isn't active
            // in Endless mode, so it doesn't count toward the limit).
            if (have.Count >= Cap)
            {
                int pen = EndlessRun.PassionPenalty.TryGetValue(def, out var p) ? p : 0;
                EndlessRun.PassionPenalty[def] = pen + 10;
                EndlessPlugin.Log.LogInfo($"Baggage: {def.name} at cap ({have.Count}) -> permanent passion penalty now {pen + 10}");
                return;
            }

            var candidates = Pool.Where(a => !have.Contains(a)).ToList();
            if (candidates.Count == 0) { EndlessPlugin.Log.LogWarning("Baggage: no candidates for " + def.name); return; }

            var pick = candidates[Random.Range(0, candidates.Count)];
            have.Add(pick);
            EndlessPlugin.Log.LogInfo($"Baggage: {def.name} gains permanent '{pick.name}' ({have.Count}/{Cap} inflicted)");

            // Apply to the live date immediately.
            if (!g.HasAilment(pick) && g.ailments.Count < Cap) g.ApplyAilment(pick);
        }

        /// <summary>Re-apply a girl's inflicted baggage for the current date (called from
        /// the PopulateAilments postfix).</summary>
        internal static void ReapplyFor(PuzzleStatusGirl g)
        {
            var def = g.girlDefinition;
            if (def == null || !EndlessRun.PermaBaggage.TryGetValue(def, out var list) || list.Count == 0) return;
            int added = 0;
            foreach (var a in list)
            {
                if (g.ailments.Count >= Cap) break;
                if (!g.HasAilment(a)) { g.ApplyAilment(a); added++; }
            }
            EndlessPlugin.Log.LogInfo($"[baggage] re-applied {added}/{list.Count} permanent baggage to {def.name}");
        }
    }
}

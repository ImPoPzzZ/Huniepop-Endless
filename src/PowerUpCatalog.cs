using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>
    /// Every draftable power-up, plus the weighted "pick 3 distinct valid cards" draw.
    /// Effects tagged (date) act on the live puzzle now; (run) effects either mutate
    /// <see cref="RunModifiers"/> for future dates or make a change that Non-Stop mode
    /// naturally carries forward (token weights, puzzle offsets, PlayerFile levels).
    /// </summary>
    internal static class PowerUpCatalog
    {
        private static List<PowerUp> _all;

        internal static List<PowerUp> All => _all ?? (_all = Build());

        internal static List<PowerUp> DrawThree()
        {
            var pool = All.Where(p => Safe(p.CanOffer)).ToList();
            EndlessPlugin.Log.LogInfo($"[draft] {pool.Count}/{All.Count} cards eligible");
            var picks = new List<PowerUp>();
            var rng = new System.Random();
            while (picks.Count < 3 && pool.Count > 0)
            {
                int total = pool.Sum(p => p.Weight);
                int roll = rng.Next(total);
                PowerUp chosen = pool[pool.Count - 1];
                foreach (var p in pool)
                {
                    roll -= p.Weight;
                    if (roll < 0) { chosen = p; break; }
                }
                picks.Add(chosen);
                pool.Remove(chosen);
            }
            EndlessPlugin.Log.LogInfo("[draft] offered: " + string.Join(", ", picks.Select(p => p.Id)));
            return picks;
        }

        private static bool Safe(Func<bool> f) { try { return f(); } catch { return false; } }

        private static PowerUp P(string id, string title, string desc, PowerTone tone, int weight,
            Action<PuzzleStatus, Action> apply, Func<bool> can = null)
        {
            Action<PuzzleStatus, Action> logged = (s, done) =>
            {
                EndlessPlugin.Log.LogInfo($"[card] applying '{id}'  (goal={s?.affectionGoal} moves={s?.movesRemaining})");
                apply(s, () =>
                {
                    EndlessPlugin.Log.LogInfo($"[card] '{id}' done  (goal={s?.affectionGoal} moves={s?.movesRemaining} "
                        + $"pL={s?.girlStatusLeft?.passion} pR={s?.girlStatusRight?.passion} "
                        + $"aff={s?.affection})");
                    done();
                });
            };
            return new PowerUp(id, title, desc, tone, weight, logged, can);
        }

        private static void AddPerDateMoves(int n)
        {
            var m = EndlessRun.Mods;
            m.MovesPerDate = Mathf.Min(DateFlowPatch.MovesPerDateCap, m.MovesPerDate + n);
        }

        private static void Both(PuzzleStatus s, PuzzleResourceType rt, int v)
        {
            s.AddResourceValue(rt, v, false);
            s.AddResourceValue(rt, v, true);
        }

        private static readonly PuzzleAffectionType[] Affs =
        {
            PuzzleAffectionType.TALENT, PuzzleAffectionType.FLIRTATION,
            PuzzleAffectionType.ROMANCE, PuzzleAffectionType.SEXUALITY
        };

        private static List<PowerUp> Build()
        {
            var l = new List<PowerUp>();
            var m = EndlessRun.Mods;

            // ---- Moves -------------------------------------------------------
            l.Add(P("mv_here", "Second Wind", "+5 moves, this date only.", PowerTone.Boon, 10,
                (s, done) => { m.MoveBonusThisDate += 5; done(); }));

            l.Add(P("mv_run", "Steady Pace", "+2 moves at the start of every future date (max +5 total).\nDrawback: both girls on this date gain 1 permanent random baggage.",
                PowerTone.Mixed, 7, (s, done) => { AddPerDateMoves(2); m.LogEffect("+2 moves/date"); BaggageService.InflictOnCurrentPair(); done(); }));

            // ---- Affection openers ----------------------------------------
            l.Add(P("aff_headstart", "Head Start", "Begin this date with the affection meter 20% filled.\nDrawback: −4 moves this date.",
                PowerTone.Mixed, 9, (s, done) =>
                {
                    s.AddResourceValue(PuzzleResourceType.AFFECTION, Mathf.RoundToInt(s.affectionGoal * 0.20f), false);
                    m.MoveDebtThisDate += 4;
                    done();
                }));

            l.Add(P("aff_bighead", "Running Start", "Begin this date with the affection meter 30% filled.", PowerTone.Boon, 7,
                (s, done) =>
                {
                    s.AddResourceValue(PuzzleResourceType.AFFECTION, Mathf.RoundToInt(s.affectionGoal * 0.30f), false);
                    done();
                }));

            // ---- Sentiment / passion meters ------------------------------
            l.Add(P("sent_start", "Open Up", "Both girls start this date with +5 sentiment.", PowerTone.Boon, 9,
                (s, done) => { Both(s, PuzzleResourceType.SENTIMENT, 5); done(); }));

            l.Add(P("pass_start", "Warm Welcome", "Both girls start this date with +25 passion.", PowerTone.Boon, 9,
                (s, done) => { Both(s, PuzzleResourceType.PASSION, 25); done(); }));

            // ---- Stamina --------------------------------------------------
            l.Add(P("stam_now", "Caffeine", "+3 stamina to both girls right now.", PowerTone.Boon, 9,
                (s, done) => { Both(s, PuzzleResourceType.STAMINA, 3); done(); }));

            l.Add(P("stam_recovery", "Second Nature", "Exhausted girls bounce back faster from matches, for the rest of the run.\nDrawback: girls start every future date at 3 stamina instead of 4.",
                PowerTone.Mixed, 6, (s, done) => { Hp2.Offset("exhausted_recovery_rate", 2); m.StartStaminaOverride = 3; m.LogEffect("Faster exhaustion recovery; start dates at 3 stamina"); done(); }));

            // ---- Power tokens (the sparkly upgraded ones — huge affection). Capped:
            //      after 3 of these across the run they stop being offered. ------------
            l.Add(P("power_chance", "Sparks Fly",
                "Matches turn into Power Tokens more often, for the rest of the run.\nDrawback: both girls on this date gain 1 permanent random baggage.",
                PowerTone.Mixed, 6, (s, done) => { Hp2.Offset("power_token_chance", 3); m.PowerTokenBoons++; m.LogEffect("Power Tokens spawn more often"); BaggageService.InflictOnCurrentPair(); done(); },
                () => m.PowerTokenBoons < 3));

            l.Add(P("power_mult", "Big Feelings",
                "Power Tokens score even more, for the rest of the run.\nDrawback: every future affection goal is 10% higher.",
                PowerTone.Mixed, 6, (s, done) => { Hp2.Offset("power_token_multiplier", 3); m.PowerTokenBoons++; m.GoalPercentAdd += 0.10f; m.LogEffect("Power Tokens score more; goals +10%"); done(); },
                () => m.PowerTokenBoons < 3));

            // ---- Token tuning family (+/- 20% weight of one token type) ----
            AddTokenTuning(l);

            // ---- Permanent affection levels ------------------------------
            foreach (var aff in Affs)
            {
                var a = aff; string name = Hp2.Titleize(a);
                l.Add(P("lvl_" + a.ToString().ToLower(), name + " Mastery",
                    "+1 permanent " + name + " level — every " + name + " match scores more, for good.\nDrawback: −6 moves on this date.",
                    PowerTone.Mixed, 7, (s, done) =>
                    {
                        Game.Persistence.playerFile.AddAffectionLevelExp(a, 6);
                        m.LogEffect("+1 permanent " + name + " level");
                        m.MoveDebtThisDate += 6;
                        done();
                    }));
            }

            // ---- Baggage trades -----------------------------------------
            l.Add(P("bag_all_moves", "Emotional Baggage", "+3 moves at the start of every future date (max +5 total).\nDrawback: both girls on this date gain 1 permanent random baggage.",
                PowerTone.Heavy, 5, (s, done) => { AddPerDateMoves(3); m.LogEffect("+3 moves/date"); BaggageService.InflictOnCurrentPair(); done(); }));

            l.Add(P("bag_clear_date", "Clean Slate", "Clears both girls' baggage for this date only.", PowerTone.Boon, 6,
                (s, done) =>
                {
                    foreach (var g in Hp2.ActiveGirls(s)) g.ailments.Clear();
                    Hp2.RefreshAilmentTray();
                    done();
                },
                () => Hp2.Status != null && System.Linq.Enumerable.Any(Hp2.ActiveGirls(Hp2.Status), g => g.ailments.Count > 0)));

            // ---- Date gifts -------------------------------------------
            foreach (var aff in Affs)
            {
                var a = aff; string name = Hp2.Titleize(a);
                l.Add(P("gift_" + a.ToString().ToLower(), name + " Gift",
                    "A random " + name + " date gift, attached to a girl you choose — permanently. It's in her tray every date; drag it in when you want.",
                    PowerTone.Boon, 12,
                    (s, done) => GiftPicker.Run(1, a, done),
                    () => Hp2.RandomDateGift(a) != null && Hp2.AnyGiftSlotFree()));
            }
            l.Add(P("gift_any", "Care Package",
                "A random date gift of any kind, attached to a girl you choose — permanently.",
                PowerTone.Boon, 10, (s, done) => GiftPicker.Run(1, null, done),
                () => Hp2.RandomDateGift(null) != null && Hp2.AnyGiftSlotFree()));
            l.Add(P("gift_bundle", "Gift Basket",
                "Three random date gifts — assign each to a girl, permanently.",
                PowerTone.Boon, 7, (s, done) => GiftPicker.Run(3, null, done),
                () => Hp2.RandomDateGift(null) != null && Hp2.AnyGiftSlotFree()));
            l.Add(P("gift_free", "Generous", "Every date gift costs 2 less sentiment to give, for the run.", PowerTone.Boon, 5,
                (s, done) => { Hp2.Offset("date_gift_use_cost", -2); m.LogEffect("Date gifts cost 2 less sentiment"); done(); }));

            return l;
        }

        private sealed class TokenKind
        {
            internal readonly PuzzleResourceType Rt;
            internal readonly PuzzleAffectionType Aff;
            internal readonly string Name;
            internal TokenKind(PuzzleResourceType rt, PuzzleAffectionType aff, string name)
            { Rt = rt; Aff = aff; Name = name; }
        }

        // Token types the tuning family can nudge, with display names.
        private static readonly TokenKind[] TokenKinds =
        {
            new TokenKind(PuzzleResourceType.AFFECTION, PuzzleAffectionType.TALENT,     "Talent"),
            new TokenKind(PuzzleResourceType.AFFECTION, PuzzleAffectionType.FLIRTATION, "Flirtation"),
            new TokenKind(PuzzleResourceType.AFFECTION, PuzzleAffectionType.ROMANCE,    "Romance"),
            new TokenKind(PuzzleResourceType.AFFECTION, PuzzleAffectionType.SEXUALITY,  "Sexuality"),
            new TokenKind(PuzzleResourceType.PASSION,   PuzzleAffectionType.TALENT,     "Passion"),
            new TokenKind(PuzzleResourceType.SENTIMENT, PuzzleAffectionType.TALENT,     "Sentiment"),
            new TokenKind(PuzzleResourceType.STAMINA,   PuzzleAffectionType.TALENT,     "Stamina"),
        };

        /// <summary>Token tuning — affection types only, driven by HP2's real scented
        /// candles. A candle natively boosts one affection type's tokens +50% for the
        /// date; we apply it directly (buff) or apply-then-block it and stack a −50%
        /// cut (debuff). Both show as candle icons in the effects strip.</summary>
        private static void AddTokenTuning(List<PowerUp> l)
        {
            var m = EndlessRun.Mods;
            foreach (var aff in Affs)
            {
                var a = aff; string n = Hp2.Titleize(a);

                l.Add(P("tok_" + n.ToLower() + "_candle", n + " Candle",
                    n + " tokens fall 50% more often — this date. (Lights a " + n + " candle.)",
                    PowerTone.Boon, 5,
                    (s, done) => { Hp2.ApplyCandle(a, false); done(); }));

                l.Add(P("tok_" + n.ToLower() + "_trade", n + " Bias",
                    n + " tokens +50% this date — but a random other affection type −50% this date. "
                    + "Two candles: one lit, one blocked.",
                    PowerTone.Mixed, 4, (s, done) =>
                    {
                        Hp2.ApplyCandle(a, false);
                        var others = Affs.Where(x => x != a).ToArray();
                        var other = others[UnityEngine.Random.Range(0, others.Length)];
                        Hp2.ApplyCandle(other, true);
                        done();
                    }));
            }
        }

        /// <summary>Shift a token, honoring the passion↔broken-hearts flip for a
        /// heartbreak-lover in the current pair.</summary>
        private static void ShiftFor(PuzzleStatus s, PuzzleResourceType rt, PuzzleAffectionType aff,
            float pct, bool dateOnly)
        {
            bool lover = Hp2.ActiveGirls(s).Any(g => HeartbreakLover.IsLover(g.girlDefinition));
            if (lover && rt == PuzzleResourceType.PASSION) rt = PuzzleResourceType.SENTIMENT;
            else if (lover && rt == PuzzleResourceType.SENTIMENT) rt = PuzzleResourceType.PASSION;
            Hp2.ApplyTokenShift(s, rt, aff, pct, dateOnly);
        }

        private static PuzzleAffectionType Other(PuzzleAffectionType a)
        {
            var pool = Affs.Where(x => x != a).ToArray();
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
    }
}

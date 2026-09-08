using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>Convenience wrappers over HuniePop 2 puzzle internals used by power-ups.</summary>
    internal static class Hp2
    {
        internal static PuzzleStatus Status => Game.Session?.Puzzle?.puzzleStatus;

        internal static IEnumerable<PuzzleStatusGirl> ActiveGirls(PuzzleStatus s)
        {
            if (s?.girlStatusLeft != null) yield return s.girlStatusLeft;
            if (s?.girlStatusRight != null) yield return s.girlStatusRight;
        }

        /// <summary>Shift a token type's max spawn weight by a percentage, board-wide.
        /// Persists for the rest of the Non-Stop run.</summary>
        internal static void ShiftTokenWeight(PuzzleStatus s, PuzzleResourceType rt,
            PuzzleAffectionType at, float pct, bool decrease)
        {
            if (s?.tokenStatus == null) return;
            var target = Game.Data.Tokens.GetByResourceType(rt, at);
            foreach (var tok in s.tokenStatus)
            {
                if (tok.tokenDefinition == target || (target == null && tok.tokenDefinition.resourceType == rt))
                {
                    tok.AdjustMaxWeight(pct, decrease);
                    return;
                }
            }
        }

        /// <summary>Shift a token type's weight by a signed percentage. When
        /// <paramref name="dateOnly"/> the shift is auto-reverted at the next date.</summary>
        internal static void ApplyTokenShift(PuzzleStatus s, PuzzleResourceType rt,
            PuzzleAffectionType aff, float signedPct, bool dateOnly)
        {
            ShiftTokenWeight(s, rt, aff, Mathf.Abs(signedPct), decrease: signedPct < 0f);
            if (dateOnly)
                EndlessRun.Mods.DateTokenReverts.Add(new RunModifiers.TokenShift
                { Rt = rt, Aff = aff, Pct = signedPct });
        }

        internal static void NudgeTokenWeight(PuzzleStatus s, PuzzleResourceType rt,
            PuzzleAffectionType at, int flat)
        {
            if (s?.tokenStatus == null) return;
            var target = Game.Data.Tokens.GetByResourceType(rt, at);
            foreach (var tok in s.tokenStatus)
                if (tok.tokenDefinition == target) { tok.AdjustCurrentWeight(flat); return; }
        }

        internal static void Offset(string name, int delta)
        {
            try { Game.Session?.Puzzle?.AdjustPuzzleOffset(name, delta); } catch { }
        }

        /// <summary>Permanently attach a date gift to one girl. It drops into her tray
        /// now and every future date she appears on (she drags it in-game when she
        /// wants). Refreshes the on-screen tray.</summary>
        internal static void GrantDateGift(PuzzleStatusGirl girl, ItemDefinition item)
        {
            if (girl == null || item == null) return;
            var def = girl.girlDefinition;
            if (!EndlessRun.PermaGifts.TryGetValue(def, out var list))
                EndlessRun.PermaGifts[def] = list = new System.Collections.Generic.List<ItemDefinition>();
            list.Add(item);
            EndlessPlugin.Log.LogInfo($"[gift] {def.name} permanently gets '{item.itemName}' ({list.Count} total)");
            if (girl.dateGifts.Count < 4)
                girl.dateGifts.Add(new PuzzleStatusDateGift(item));
            RefreshGiftTray();
        }

        /// <summary>True if at least one active girl has a free date-gift slot.</summary>
        internal static bool AnyGiftSlotFree()
        {
            var s = Status;
            if (s == null) return false;
            foreach (var g in ActiveGirls(s)) if (g.dateGifts.Count < 4) return true;
            return false;
        }

        internal static void RefreshGiftTray()
        {
            try
            {
                var c = Game.Session.Puzzle.puzzleGrid.dateGiftsContainer;
                c.PopulateSlots();
                c.FocusSides(0f);
            }
            catch (System.Exception e) { EndlessPlugin.Log.LogWarning("RefreshGiftTray: " + e.Message); }
        }

        private static readonly System.Collections.Generic.List<string> _pendingNotes =
            new System.Collections.Generic.List<string>();

        /// <summary>On-doll notification. This is the primary feedback for token effects.
        /// If a modal (the draft) is up, time is frozen and the notification animation
        /// won't play — so it's queued and flushed by <see cref="FlushNotes"/> once the
        /// draft closes.</summary>
        internal static void Notify(string text, float seconds = 4f)
        {
            if (EndlessRun.ModalOpen) { _pendingNotes.Add(text); return; }
            ShowNote(text, seconds);
        }

        internal static void FlushNotes()
        {
            if (_pendingNotes.Count == 0) return;
            var joined = string.Join("\n", _pendingNotes);
            _pendingNotes.Clear();
            ShowNote(joined, 5f);
        }

        private static void ShowNote(string text, float seconds)
        {
            try
            {
                Game.Session.gameCanvas.GetDoll(false).notificationBox.Show(text, seconds);
                Game.Session.gameCanvas.GetDoll(true).notificationBox.Show(text, seconds);
            }
            catch { }
        }

        /// <summary>
        /// Instantly apply the candle for <paramref name="type"/> to the focused girl —
        /// the candle's ailment fires and shows in the active-effects strip. A buff
        /// candle is left enabled (doing its thing); a debuff candle is applied then
        /// disabled, so it shows greyed-out — present but doing nothing.
        /// </summary>
        internal static void ApplyCandle(PuzzleAffectionType type, bool isDebuff)
        {
            var s = Status;
            if (s == null) return;

            var candle = Game.Data.Items.GetAllOfType(ItemType.DATE_GIFT)
                .FirstOrDefault(g => g.dateGiftType == ItemDateGiftType.CANDLES && g.affectionType == type
                                     && g.ailmentDefinition != null);
            var girl = s.girlStatusFocused ?? s.girlStatusLeft;
            if (candle == null || girl == null)
            {
                Notify(Titleize(type) + " tokens " + (isDebuff ? "-" : "+") + "20%");
                return;
            }

            var def = candle.ailmentDefinition;
            if (!girl.HasAilment(def)) girl.ApplyAilment(def);   // adds + Enable() — candle's native +50% this date

            if (isDebuff)
            {
                // Block the candle (no +50%) AND stack a real -50% weight cut for the date.
                foreach (var ail in girl.ailments)
                    if (ail.definition == def) { ail.Disable(); break; }
                ApplyTokenShift(Status, PuzzleResourceType.AFFECTION, type, -0.50f, dateOnly: true);
            }

            RefreshAilmentTray();
            Notify(Titleize(type) + " tokens " + (isDebuff ? "-50% this date  (candle blocked)"
                                                          : "+50% this date  (" + candle.itemName + ")"));
            EndlessPlugin.Log.LogInfo($"[candle] {(isDebuff ? "debuff(blocked +-50%)" : "buff +50%")} {Titleize(type)} on {girl.girlDefinition.name} — '{candle.itemName}'");
        }

        internal static void RefreshAilmentTray()
        {
            try { Game.Session.Puzzle.puzzleGrid.ailmentsContainer.RePopulate(); }
            catch (System.Exception e) { EndlessPlugin.Log.LogWarning("RefreshAilmentTray: " + e.Message); }
        }

        internal static ItemDefinition RandomDateGift(PuzzleAffectionType? affection)
        {
            var pool = new List<ItemDefinition>();
            foreach (var it in Game.Data.Items.GetAllOfType(ItemType.DATE_GIFT))
            {
                if (it == null) continue;
                if (it.giveConditionType != ItemGiveConditionType.NONE) continue;
                if (it.difficultyExclusive) continue;   // would show "HARD only" and be unusable
                if (it.dateGiftAilment) continue;        // negative gift — not a boon
                if (affection.HasValue && it.affectionType != affection.Value) continue;
                pool.Add(it);
            }
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        internal static string Titleize(PuzzleAffectionType t)
        {
            var s = t.ToString().ToLower();
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HuniepopEndless
{
    /// <summary>
    /// The between-banter draft: blurred, click-blocking backdrop; three cards; every
    /// pick needs an "Are you sure?" confirm before it resolves.
    /// </summary>
    internal static class PowerUpDraft
    {
        private static Canvas _canvas;
        private static GameObject _root;
        private static bool _resolving;

        internal static bool IsOpen => _root != null;

        internal static void Open()
        {
            if (_root != null || _resolving) return;
            EndlessRun.ModalOpen = true;
            _resolving = false;
            TimeFreeze(true);

            if (_canvas == null) _canvas = UiKit.MakeOverlayCanvas("HE_Draft", 5500);
            _root = UiKit.MakeBackdrop(_canvas.transform, 0.5f);

            var panel = UiKit.Box(_root.transform, "panel", UiKit.Panel, Vector2.zero, new Vector2(1200f, 680f));

            UiKit.TextBox(panel, "head",
                "DATE " + (EndlessRun.DatesCompleted + 1) + " — DRAFT A POWER-UP",
                new Vector2(0f, 300f), new Vector2(1140f, 44f), 30, TextAnchor.MiddleCenter, UiKit.Accent);
            UiKit.TextBox(panel, "sub", "Pick one. It lasts the whole run unless it says “this date”.",
                new Vector2(0f, 262f), new Vector2(1140f, 30f), 17, TextAnchor.MiddleCenter, UiKit.TextLight);

            var cards = PowerUpCatalog.DrawThree();
            for (int i = 0; i < cards.Count; i++)
                BuildCard(panel, cards[i], (i - (cards.Count - 1) / 2f) * 380f);

            if (cards.Count == 0)
                UiKit.MakeButton(panel, "CONTINUE", new Vector2(240f, 60f), new Vector2(0f, -260f), () => Resolve(null),
                    UiKit.Accent, 24);
        }

        // Card is 340 x 520, centered at (x, -30) inside the panel.
        private static void BuildCard(Transform panel, PowerUp card, float x)
        {
            var col = ToneColor(card.Tone);
            const float W = 340f, H = 520f;
            var cardRt = UiKit.Box(panel, "card_" + card.Id, new Color(0.13f, 0.10f, 0.17f, 1f),
                new Vector2(x, -30f), new Vector2(W, H));

            // Tone strip pinned to the top of the card.
            UiKit.Box(cardRt, "strip", col, new Vector2(0f, H / 2f - 22f), new Vector2(W, 44f));
            UiKit.TextBox(cardRt, "tone", card.Tone.ToString().ToUpper(),
                new Vector2(0f, H / 2f - 22f), new Vector2(W, 44f), 16, TextAnchor.MiddleCenter, UiKit.Ink);

            // Title: fixed 3-line box just under the strip.
            UiKit.TextBox(cardRt, "title", card.Title,
                new Vector2(0f, H / 2f - 96f), new Vector2(W - 32f, 96f), 24, TextAnchor.MiddleCenter, col);

            // Description: fills the middle, top-aligned.
            UiKit.TextBox(cardRt, "desc", card.Desc,
                new Vector2(0f, -20f), new Vector2(W - 40f, 230f), 18, TextAnchor.UpperCenter, UiKit.TextLight);

            UiKit.MakeButton(cardRt, "TAKE", new Vector2(W - 48f, 54f), new Vector2(0f, -H / 2f + 42f), () =>
            {
                if (_resolving) return;
                ConfirmDialog.Show(_root.transform, "Take “" + card.Title + "”?", "TAKE IT", () => Resolve(card));
            }, col, 24);
        }

        private static void Resolve(PowerUp card)
        {
            if (_resolving) return;
            _resolving = true;
            EndlessPlugin.Log.LogInfo("Draft pick: " + (card?.Id ?? "<none>"));

            if (_root != null) Object.Destroy(_root);
            _root = null;

            var status = Hp2.Status;
            System.Action finish = () =>
            {
                try { DateFlowPatch.OnDraftResolved(); }
                catch (System.Exception e) { EndlessPlugin.Log.LogError("OnDraftResolved: " + e); }
                EndlessRun.ModalOpen = false;
                EndlessRun.PendingDraft = false;
                _resolving = false;
                TimeFreeze(false);
                // Time is running again — now the on-doll notifications can animate.
                try { Hp2.FlushNotes(); } catch (System.Exception e) { EndlessPlugin.Log.LogError("FlushNotes: " + e); }
            };

            if (card == null) { finish(); return; }

            try { card.Apply(status, finish); }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogError("PowerUp '" + card.Id + "' apply: " + e);
                finish();
            }
        }

        private static Color ToneColor(PowerTone t)
        {
            switch (t)
            {
                case PowerTone.Boon: return new Color(0.42f, 0.85f, 0.55f, 1f);
                case PowerTone.Heavy: return new Color(0.95f, 0.42f, 0.42f, 1f);
                default: return UiKit.Warn;
            }
        }

        private static void TimeFreeze(bool on)
        {
            try
            {
                var pd = Game.Session?.Puzzle?.puzzleGrid?.pauseDefinition;
                if (pd == null) return;
                if (on) Game.Manager.Time.Pause(pd);
                else Game.Manager.Time.Unpause(pd);
            }
            catch { }
        }
    }
}

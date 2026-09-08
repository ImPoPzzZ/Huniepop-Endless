using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HuniepopEndless
{
    /// <summary>
    /// After a gift power-up resolves: show each gift — name, type, what it does, its
    /// sentiment cost — and let the player choose which girl it goes to. The gift then
    /// sits in that girl's tray to be dragged in-game whenever they want.
    /// </summary>
    internal static class GiftPicker
    {
        internal static void Run(int count, PuzzleAffectionType? affection, Action done)
        {
            if (!Hp2.AnyGiftSlotFree()) { done?.Invoke(); return; }

            var items = new List<ItemDefinition>();
            for (int i = 0; i < count; i++)
            {
                var it = Hp2.RandomDateGift(affection);
                if (it != null) items.Add(it);
            }
            if (items.Count == 0) { done?.Invoke(); return; }

            var canvas = UiKit.MakeOverlayCanvas("HE_GiftPicker", 5800);
            Step(canvas, items, 0, done);
        }

        private static void Step(Canvas canvas, List<ItemDefinition> items, int index, Action done)
        {
            if (index >= items.Count)
            {
                UnityEngine.Object.Destroy(canvas.gameObject);
                done?.Invoke();
                return;
            }

            var item = items[index];
            var root = UiKit.MakeBackdrop(canvas.transform, 0.55f);
            var panel = UiKit.Box(root.transform, "panel", UiKit.Panel, Vector2.zero, new Vector2(820f, 560f));

            UiKit.TextBox(panel, "step", "GIFT " + (index + 1) + " OF " + items.Count,
                new Vector2(0f, 245f), new Vector2(760f, 30f), 16, TextAnchor.MiddleCenter, UiKit.Warn);
            UiKit.TextBox(panel, "name", item.itemName,
                new Vector2(0f, 205f), new Vector2(760f, 40f), 28, TextAnchor.MiddleCenter, UiKit.Accent);
            UiKit.TextBox(panel, "type",
                Hp2.Titleize(item.affectionType) + " · " + Nice(item.dateGiftType.ToString())
                + "    ·    Sentiment cost: " + item.GetUseCost(),
                new Vector2(0f, 170f), new Vector2(760f, 28f), 16, TextAnchor.MiddleCenter, UiKit.TextLight);

            string desc = !string.IsNullOrEmpty(item.itemDescription) ? item.itemDescription
                        : !string.IsNullOrEmpty(item.categoryDescription) ? item.categoryDescription
                        : "A " + Hp2.Titleize(item.affectionType) + " date gift. Drag it onto her during the date "
                          + "to spend sentiment for a burst of " + Hp2.Titleize(item.affectionType) + " affection.";
            UiKit.TextBox(panel, "desc", desc,
                new Vector2(0f, 90f), new Vector2(720f, 110f), 18, TextAnchor.UpperCenter, UiKit.TextLight);

            var status = Hp2.Status;
            var girls = new List<PuzzleStatusGirl>(Hp2.ActiveGirls(status));
            for (int i = 0; i < girls.Count; i++)
            {
                var g = girls[i];
                float x = (girls.Count == 1) ? 0f : (i == 0 ? -190f : 190f);
                bool full = g.dateGifts.Count >= 4;
                string label = "GIVE TO\n" + g.girlDefinition.GetNickName() + (full ? "\n(tray full)" : "");
                UiKit.MakeButton(panel, label, new Vector2(300f, 96f), new Vector2(x, -70f), () =>
                {
                    if (!full) Hp2.GrantDateGift(g, item);
                    UnityEngine.Object.Destroy(root);
                    Step(canvas, items, index + 1, done);
                }, full ? UiKit.AccentDim : UiKit.Accent, 22);
            }

            UiKit.MakeButton(panel, "SKIP THIS GIFT", new Vector2(240f, 52f), new Vector2(0f, -190f), () =>
            {
                UnityEngine.Object.Destroy(root);
                Step(canvas, items, index + 1, done);
            }, UiKit.Ink, 18);
        }

        private static string Nice(string enumName)
        {
            var s = enumName.Replace('_', ' ').ToLower();
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}

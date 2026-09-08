using System;
using UnityEngine;
using UnityEngine.UI;

namespace HuniepopEndless
{
    /// <summary>
    /// A blocking "are you sure?" confirmation. Every irreversible pick in this mod
    /// (start a run, take a power-up, assign a gift) routes through here so nothing
    /// happens on a single stray click.
    /// </summary>
    internal static class ConfirmDialog
    {
        internal static void Show(Transform parent, string question, string confirmLabel, Action onConfirm)
        {
            var root = UiKit.MakeImage(parent, "confirm", new Color(0f, 0f, 0f, 0.55f));
            UiKit.Stretch(root.gameObject);
            root.raycastTarget = true;
            root.transform.SetAsLastSibling();

            var panel = UiKit.Box(root.transform, "panel", UiKit.Panel,
                Vector2.zero, new Vector2(560f, 240f));

            UiKit.MakeText(panel, "q", question, 22, TextAnchor.MiddleCenter, UiKit.TextLight)
                .rectTransform.anchoredPosition = new Vector2(0f, 34f);

            UiKit.MakeButton(panel, confirmLabel, new Vector2(230f, 60f), new Vector2(-130f, -60f),
                () => { UnityEngine.Object.Destroy(root.gameObject); SafeInvoke(onConfirm); },
                UiKit.Accent, 24);
            UiKit.MakeButton(panel, "CANCEL", new Vector2(200f, 60f), new Vector2(130f, -60f),
                () => UnityEngine.Object.Destroy(root.gameObject), UiKit.AccentDim, 24);
        }

        private static void SafeInvoke(Action a)
        {
            try { a?.Invoke(); }
            catch (Exception e) { EndlessPlugin.Log.LogError("ConfirmDialog action: " + e); }
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HuniepopEndless
{
    /// <summary>Shown when a run ends. One button back to the title.</summary>
    internal static class SummaryPanel
    {
        internal static void ShowThenTitle(int datesCleared, int best)
        {
            var canvas = UiKit.MakeOverlayCanvas("HE_Summary", 6000);
            var root = UiKit.MakeBackdrop(canvas.transform, 0.7f);

            var panel = UiKit.Box(root.transform, "panel", UiKit.Panel, Vector2.zero, new Vector2(640f, 380f));

            UiKit.MakeText(panel, "head", "RUN OVER", 46, TextAnchor.UpperCenter, UiKit.Accent)
                .rectTransform.anchoredPosition = new Vector2(0f, -40f);
            UiKit.MakeText(panel, "score",
                "Dates cleared: " + datesCleared + "\nBest this session: " + Mathf.Max(best, datesCleared),
                26, TextAnchor.MiddleCenter, UiKit.TextLight)
                .rectTransform.anchoredPosition = new Vector2(0f, 20f);

            UiKit.MakeButton(panel, "BACK TO TITLE", new Vector2(300f, 66f), new Vector2(0f, -130f), () =>
            {
                Object.Destroy(canvas.gameObject);
                EndlessRun.Reset();
                SceneManager.LoadScene("TitleScene", LoadSceneMode.Single);
            }, UiKit.Accent, 26);
        }
    }
}

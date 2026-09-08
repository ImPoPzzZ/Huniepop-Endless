using UnityEngine;
using UnityEngine.SceneManagement;

namespace HuniepopEndless
{
    /// <summary>
    /// F8 keyboard shortcut for the launcher. The visible title button is drawn by
    /// <see cref="EndlessPlugin.OnGUI"/> (IMGUI) for now — HP2's phone blocks child
    /// raycasts while closed, and the uGUI overlay path proved unreliable here, so
    /// we're using the one input path that always works while we sort the UI out.
    /// </summary>
    internal static class TitleMenuPatch
    {
        internal static void Tick()
        {
            if (SceneManager.GetActiveScene().name != "TitleScene" || EndlessRun.Active) return;
            if (Input.GetKeyDown(KeyCode.F8)) EndlessLauncher.Open();
        }
    }
}

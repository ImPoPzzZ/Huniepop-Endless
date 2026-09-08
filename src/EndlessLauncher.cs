using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HuniepopEndless
{
    /// <summary>
    /// The launch screen: read the blurb, confirm, drop into a run. IMGUI (see
    /// <see cref="TitleMenuPatch"/> for why). Builds a throwaway <see cref="PlayerFile"/>
    /// in memory slot 3 and loads MainScene; <see cref="SaveGuard"/> keeps it off disk.
    /// </summary>
    internal static class EndlessLauncher
    {
        internal static bool IsOpen { get; private set; }
        private static bool _confirming;

        internal static void Open()
        {
            if (IsOpen || EndlessRun.Active) return;
            IsOpen = true;
            _confirming = false;
            EndlessPlugin.Instance.RegisterLauncherGui(DrawGui);
        }

        internal static void Close()
        {
            IsOpen = false;
            _confirming = false;
            EndlessPlugin.Instance.RegisterLauncherGui(null);
        }

        private static void DrawGui()
        {
            float w = 560f, h = 280f;
            var box = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.color = Color.white;
            GUI.Box(box, "");
            GUILayout.BeginArea(new Rect(box.x + 24, box.y + 20, w - 48, h - 40));

            GUILayout.Label("<b><size=24>ENDLESS MODE</size></b>", Rich());
            GUILayout.Space(6);
            GUILayout.Label("An unbroken chain of dates. The goal climbs each date; you keep your\n" +
                            "moves. Draft one of three power-ups before every date. Fail once and\n" +
                            "the run is over. Nothing is saved.", Rich());
            GUILayout.Space(18);

            if (!_confirming)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("START  ▶", GUILayout.Height(44))) _confirming = true;
                if (GUILayout.Button("Cancel", GUILayout.Height(44))) Close();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("<b>Start an Endless run?</b>", Rich());
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.96f, 0.36f, 0.62f);
                if (GUILayout.Button("YES, START THE RUN", GUILayout.Height(44))) Launch();
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Back", GUILayout.Height(44))) _confirming = false;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private static GUIStyle _rich;
        private static GUIStyle Rich()
        {
            if (_rich == null) _rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            return _rich;
        }

        private static void Launch()
        {
            try
            {
                EndlessRun.Reset();
                BuildSyntheticFile();
                EndlessRun.Active = true;
                EndlessRun.PendingLaunch = true;
                Game.Persistence.loadedFileIndex = EndlessRun.ScratchSlot;
                try { Game.Manager.Audio.FadeOutCategory(AudioCategory.MUSIC, 0.3f); } catch { }
                Close();
                EndlessPlugin.Log.LogInfo("Launching Endless run");
                SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                EndlessPlugin.Log.LogError("Launch failed: " + e);
                EndlessRun.Reset();
                Close();
            }
        }

        private static void BuildSyntheticFile()
        {
            var files = Game.Persistence.playerData.files;
            while (files.Count <= EndlessRun.ScratchSlot) files.Add(new PlayerFile(new SaveFile()));

            var pf = new PlayerFile(new SaveFile());
            pf.started = true;
            pf.settingGender = SettingGender.MALE;
            pf.settingDifficulty = SettingDifficulty.NORMAL;
            pf.storyProgress = 13;
            pf.daytimeElapsed = 0;
            pf.staminaFoodLimit = 4;

            foreach (PuzzleAffectionType t in Enum.GetValues(typeof(PuzzleAffectionType)))
                pf.AddAffectionLevelExp(t, 12);

            var roster = Game.Data.Girls.GetAllBySpecial(false);
            if (roster.Count > 0) pf.fileIconGirlDefinition = roster[0];

            var dates = Game.Data.Locations.GetAllByLocationType(LocationType.DATE);
            if (dates.Count > 0)
                pf.locationDefinition = dates[UnityEngine.Random.Range(0, dates.Count)];

            files[EndlessRun.ScratchSlot] = pf;
        }
    }
}

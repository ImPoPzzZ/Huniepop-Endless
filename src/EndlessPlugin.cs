using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HuniepopEndless
{
    [BepInPlugin(Guid, "Huniepop Endless", "0.1.0")]
    public class EndlessPlugin : BaseUnityPlugin
    {
        internal const string Guid = "com.manostroll.huniepopendless";

        internal static EndlessPlugin Instance;
        internal static ManualLogSource Log;
        internal static ConfigEntry<int> BestDatesEver;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            BestDatesEver = Config.Bind("stats", "best_dates_ever", 0,
                "Most dates cleared in a single Endless run (session-persistent brag counter).");

            // Surface loader exceptions early — a type that fails to load silently
            // disables every patch in this assembly otherwise.
            try { Assembly.GetExecutingAssembly().GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                    if (le != null) Log.LogError("LoaderException: " + le);
            }

            try
            {
                var harmony = new Harmony(Guid);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                int n = 0;
                foreach (var mth in harmony.GetPatchedMethods()) { Log.LogInfo("  patched: " + mth.DeclaringType.Name + "." + mth.Name); n++; }
                Log.LogInfo("Harmony patches applied (" + n + " methods).");
            }
            catch (Exception e)
            {
                Log.LogError("harmony.PatchAll() failed: " + e);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            Log.LogInfo("Huniepop Endless loaded.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (scene.name == "MainScene" && EndlessRun.PendingLaunch)
                    RunBootstrap.OnMainSceneLoaded();
                else if (scene.name == "TitleScene")
                {
                    if (EndlessRun.ReturningFromRun)
                    {
                        EndlessRun.ReturningFromRun = false;
                        RunBootstrap.KillMenuMusic();   // silence the date music carried over
                    }
                    EndlessRun.Reset();
                }
            }
            catch (Exception e)
            {
                Log.LogError("OnSceneLoaded(" + scene.name + "): " + e);
            }
        }

        private int _updateErrors;
        private bool _loggedTick;

        private void Update()
        {
            try
            {
                if (!_loggedTick) { _loggedTick = true; Log.LogInfo("Update loop running."); }
                TitleMenuPatch.Tick();
                if (EndlessRun.Active)
                {
                    RunBootstrap.Tick();

                    if (EndlessRun.PendingDraft && !EndlessRun.ModalOpen && DraftWindowOpen())
                        PowerUpDraft.Open();
                }
            }
            catch (Exception e)
            {
                if (_updateErrors++ < 10)
                    Log.LogError("Update: " + e);
            }
        }

        private Action _launcherGui;
        internal void RegisterLauncherGui(Action gui) => _launcherGui = gui;

        // Bulletproof IMGUI — no Canvas, no EventSystem, no GraphicRaycaster.
        private void OnGUI()
        {
            try
            {
                if (_launcherGui != null) { _launcherGui(); return; }
                if (EndlessRun.Active)
                {
                    if (!EndlessRun.ModalOpen) EndlessHud.Draw();
                    return;
                }
                if (SceneManager.GetActiveScene().name != "TitleScene") return;

                var w = 340f; var h = 60f;
                var r = new Rect((Screen.width - w) / 2f, Screen.height - h - 24f, w, h);
                var style = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
                GUI.backgroundColor = new Color(0.96f, 0.36f, 0.62f, 1f);
                if (GUI.Button(r, "▶  ENDLESS  MODE", style))
                {
                    Log.LogInfo("IMGUI ENDLESS button pressed.");
                    EndlessLauncher.Open();
                }
                GUI.backgroundColor = Color.white;
            }
            catch (Exception e)
            {
                if (_updateErrors++ < 10) Log.LogError("OnGUI: " + e);
            }
        }

        /// <summary>Only draft when the grid is actually waiting for the player's input
        /// and no cutscene is running — otherwise we'd freeze time mid-banter.</summary>
        private static bool DraftWindowOpen()
        {
            var session = Game.Session;
            if (session?.Puzzle == null || !session.Puzzle.isPuzzleActive) return false;
            if (session.Cutscenes != null && session.Cutscenes.isCutscenePlaying) return false;
            var grid = session.Puzzle.puzzleGrid;
            return grid != null && grid.state == PuzzleGameState.WAITING && !grid.roundOver;
        }

        /// <summary>Record a finished run's date count into the session brag counter.</summary>
        internal static void RecordRun(int datesCompleted)
        {
            if (datesCompleted > BestDatesEver.Value)
            {
                BestDatesEver.Value = datesCompleted;
                Instance.Config.Save();
            }
        }
    }
}

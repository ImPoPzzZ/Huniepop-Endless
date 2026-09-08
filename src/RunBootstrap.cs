using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HuniepopEndless
{
    /// <summary>
    /// Gets an Endless run off the ground once MainScene is live: waits for the game
    /// session to settle, then departs to the first date location. HuniePop 2 does the
    /// rest itself — arriving at a <see cref="LocationType.DATE"/> location with a null
    /// girl pair triggers the whole Non-Stop setup (shuffle roster, reset puzzle,
    /// intro cutscene, StartPuzzle).
    /// </summary>
    internal static class RunBootstrap
    {
        private static float _loadedAt;
        private static bool _watchdogDone;

        internal static void OnMainSceneLoaded()
        {
            EndlessRun.PendingLaunch = false;
            _loadedAt = Time.realtimeSinceStartup;
            _watchdogDone = false;
            KillMenuMusic();
            EndlessPlugin.Log.LogInfo("RunBootstrap: MainScene loaded — expecting straight-to-date arrival.");
        }

        /// <summary>The title theme plays on an AudioSource attached to the persistent
        /// game camera, so it survives the scene load. Fade isn't enough — destroy every
        /// MUSIC link outright.</summary>
        internal static void KillMenuMusic()
        {
            try
            {
                var audio = Game.Manager.Audio;
                var list = Reflect.Get<System.Collections.IList>(audio, "_playingAudio");
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var link = list[i];
                    var cat = (AudioCategory)Reflect.Get<object>(link, "audioCategory");
                    if (cat == AudioCategory.MUSIC)
                    {
                        Reflect.Call(link, "Destructor");
                        list.RemoveAt(i);
                    }
                }
                EndlessPlugin.Log.LogInfo("Menu music killed.");
            }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogWarning("KillMenuMusic: " + e.Message);
                try { Game.Manager.Audio.FadeOutCategory(AudioCategory.MUSIC, 0.1f); } catch { }
            }
        }

        internal static void Tick()
        {
            // The synthetic file's locationDefinition is already a DATE location, so
            // LocationManager's initial Arrive starts the Non-Stop date on its own.
            // This is just a safety net if that somehow didn't happen.
            if (_watchdogDone) return;
            var session = Game.Session;
            if (session?.Puzzle != null && session.Puzzle.isPuzzleActive) { _watchdogDone = true; return; }
            if (session?.Location == null) return;
            if (Time.realtimeSinceStartup - _loadedAt < 5f) return;

            _watchdogDone = true;
            var cur = session.Location.currentLocation;
            bool bad = cur == null
                       || cur.locationType != LocationType.DATE
                       || cur == session.Puzzle.tutorialLocationDefinition;
            if (bad)
            {
                var loc = CyclableDateLocation(session.Location, 0);
                if (loc != null)
                {
                    EndlessPlugin.Log.LogWarning("RunBootstrap watchdog: forcing depart to " + loc.name);
                    Game.Persistence.playerFile.daytimeElapsed = 0;
                    session.Location.Depart(loc, null);
                }
            }
        }

        /// <summary>
        /// Update the visible location + time of day for a new Non-Stop round. We must
        /// NOT call LocationManager.Arrive here (it re-runs StartPuzzle and resets the
        /// dolls) — just move the clock and re-skin the background.
        /// </summary>
        internal static void AdvanceLocationForRound(int dateIndex)
        {
            try
            {
                var loc = PickDateLocation(dateIndex);
                var pf = Game.Persistence.playerFile;
                pf.daytimeElapsed = dateIndex;
                if (loc != null)
                {
                    pf.locationDefinition = loc;
                    // LocationManager caches the location in a private field that all the
                    // location-aware dialogue reads — keep it in sync or the girls talk
                    // about the previous date's venue.
                    try { Reflect.Set(Game.Session.Location, "_currentLocation", loc); } catch { }
                }

                var bg = Game.Session.gameCanvas.bgLocations;
                bg.currentBg.art.Refresh(pf.locationDefinition, pf.daytimeElapsed);
                bg.currentBg.bar.Refresh(pf.locationDefinition, pf.daytimeElapsed, pf.girlPairDefinition, pf.sidesFlipped);

                // Swap the location music (we skip HP2's depart/arrive so it won't do it).
                var lm = Game.Session.Location;
                var newKlip = pf.locationDefinition != null ? pf.locationDefinition.bgMusic : null;
                if (newKlip != null && (lm.bgMusicLink == null || lm.bgMusicLink.audioClip != newKlip.clip))
                {
                    if (lm.bgMusicLink != null) lm.bgMusicLink.FadeOut(1.5f);
                    lm.bgMusicLink = Game.Manager.Audio.Play(AudioCategory.MUSIC, newKlip);
                    EndlessPlugin.Log.LogInfo("[loc] music -> " + newKlip.clip?.name);
                }
                EndlessPlugin.Log.LogInfo($"[loc] date {dateIndex}: location={loc?.locationName ?? "?"} slot={dateIndex % 4}");
            }
            catch (System.Exception e)
            {
                EndlessPlugin.Log.LogWarning("AdvanceLocationForRound: " + e.Message);
            }
        }

        private static LocationDefinition PickDateLocation(int dateIndex)
            => CyclableDateLocation(Game.Session?.Location, dateIndex);

        private static readonly string[] BlockedLocationWords =
            { "space", "volcano", "airplane", "bathroom", "tutorial", "nymphojinn", "poolside", "boss" };

        private static bool Blocked(LocationDefinition d)
        {
            if (d == null) return true;
            var p = Game.Session?.Puzzle;
            if (p != null && (d == p.tutorialLocationDefinition || d == p.bossLocationDefinition
                              || d == p.postTutorialLocationDefinition || d == p.postBossLocationDefinition))
                return true;
            string s = ((d.locationName ?? "") + " " + d.name).ToLowerInvariant();
            foreach (var w in BlockedLocationWords) if (s.Contains(w)) return true;
            return false;
        }

        /// <summary>A real, cyclable date location for the given date's time of day —
        /// never the tutorial or a boss/story date.</summary>
        internal static LocationDefinition CyclableDateLocation(LocationManager loc, int dateIndex)
        {
            if (loc == null) return null;
            int slot = ((dateIndex % 4) + 4) % 4;
            var pool = new System.Collections.Generic.List<LocationDefinition>();

            try
            {
                var infos = loc.dateLocationsInfos;
                if (infos != null)
                    foreach (var info in infos)
                        if ((int)info.daytimeType == slot && info.locationDefinitions != null)
                            foreach (var d in info.locationDefinitions)
                                if (!Blocked(d)) pool.Add(d);
            }
            catch { }

            if (pool.Count == 0)
            {
                try
                {
                    var defs = loc.dateLocationDefs;
                    if (defs != null)
                        foreach (var d in defs) if (!Blocked(d)) pool.Add(d);
                }
                catch { }
            }

            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
        }

        /// <summary>End the run: record the score and go back to the title screen.</summary>
        internal static void EndRun()
        {
            EndlessPlugin.RecordRun(EndlessRun.DatesCompleted);
            int cleared = EndlessRun.DatesCompleted;
            _watchdogDone = true;
            EndlessRun.ReturningFromRun = true;
            SummaryPanel.ShowThenTitle(cleared, EndlessPlugin.BestDatesEver.Value);
        }
    }
}

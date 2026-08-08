using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Determinism
{
    /// <summary>
    /// The mode + level for the current run, and the one place that decides
    /// them (Phase 3).
    ///
    /// NOT a mirror file -- Unity-side plumbing like MsRunSeed. The one piece
    /// of mirrored logic is <see cref="DailyRotationLevel"/>, which must agree
    /// with ms_run_guard.daily_level (1 + days-since-epoch % 3) -- change both
    /// or neither. Deliberately date-only, no secret: the client picks its
    /// profile BEFORE the anchor response arrives, so it must be computable
    /// offline. Only the daily seed is unpredictable, and that stays
    /// server-side.
    ///
    /// PREF CONTRACT:
    ///   ms_selected_level -- STICKY (1..3, never cleared). Survives retry and
    ///     app restarts, which fixes the old IsLevel2 landmine where Retry
    ///     silently dropped the player back onto Level 1.
    ///   ms_daily_mode -- ONE-SHOT (cleared when the gameplay scene resolves
    ///     it). Retry after a daily run therefore falls back to a normal run
    ///     of the sticky level: the daily attempt is burned at /run/start, so
    ///     replaying it as "daily" would only mint a 409.
    ///
    /// WHY A SINGLE RESOLVER: EnemySpawner.Start and BackgroundResolver.Start
    /// both need the answer and their relative order is undefined. If each
    /// read the prefs itself, whichever ran second would see the daily flag
    /// already cleared and pick a different level than the spawner. One
    /// resolve per scene load (invalidated on sceneLoaded, which fires before
    /// any Start) gives every consumer the same view, including JsonBuilder
    /// at game over.
    /// </summary>
    public static class MsLevelSelect
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 3;

        private const string LevelPref = "ms_selected_level";
        private const string DailyPref = "ms_daily_mode";

        private static bool resolved;
        private static bool resolvedDaily;
        private static int resolvedLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded += (_, __) => resolved = false;
        }

        /// <summary>The sticky level selection (1..3). Side-effect free --
        /// safe to read from menu/inventory UI without consuming anything.</summary>
        public static int SelectedLevel
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(LevelPref, MinLevel), MinLevel, MaxLevel); }
        }

        public static void SetSelectedLevel(int level)
        {
            PlayerPrefs.SetInt(LevelPref, Mathf.Clamp(level, MinLevel, MaxLevel));
            PlayerPrefs.Save();
        }

        /// <summary>Arms the one-shot daily flag. Call from the DAILY button,
        /// immediately before loading the gameplay scene.</summary>
        public static void ArmDaily()
        {
            PlayerPrefs.SetInt(DailyPref, 1);
            PlayerPrefs.Save();
        }

        /// <summary>True when the CURRENT run is a daily-challenge run.
        /// First access this scene load consumes the one-shot flag.</summary>
        public static bool IsDaily
        {
            get { Resolve(); return resolvedDaily; }
        }

        /// <summary>The campaign level the current run actually plays:
        /// the date rotation for daily runs, the sticky selection otherwise.</summary>
        public static int EffectiveLevel
        {
            get { Resolve(); return resolvedLevel; }
        }

        /// <summary>"daily" or "normal" -- the string /run/start and the
        /// submission JSON both carry.</summary>
        public static string Mode
        {
            get { return IsDaily ? "daily" : "normal"; }
        }

        /// <summary>Which level today's daily challenge plays, from the UTC
        /// date alone. MIRROR of ms_run_guard.daily_level.</summary>
        public static int DailyRotationLevel(DateTime utcNow)
        {
            var days = (utcNow.Date - new DateTime(1970, 1, 1)).Days;
            return MinLevel + (days % (MaxLevel - MinLevel + 1));
        }

        private static void Resolve()
        {
            if (resolved)
                return;

            resolved = true;
            resolvedDaily = PlayerPrefs.GetInt(DailyPref, 0) == 1;

            if (resolvedDaily)
            {
                PlayerPrefs.DeleteKey(DailyPref);
                PlayerPrefs.Save();
                resolvedLevel = DailyRotationLevel(DateTime.UtcNow);
            }
            else
            {
                resolvedLevel = SelectedLevel;
            }
        }
    }
}

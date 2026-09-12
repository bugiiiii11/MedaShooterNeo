using System;
using System.Globalization;
using Cryptomeda.Minigames.BackendComs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Determinism
{
    /// <summary>
    /// Requests a server-issued run token + seed at run start and holds them
    /// for the score submission. The server half of MsRunSeed.
    ///
    /// NOT a mirror file -- Unity-side plumbing, like MsRunSeed.
    ///
    /// FAIL-OPEN BY DESIGN FOR NORMAL RUNS: any failure here (endpoint down,
    /// rate limit, slow connection, stale response, version mismatch) leaves
    /// the run unanchored on its local seed. Unanchored runs play and submit
    /// exactly like before; the backend just records them as "unanchored"
    /// instead of "ok". A determinism feature must never be able to stop
    /// someone playing.
    ///
    /// DAILY RUNS ARE THE EXCEPTION (F5): the daily challenge is everyone
    /// playing the SAME server-issued seed, so an unanchored daily run is a
    /// private run wearing the daily's name -- it cannot reach the daily board
    /// (that write is keyed on run_id) and its schedule is one nobody else
    /// played. A daily run is therefore anchored or not played at all: the
    /// player is returned to inventory with the reason, having burned nothing.
    /// The attempt is reserved server-side only on a 200
    /// (`api_routes.py` /run/start, reservation and run row in one
    /// transaction), so an aborted daily is still playable afterwards.
    ///
    /// Every asynchronous result is generation-stamped (see the MsRunSeed
    /// docstring for why): a response from run N arriving during run N+1 is
    /// discarded, never applied.
    /// </summary>
    public static class MsRunAnchor
    {
        [Serializable]
        private class RunStartResponse
        {
            // field names match the backend JSON exactly -- JsonUtility does
            // no case mapping
            public string run_id;
            public string seed;
            public string token;
            public int schedule_version;
        }

        private static string runId;
        private static string runToken;
        private static uint anchorGeneration;

        // The inventory scene, where the DAILY button lives -- build order is
        // loading(0), menu(1), inventory(2), gameplay(3). UIGameOverScreen's
        // Exit goes to 1 (menu) on purpose; an aborted daily goes to 2 so the
        // player is one click from trying again.
        private const int InventorySceneIndex = 2;

        // Every malformed-response path says the same thing: the player does
        // not care which field was missing, only that nothing was spent.
        private const string BadResponseNotice =
            "Daily Challenge could not start -- the server sent something we could not read. Your attempt was not used.";

        // Set when a daily run is abandoned, read once by the inventory UI.
        private static string pendingNotice;

        /// <summary>
        /// Abandons a daily run that failed to anchor and returns the player to
        /// inventory with the reason. Returns whether it acted, so a normal run
        /// can log its own fail-open line instead.
        /// </summary>
        private static bool AbortIfDaily(uint generation, bool isDaily, string notice)
        {
            if (!isDaily)
                return false;

            // Generation guard, same contract as TryApplyServerSeed: a late
            // response from an ABANDONED daily must never yank the run the
            // player has since started out from under them.
            if (generation != MsRunSeed.Generation)
                return false;

            pendingNotice = notice;
            Debug.Log($"[MsRunAnchor] daily run abandoned: {notice}");
            SceneManager.LoadScene(InventorySceneIndex);
            return true;
        }

        /// <summary>
        /// The reason the last daily run was abandoned, or null. Reading it
        /// clears it -- the notice belongs to one return trip, not to every
        /// later visit to inventory.
        /// </summary>
        public static string ConsumeAbortNotice()
        {
            var notice = pendingNotice;
            pendingNotice = null;
            return notice;
        }

        /// <summary>
        /// Fires the /run/start request for the run that just began. Call once
        /// per run, right after MsRunSeed.BeginRun, with the generation it
        /// returned. Phase 3: mode ("normal"|"daily") and level (1..3) ride
        /// along; the server records them on the run row, and for daily runs
        /// forces its own date-derived level + fixed seed.
        /// </summary>
        public static void RequestAnchor(uint generation, string mode, int level)
        {
            // stale anchor data must not outlive its run
            if (anchorGeneration != generation)
            {
                runId = null;
                runToken = null;
            }

            var wallet = PlayerProfileInfo.instance ? PlayerProfileInfo.instance.WalletAddress : null;
            if (string.IsNullOrEmpty(wallet))
                return; // practice/editor run -- nothing to anchor to

            // F5: daily runs are anchored or not played. Resolved here rather
            // than inside the callback so a mode string that arrives null is
            // treated as a normal run, never as a daily one.
            var isDaily = string.Equals(mode, "daily", StringComparison.Ordinal);

            var json = "{\"address\":\"" + wallet + "\",\"mode\":\"" + (mode ?? "normal")
                + "\",\"level\":" + level.ToString(CultureInfo.InvariantCulture) + "}";

            RestfulManager.Post(RestfulEndpoint.RunStart, json, response =>
            {
                if (response.Code != 200)
                {
                    // 409 = the daily attempt was already burned for this UTC
                    // day; every other code is the endpoint being unreachable.
                    // A NORMAL run shrugs and continues unanchored on its local
                    // seed. A DAILY run cannot: it would be a private schedule
                    // submitted as the day's shared one, so it goes back to
                    // inventory instead (F5). Nothing was reserved -- the
                    // server writes the attempt row only on a 200.
                    Debug.Log($"[MsRunAnchor] run/start returned {response.Code}");
                    if (!AbortIfDaily(generation, isDaily, response.Code == 409
                            ? "Today's Daily Challenge has already been played. It resets at 00:00 UTC."
                            : "Daily Challenge could not start -- the server did not answer. Your attempt was not used."))
                        Debug.Log("[MsRunAnchor] run stays unanchored");
                    return;
                }

                // PostCo prefixes the body with "Code: NNN:\n"
                var text = response.Text;
                var brace = string.IsNullOrEmpty(text) ? -1 : text.IndexOf('{');
                if (brace < 0)
                {
                    AbortIfDaily(generation, isDaily, BadResponseNotice);
                    return;
                }

                RunStartResponse parsed;
                try
                {
                    parsed = JsonUtility.FromJson<RunStartResponse>(text.Substring(brace));
                }
                catch (Exception e)
                {
                    Debug.Log($"[MsRunAnchor] run/start response unparseable: {e.Message}");
                    AbortIfDaily(generation, isDaily, BadResponseNotice);
                    return;
                }

                if (parsed == null || string.IsNullOrEmpty(parsed.run_id) || string.IsNullOrEmpty(parsed.token))
                {
                    AbortIfDaily(generation, isDaily, BadResponseNotice);
                    return;
                }

                if (parsed.schedule_version != MsSchedule.ScheduleVersion)
                {
                    // the server would refuse to compare this run anyway --
                    // playing the server seed would only mint a false divergence
                    Debug.Log($"[MsRunAnchor] schedule version mismatch (server {parsed.schedule_version}, client {MsSchedule.ScheduleVersion}) -- run stays unanchored");
                    AbortIfDaily(generation, isDaily,
                        "Daily Challenge is unavailable for this build -- reload the page to update. Your attempt was not used.");
                    return;
                }

                if (!ulong.TryParse(parsed.seed, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                {
                    AbortIfDaily(generation, isDaily, BadResponseNotice);
                    return;
                }

                // TryApplyServerSeed is the generation gate; only store the
                // token when the seed actually took
                if (MsRunSeed.TryApplyServerSeed(generation, seed))
                {
                    runId = parsed.run_id;
                    runToken = parsed.token;
                    anchorGeneration = generation;
                    Debug.Log($"[MsRunAnchor] run anchored: {runId}");
                }
                else
                {
                    Debug.Log("[MsRunAnchor] stale run/start response discarded");
                }
            });
        }

        /// <summary>
        /// The token pair for the submission, but only if it belongs to the
        /// run being submitted. False leaves the caller submitting unanchored.
        /// </summary>
        public static bool TryGetForSubmission(out string id, out string token)
        {
            if (MsRunSeed.Anchored && anchorGeneration == MsRunSeed.Generation && !string.IsNullOrEmpty(runId))
            {
                id = runId;
                token = runToken;
                return true;
            }

            id = null;
            token = null;
            return false;
        }
    }
}

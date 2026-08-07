using System;
using System.Globalization;
using Cryptomeda.Minigames.BackendComs;
using UnityEngine;

namespace Determinism
{
    /// <summary>
    /// Requests a server-issued run token + seed at run start and holds them
    /// for the score submission. The server half of MsRunSeed.
    ///
    /// NOT a mirror file -- Unity-side plumbing, like MsRunSeed.
    ///
    /// FAIL-OPEN BY DESIGN: any failure here (endpoint down, rate limit, slow
    /// connection, stale response, version mismatch) leaves the run unanchored
    /// on its local seed. Unanchored runs play and submit exactly like before;
    /// the backend just records them as "unanchored" instead of "ok". A
    /// determinism feature must never be able to stop someone playing.
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

        /// <summary>
        /// Fires the /run/start request for the run that just began. Call once
        /// per run, right after MsRunSeed.BeginRun, with the generation it
        /// returned.
        /// </summary>
        public static void RequestAnchor(uint generation)
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

            var json = "{\"address\":\"" + wallet + "\"}";

            RestfulManager.Post(RestfulEndpoint.RunStart, json, response =>
            {
                if (response.Code != 200)
                {
                    Debug.Log($"[MsRunAnchor] run/start returned {response.Code} -- run stays unanchored");
                    return;
                }

                // PostCo prefixes the body with "Code: NNN:\n"
                var text = response.Text;
                var brace = string.IsNullOrEmpty(text) ? -1 : text.IndexOf('{');
                if (brace < 0)
                    return;

                RunStartResponse parsed;
                try
                {
                    parsed = JsonUtility.FromJson<RunStartResponse>(text.Substring(brace));
                }
                catch (Exception e)
                {
                    Debug.Log($"[MsRunAnchor] run/start response unparseable: {e.Message}");
                    return;
                }

                if (parsed == null || string.IsNullOrEmpty(parsed.run_id) || string.IsNullOrEmpty(parsed.token))
                    return;

                if (parsed.schedule_version != MsSchedule.ScheduleVersion)
                {
                    // the server would refuse to compare this run anyway --
                    // playing the server seed would only mint a false divergence
                    Debug.Log($"[MsRunAnchor] schedule version mismatch (server {parsed.schedule_version}, client {MsSchedule.ScheduleVersion}) -- run stays unanchored");
                    return;
                }

                if (!ulong.TryParse(parsed.seed, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                    return;

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

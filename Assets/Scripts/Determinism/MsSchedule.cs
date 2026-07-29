// -----------------------------------------------------------------------------
// MsSchedule -- the deterministic wave schedule for MedaShooter 2.0.
//
// MIRROR FILE. Line-for-line Python twin: backend/app/services/ms_schedule.py.
// Edit both in the same commit. See MsRng.cs for the three mirror rules; the
// same ones apply here (no UnityEngine, integer only, no rejection loops).
//
// WHAT IS AND IS NOT DETERMINISTIC HERE
//
// This file reproduces exactly one thing: WHICH wave asset plays at each wave
// transition. It deliberately does not touch enemy type per spawn, spawn
// position, spawn cooldown, HP rolls, perk offers, drops, powerups or mines.
//
// That scope is not timidity, it is the only scope that survives the server
// side. Anything indexed by the SPAWN ordinal is not reconstructible from a
// score submission, for two independent reasons found while reviewing this:
//
//   a) The campaign boss wave is authored MaxEnemyCount 1 / EnemyQuantity 2 but
//      structurally emits ONE spawn -- the spawn gate never reopens while the
//      boss is alive, and the wave ends via OnEnemyKilled(BasicBoss) instead of
//      the quota check. So spawns-per-wave != EnemyQuantity from the boss
//      onward, permanently.
//   b) The SpawnEnemyForHackers branch adds a second spawn on a probability
//      derived from the LIVE score, which the server never sees. That makes
//      every later wave boundary ambiguous by one spawn, and the ambiguity
//      compounds -- roughly 2^k candidate alignments after k waves.
//
// The wave-TRANSITION ordinal has neither problem. Every call to
// EnemySpawner.GetIndexForNextWave is paired 1:1 with exactly one NextWaveEvent
// dispatch (normal advance, boss kill, and the editor Headstart path all do
// both; the miniboss branch does neither), and PlayerMovement.OnNextWaveSpawned
// increments GameStats.WavesCount BEFORE its IsSilent early-return. So the k-th
// schedule draw is exactly the transition that takes WavesCount to k -- and
// WavesCount ships as parameter3 on every submission.
//
// That invariance is what makes the mirror hold regardless of frame rate, of
// the 15s CheckForMissingEnemies watchdog firing, of boss kills, of silent
// waves, or of how fast the player clears the field.
// -----------------------------------------------------------------------------

namespace Determinism
{
    /// <summary>
    /// Integer-only description of a run's wave data. Built once from the
    /// ScriptableObject assets and then never recomputed -- see MsScheduleData.
    /// </summary>
    public sealed class MsScheduleProfile
    {
        /// <summary>Number of wave entries in the campaign profile.</summary>
        public uint CampaignWaveCount;

        /// <summary>Playable endless entries, as indices into the endless profile.</summary>
        public uint[] EndlessIndices = new uint[0];

        /// <summary>
        /// Selection weight per endless entry, aligned with EndlessIndices.
        /// INTEGER by contract. These are derived from the float32 asset
        /// cooldown ranges exactly once, by MsScheduleData, and then shipped as
        /// integers to both sides. Neither the client nor the server may
        /// recompute them from floats: the shipped WaveDensityPerMinute is
        /// float32 throughout and any double-precision restatement of it
        /// produces different values (verified: endless entry 2 gives
        /// 2.378209352493286 in float32 vs 2.378209412097931 in double). Today
        /// those still floor to the same integer, but only by luck of the
        /// current asset values, and a single balance tweak near a boundary
        /// would split client and server silently.
        /// </summary>
        public uint[] EndlessWeights = new uint[0];

        public uint WeightTotal
        {
            get
            {
                uint total = 0;
                for (var i = 0; i < EndlessWeights.Length; i++)
                {
                    unchecked { total += EndlessWeights[i]; }
                }
                return total;
            }
        }
    }

    /// <summary>
    /// The evolving part of the schedule. Only the previous endless pick, which
    /// the shipped PickEndlessWave uses for its one-shot repeat break.
    /// </summary>
    public struct MsScheduleState
    {
        public int LastEndlessWave;

        public static MsScheduleState New()
        {
            return new MsScheduleState { LastEndlessWave = -1 };
        }
    }

    public static class MsSchedule
    {
        /// <summary>
        /// Bump when the MEANING of a draw changes (a new stream, a different
        /// index allocation, a changed selection rule). The server refuses to
        /// compare runs whose version it does not implement, rather than
        /// reporting a divergence it caused itself.
        /// </summary>
        public const uint ScheduleVersion = 1;

        /// <summary>
        /// Each transition reserves this many consecutive draw indices, so the
        /// index of transition t never depends on whether earlier transitions
        /// took their redraw. PickEndlessWave draws once, or twice when it hits
        /// the same wave twice in a row; a running counter would therefore
        /// advance by a player-visible-but-server-invisible amount. A fixed
        /// stride costs nothing and removes the whole failure mode.
        /// </summary>
        private const uint DrawsPerTransition = 2;

        /// <summary>
        /// The wave index in effect after <paramref name="transition"/> wave
        /// transitions, where transition 1 is the first advance away from the
        /// starting wave. Mirrors EnemySpawner.GetIndexForNextWave exactly.
        ///
        /// Returns an index into the CAMPAIGN profile while
        /// transition &lt; CampaignWaveCount, and into the ENDLESS profile at or
        /// after it -- matching the shipped swap, which happens when the
        /// incremented index would run past the end of the campaign list.
        /// </summary>
        public static int Step(ulong seed, MsScheduleProfile profile, ref MsScheduleState state, uint transition)
        {
            if (transition < profile.CampaignWaveCount)
                return (int)transition;

            return PickEndless(seed, profile, ref state, transition);
        }

        /// <summary>True once the run has left the hand-authored campaign profile.</summary>
        public static bool IsEndless(MsScheduleProfile profile, uint transition)
        {
            return transition >= profile.CampaignWaveCount;
        }

        private static int PickEndless(ulong seed, MsScheduleProfile profile, ref MsScheduleState state, uint transition)
        {
            var count = profile.EndlessIndices.Length;

            // Mirrors the shipped guards. An endless profile with nothing
            // playable falls back to index 0 rather than throwing.
            if (count == 0)
                return 0;

            if (count == 1)
            {
                state.LastEndlessWave = (int)profile.EndlessIndices[0];
                return state.LastEndlessWave;
            }

            var baseIndex = transition * DrawsPerTransition;
            var picked = DrawWeighted(seed, profile, baseIndex);

            // One redraw, never a loop: enough to break a back-to-back repeat
            // without skewing the distribution the way reject-until-different
            // would, and it keeps the draw count bounded at two.
            if (picked == state.LastEndlessWave)
                picked = DrawWeighted(seed, profile, baseIndex + 1);

            state.LastEndlessWave = picked;
            return picked;
        }

        private static int DrawWeighted(ulong seed, MsScheduleProfile profile, uint drawIndex)
        {
            var total = profile.WeightTotal;
            if (total == 0)
                return (int)profile.EndlessIndices[0];

            var roll = MsRng.Below(seed, MsRng.StreamWaveSelect, drawIndex, total);

            // Accumulate rather than subtract. The shipped float version
            // subtracts in iteration order and so accumulates rounding; with
            // integer weights the accumulating form is exactly equivalent, has
            // no rounding at all, and cannot underflow an unsigned type.
            uint acc = 0;
            for (var i = 0; i < profile.EndlessWeights.Length; i++)
            {
                unchecked { acc += profile.EndlessWeights[i]; }

                if (roll < acc)
                    return (int)profile.EndlessIndices[i];
            }

            return (int)profile.EndlessIndices[profile.EndlessIndices.Length - 1];
        }

        /// <summary>
        /// The full realised wave sequence for a run that reached
        /// <paramref name="transitions"/> wave transitions. This is the function
        /// the backend mirror evaluates; the client reaches the same values one
        /// Step at a time.
        /// </summary>
        public static int[] BuildSequence(ulong seed, MsScheduleProfile profile, uint transitions)
        {
            var result = new int[transitions];
            var state = MsScheduleState.New();

            for (uint t = 1; t <= transitions; t++)
            {
                result[t - 1] = Step(seed, profile, ref state, t);
            }

            return result;
        }

        /// <summary>
        /// Order-sensitive digest of a realised sequence. Compared between
        /// client and server as a two-sided drift alarm: it never rejects a
        /// submission, it just turns silent mirror drift into something we can
        /// see in the logs before it matters.
        /// </summary>
        public static ulong SequenceDigest(int[] sequence)
        {
            unchecked
            {
                ulong h = 0xCBF29CE484222325UL;

                for (var i = 0; i < sequence.Length; i++)
                {
                    h ^= (ulong)(uint)sequence[i];
                    h *= 0x100000001B3UL;
                }

                return h;
            }
        }
    }
}

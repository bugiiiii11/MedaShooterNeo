using UnityEngine;

namespace Determinism
{
    /// <summary>
    /// The seed for the current run, and the one place that decides it.
    ///
    /// NOT a mirror file -- it may use UnityEngine. The mirror files (MsRng,
    /// MsSchedule) must stay dependency-free so the parity harness can compile
    /// them outside Unity; this one is the Unity-side plumbing around them.
    ///
    /// WHY THE GENERATION COUNTER EXISTS (it is not defensive boilerplate)
    ///
    /// When the seed later comes from the server, the request is dispatched on
    /// RestfulManager, which is a DontDestroyOnLoad singleton with a 25 second
    /// UnityWebRequest timeout. Retry reloads the gameplay scene directly
    /// (UIGameOverScreen), so an in-flight request from the PREVIOUS run
    /// outlives its own run and its callback lands mid-way through the next one.
    /// Without a generation stamp that callback would overwrite the live seed
    /// part-way through a run, and the run would then match no single seed at
    /// all -- reported as a divergence, on a player whose only mistake was a
    /// slow connection. Wiring the discipline in now costs three fields; adding
    /// it after the fact means re-auditing every call site.
    /// </summary>
    public static class MsRunSeed
    {
        /// <summary>The seed the current run's schedule is drawn from.</summary>
        public static ulong Seed { get; private set; }

        /// <summary>
        /// Monotonic run counter. Every BeginRun bumps it; any asynchronous
        /// result carrying a stale generation must be discarded, not applied.
        /// </summary>
        public static uint Generation { get; private set; }

        /// <summary>
        /// False while the seed is locally generated rather than server-issued.
        /// Unanchored runs are still fully playable and still submit -- a
        /// determinism feature must never be able to stop someone playing.
        /// </summary>
        public static bool Anchored { get; private set; }

        /// <summary>
        /// Starts a new run. Called exactly once per gameplay run, from
        /// EnemySpawner.Start -- deliberately NOT from GameManager.Start, which
        /// also runs in inventory.unity where there is no run to seed.
        /// </summary>
        public static uint BeginRun()
        {
            unchecked { Generation++; }

            Seed = GenerateLocalSeed();
            Anchored = false;

            return Generation;
        }

        /// <summary>
        /// Applies a server-issued seed, but only if it belongs to the run that
        /// is actually being played. Returns false when the response was stale,
        /// so the caller can log it rather than silently mis-seeding a run.
        /// </summary>
        public static bool TryApplyServerSeed(uint generation, ulong seed)
        {
            if (generation != Generation)
                return false;

            Seed = seed;
            Anchored = true;
            return true;
        }

        /// <summary>
        /// A locally generated seed, used until the server issues them.
        ///
        /// System.Random is seeded from the clock and two runs started in the
        /// same millisecond would otherwise share a seed, so the run generation
        /// and a Unity time source are mixed in as well. This does not need to
        /// be cryptographically strong: nothing is protected by the seed being
        /// unguessable, and for casual runs the server records whatever seed the
        /// client reports rather than trusting it.
        /// </summary>
        private static ulong GenerateLocalSeed()
        {
            unchecked
            {
                var rng = new System.Random();
                var hi = (ulong)(uint)rng.Next(int.MinValue, int.MaxValue);
                var lo = (ulong)(uint)rng.Next(int.MinValue, int.MaxValue);

                var mixed = (hi << 32) | lo;
                mixed ^= (ulong)Generation * 0x9E3779B97F4A7C15UL;
                mixed ^= (ulong)(uint)Time.realtimeSinceStartup.GetHashCode() << 16;

                return mixed;
            }
        }
    }
}

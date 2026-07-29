// -----------------------------------------------------------------------------
// MsRng -- counter-based deterministic RNG for the MedaShooter 2.0 schedule.
//
// MIRROR FILE. Its line-for-line Python twin is backend/app/services/ms_rng.py.
// Any edit here MUST be made there in the same commit, or the schedule mirror
// silently drifts and honest runs start reading as divergent.
//
// THREE RULES THIS FILE EXISTS TO ENFORCE, all learned the hard way:
//
// 1. NO `using UnityEngine`. This file must compile standalone under plain
//    dotnet so the parity harness can run it against the Python mirror without
//    a ~10 minute WebGL build. The moment a Unity type appears here that stops
//    being possible and parity becomes unverifiable in practice, which means
//    unverified.
//
// 2. INTEGER ONLY. No float or double may cross the mirror. C# on IL2CPP/wasm
//    and CPython agree on unchecked integer arithmetic by specification --
//    wrapping multiply, logical shift, two's complement -- and they do NOT
//    agree on float codegen. Verified on this machine: uint32 wrap-multiply +
//    xorshift, uint64 the same, signed reinterpretation and negative->unsigned
//    conversion all match Python-with-masking exactly.
//
// 3. NO REJECTION LOOPS. Every draw consumes exactly one hash evaluation at a
//    caller-supplied index. The shipped weighted pickers elsewhere in this
//    project use `do { ... } while (randomWeight == sum)` (PerkDatabaseAsset,
//    PowerupDatabaseAsset, DropDatabaseAsset); that pattern makes the number of
//    draws depend on the draw values, so a mirror cannot know how far the
//    stream advanced. Never port it here.
//
// The generator is counter-based (stateless), not a sequential stream. A
// sequential stream would force the mirror to reproduce the ORDER in which
// unrelated decisions were interleaved, and in this game that order is
// player-dependent -- the spawn gate opens as fast as the player clears the
// field. Counter-based indexing means each decision domain is addressed
// independently by an ordinal the server can reconstruct, so pacing may vary
// while the schedule stays identical.
// -----------------------------------------------------------------------------

namespace Determinism
{
    public static class MsRng
    {
        // Stream ids. Each decision domain gets its own so that adding a draw to
        // one domain can never shift another domain's sequence.
        // Append only -- never renumber, or every historical seed reinterprets.
        public const uint StreamWaveSelect = 1;

        // SplitMix64-style finalizer. Chosen over xorshift/PCG because it is
        // stateless by construction (exactly what counter-based indexing wants),
        // it is four multiplies and four shifts, and every constant is odd so
        // each multiply is invertible mod 2^64 -- no index can collapse to zero.
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;

        /// <summary>
        /// Pure hash of (seed, stream, index) -> 64 uniform bits.
        /// Same three inputs always give the same output, on any machine, forever.
        /// </summary>
        public static ulong Hash(ulong seed, uint stream, uint index)
        {
            unchecked
            {
                // Fold stream and index into one 64-bit counter. The stream sits
                // in the high half so that stream 1 index 0 and stream 0 index 1
                // cannot alias -- an aliasing pair would make two different
                // decisions share a value and correlate visibly in play.
                ulong counter = ((ulong)stream << 32) | index;

                ulong z = seed + (counter + 1UL) * GoldenGamma;
                z = (z ^ (z >> 30)) * MixA;
                z = (z ^ (z >> 27)) * MixB;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Uniform-ish integer in [0, bound). Lemire multiply-shift: one hash, no
        /// rejection, so the draw count is fixed at exactly one.
        ///
        /// The residual modulo bias is at most bound / 2^32. For the wave tables
        /// here (bound is a per-mille weight total in the low hundred-thousands)
        /// that is under one part in ten thousand -- far below anything a player
        /// could perceive, and worth paying to keep the draw count constant.
        /// A rejection loop would remove the bias and break the mirror; that is a
        /// bad trade.
        /// </summary>
        public static uint Below(ulong seed, uint stream, uint index, uint bound)
        {
            if (bound == 0)
                return 0;

            unchecked
            {
                uint bits = (uint)(Hash(seed, stream, index) >> 32);
                return (uint)(((ulong)bits * bound) >> 32);
            }
        }
    }
}

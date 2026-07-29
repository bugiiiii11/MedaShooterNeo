using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Determinism;

namespace ScheduleParity
{
    /// <summary>
    /// Emits golden vectors for the C# side of the schedule mirror. The Python
    /// twin (backend/tests/test_ms_schedule_parity.py) reads the file and
    /// asserts every value matches.
    ///
    /// Output is written to a FILE, never to stdout, and with an explicit "\n".
    /// On Windows, Python's text-mode stdout rewrites "\n" to "\r\n" while
    /// C#'s does not, so a naive stdout-to-stdout diff reports total divergence
    /// between two implementations that actually agree perfectly. Writing bytes
    /// to a file with a pinned newline removes that trap.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var outPath = args.Length > 0
                ? args[0]
                : Path.Combine(AppContext.BaseDirectory, "csharp_vectors.json");

            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"schedule_version\": ").Append(MsSchedule.ScheduleVersion).Append(",\n");

            // --- raw RNG vectors -------------------------------------------------
            // Checked before the schedule so a failure points at the generator
            // rather than at the selection logic on top of it.
            sb.Append("  \"rng\": [\n");
            var rngFirst = true;
            foreach (var seed in Seeds())
            {
                foreach (uint stream in new uint[] { 0u, 1u, 7u, uint.MaxValue })
                {
                    foreach (uint index in new uint[] { 0u, 1u, 2u, 1000u, 65535u, uint.MaxValue })
                    {
                        if (!rngFirst) sb.Append(",\n");
                        rngFirst = false;

                        var h = MsRng.Hash(seed, stream, index);
                        var b = MsRng.Below(seed, stream, index, 118712u);

                        sb.Append("    {\"seed\": ").Append(U64(seed))
                          .Append(", \"stream\": ").Append(stream)
                          .Append(", \"index\": ").Append(index)
                          .Append(", \"hash\": ").Append(U64(h))
                          .Append(", \"below\": ").Append(b)
                          .Append('}');
                    }
                }
            }
            sb.Append("\n  ],\n");

            // --- schedule vectors ------------------------------------------------
            sb.Append("  \"schedules\": [\n");
            var schedFirst = true;
            var profiles = Profiles();

            foreach (var kv in profiles)
            {
                foreach (var seed in Seeds())
                {
                    foreach (uint transitions in new uint[] { 1u, 9u, 10u, 11u, 36u, 200u })
                    {
                        if (!schedFirst) sb.Append(",\n");
                        schedFirst = false;

                        var seq = MsSchedule.BuildSequence(seed, kv.Value, transitions);

                        sb.Append("    {\"profile\": \"").Append(kv.Key).Append('"')
                          .Append(", \"seed\": ").Append(U64(seed))
                          .Append(", \"transitions\": ").Append(transitions)
                          .Append(", \"digest\": ").Append(U64(MsSchedule.SequenceDigest(seq)))
                          .Append(", \"sequence\": [");

                        for (var i = 0; i < seq.Length; i++)
                        {
                            if (i > 0) sb.Append(", ");
                            sb.Append(seq[i].ToString(CultureInfo.InvariantCulture));
                        }

                        sb.Append("]}");
                    }
                }
            }
            sb.Append("\n  ]\n}\n");

            File.WriteAllText(outPath, sb.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
            Console.WriteLine($"wrote {outPath}");
            return 0;
        }

        private static string U64(ulong v) => v.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Seeds chosen to hit the arithmetic edges, not just "some numbers":
        /// zero, one, the 32-bit boundary where a careless cast would truncate,
        /// the sign bit where a signed/unsigned confusion would flip, and the
        /// all-ones value where a missing mask would overflow.
        /// </summary>
        private static IEnumerable<ulong> Seeds()
        {
            yield return 0UL;
            yield return 1UL;
            yield return 0xFFFFFFFFUL;          // 2^32 - 1
            yield return 0x100000000UL;         // 2^32
            yield return 0x8000000000000000UL;  // sign bit set
            yield return 0xFFFFFFFFFFFFFFFFUL;  // all ones
            yield return 0x0123456789ABCDEFUL;
            yield return 0xDEADBEEFCAFEBABEUL;
        }

        private static Dictionary<string, MsScheduleProfile> Profiles()
        {
            return new Dictionary<string, MsScheduleProfile>
            {
                // The real shipped shape: 10 campaign waves, 6 playable endless
                // entries. Weights are the integers derived from the current
                // UnendingWavesProfile cooldown ranges.
                ["default"] = new MsScheduleProfile
                {
                    CampaignWaveCount = 10,
                    EndlessIndices = new uint[] { 0, 1, 2, 3, 4, 5 },
                    EndlessWeights = new uint[] { 21336, 20403, 25229, 10796, 20273, 20675 },
                },

                // Level 2 campaign is one wave longer -- the switch-to-endless
                // transition must land in a different place.
                ["level2"] = new MsScheduleProfile
                {
                    CampaignWaveCount = 11,
                    EndlessIndices = new uint[] { 0, 1, 2, 3, 4, 5 },
                    EndlessWeights = new uint[] { 21336, 20403, 25229, 10796, 20273, 20675 },
                },

                // Degenerate shapes. These exist because the shipped spawner has
                // explicit guards for them and the mirror must agree on what the
                // guards do, not just on the happy path.
                ["single"] = new MsScheduleProfile
                {
                    CampaignWaveCount = 3,
                    EndlessIndices = new uint[] { 4 },
                    EndlessWeights = new uint[] { 999 },
                },
                ["empty"] = new MsScheduleProfile
                {
                    CampaignWaveCount = 2,
                    EndlessIndices = new uint[0],
                    EndlessWeights = new uint[0],
                },
                // A zero-weight entry must be unreachable, never a divide-by-zero
                // and never a silent fallthrough to the last index.
                ["zeroweight"] = new MsScheduleProfile
                {
                    CampaignWaveCount = 4,
                    EndlessIndices = new uint[] { 0, 1, 2 },
                    EndlessWeights = new uint[] { 500, 0, 500 },
                },
            };
        }
    }
}

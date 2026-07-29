using UnityEngine;

namespace Determinism
{
    /// <summary>
    /// Turns the live wave ScriptableObjects into the integer-only
    /// <see cref="MsScheduleProfile"/> that both the game and the backend mirror
    /// consume.
    ///
    /// NOT a mirror file -- it uses UnityEngine and is excluded from the parity
    /// harness on purpose. It is the ONE place float wave data becomes integer
    /// schedule data.
    ///
    /// THE RULE THIS FILE ENFORCES: THE WEIGHTS ARE COMPUTED EXACTLY ONCE.
    ///
    /// The backend must never derive weights from the float asset values itself.
    /// The shipped EnemySpawner.WaveDensityPerMinute computes entirely in
    /// float32; any restatement of it in double produces different intermediate
    /// values (endless entry 2 gives a mean of 2.378209352493286 in float32
    /// against 2.378209412097931 in double). With today's asset values both
    /// still land on the same integer, but only by luck -- one balance tweak
    /// near a rounding boundary would split client and server silently, and the
    /// symptom would be honest players reading as cheaters.
    ///
    /// So: this function runs in Unity, its integer output is what the game
    /// plays, and the same output is exported for the server to load verbatim.
    /// </summary>
    public static class MsScheduleBuilder
    {
        /// <summary>
        /// Density is scaled by this before rounding to integer. 1000 keeps the
        /// weight total comfortably inside uint while preserving three decimal
        /// digits of the original density -- the resulting shift in selection
        /// probability is under one part in twenty thousand, which is orders of
        /// magnitude below both perceptibility and the run-to-run variance of
        /// the draw itself.
        /// </summary>
        private const float WeightScale = 1000f;

        /// <summary>Matches EnemySpawner.MinSpawnGapSeconds.</summary>
        private const float MinSpawnGapSeconds = 0.15f;

        /// <summary>
        /// A wave is playable only if it can actually put an enemy on the field.
        /// Checking the probability alone is not enough (a wave can hold a real
        /// prefab at probability 0) and checking the prefab alone is not enough
        /// either (GetEnemyRandomByProbability short-circuits to the first
        /// spawnable entry when every probability is 0).
        ///
        /// Kept identical to EnemySpawner's own check -- if the two ever
        /// disagree the game and the mirror would draw from different candidate
        /// sets, which is the worst possible kind of drift because the wave
        /// numbers would still look plausible.
        /// </summary>
        public static bool IsWavePlayable(EnemyWave wave)
        {
            if (wave == null || wave.Enemies == null || wave.Enemies.Count == 0)
                return false;

            for (var i = 0; i < wave.Enemies.Count; i++)
            {
                var enemy = wave.Enemies[i];

                if (enemy != null && enemy.Prefab != null && enemy.ProbabilityInWave > 0f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Expected enemies per minute at SpawnRateFactor 1, the selection
        /// weight. Float32 throughout, matching the shipped implementation
        /// exactly.
        ///
        /// The zero-cooldown guard is not cosmetic: a wave authored with
        /// SpawnCooldownRange {0,0} yields 60/0 = infinity, and an infinite
        /// weight would win every draw. Treating it as the fastest allowed
        /// cadence keeps it finite.
        /// </summary>
        public static float WaveDensityPerMinute(EnemyWave wave)
        {
            var meanCooldown = (wave.SpawnCooldownRange.x + wave.SpawnCooldownRange.y) * 0.5f;
            meanCooldown = Mathf.Max(meanCooldown, MinSpawnGapSeconds);

            return Mathf.Clamp(60f / meanCooldown, 1f, 60f);
        }

        /// <summary>
        /// Builds the integer profile. <paramref name="campaignWaveCount"/> is
        /// the number of entries in the CAMPAIGN profile, which is where the
        /// switch to endless happens.
        /// </summary>
        public static MsScheduleProfile Build(EnemyWavesProfile endlessProfile, int campaignWaveCount)
        {
            var profile = new MsScheduleProfile
            {
                CampaignWaveCount = (uint)Mathf.Max(0, campaignWaveCount),
            };

            if (endlessProfile == null || endlessProfile.Waves == null)
                return profile;

            var indices = new System.Collections.Generic.List<uint>();
            var weights = new System.Collections.Generic.List<uint>();

            for (var i = 0; i < endlessProfile.Waves.Count; i++)
            {
                var wave = endlessProfile.Waves[i];

                // Silent waves are scripted breathers -- they suppress the
                // per-wave stat upgrade and the difficulty tick -- so they must
                // never come up in a random rotation. The endless profile has
                // none today; this keeps that true if one is ever authored.
                if (wave == null || wave.IsSilent || !IsWavePlayable(wave))
                    continue;

                var weight = (uint)Mathf.Max(1, Mathf.RoundToInt(WaveDensityPerMinute(wave) * WeightScale));

                indices.Add((uint)i);
                weights.Add(weight);
            }

            profile.EndlessIndices = indices.ToArray();
            profile.EndlessWeights = weights.ToArray();

            if (profile.EndlessIndices.Length == 0)
                Debug.LogError("[MsSchedule] The endless profile has no playable waves; falling back to index 0.");

            return profile;
        }

        /// <summary>
        /// Compact, loggable description of the profile. This is what gets
        /// compared against the backend's copy when diagnosing a reported
        /// divergence -- if the weights differ, the mirror was never the problem.
        /// </summary>
        public static string Describe(MsScheduleProfile profile)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("campaign=").Append(profile.CampaignWaveCount).Append(" endless=[");

            for (var i = 0; i < profile.EndlessIndices.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(profile.EndlessIndices[i]).Append(':').Append(profile.EndlessWeights[i]);
            }

            return sb.Append("] total=").Append(profile.WeightTotal).ToString();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnendingWavesProfile : EnemyWavesProfile
{
    public EnemyWavesProfile PreviousProfile;
    public List<MinibossWave> Minibosses;

    public override void CalculateIndices()
    {
        var index = PreviousProfile.Waves[^1].Index + 1;
        var difficulty = PreviousProfile.Waves[^1].WaveDifficulty + 1;

        for (var i = 0; i < Waves.Count; i++)
        {
            var wave = Waves[i];

            wave.WaveDifficulty = difficulty++;

            if (wave.IsSilent)
            {
                wave.Index = index - 1;
                continue;
            }

            wave.Index = index++;
        }
    }
}

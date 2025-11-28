using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EnemyWavesProfile : ScriptableObject
{
    public List<EnemyWave> Waves;

    public int GetIndexForWave(EnemyWave wave)
    {
        var index = 0;
        for(var i = 0; i < Waves.Count; i++)
        {
            if (Waves[i].IsSilent)
            {
                if (Waves[i] == wave)
                {
                    return index - 1;
                }
                continue;
            }
            else if (Waves[i] == wave)
                return index;

            index++;
        }

        return -1;
    }

    [Button("Calc Indices")]
    public virtual void CalculateIndices()
    {
        var index = 0;
        var difficulty = 0;
        for (var i = 0; i < Waves.Count; i++)
        {
            var wave = Waves[i];

            wave.WaveDifficulty = difficulty++;
            
            if(wave.IsSilent)
            {
                wave.Index = index - 1;
                continue;
            }

            wave.Index = index++;
        }
    }
}



[System.Serializable]
public class MinibossWave
{
    [Tooltip("Wont trigger any perks and upgrades")]
    public bool IsSilent = false;
    public int WaveIndexAt = 15;
    [MinMaxSlider(0, 10, true)]
    public Vector2 SpawnCooldownRange;
    public List<Enemy> Enemies;

    public Enemy GetMiniboss() => Enemies[0];
}

    [System.Serializable]
public class EnemyWave
{
    public int Index = 0;
    public bool IsBoss = false;

    public int WaveDifficulty;
    [Tooltip("Wont trigger any perks and upgrades")]
    public bool IsSilent = false;
    public int MaxEnemyCount = 4;

    [Tooltip("Number of enemies to spawn in this wave and switch to another")]
    public int EnemyQuantity = 30;

    public bool SpawnMines = false;

    [ShowIf("SpawnMines", true)]
    [MinMaxSlider(0, 20, true)]
    public Vector2 MineSpawnCooldownRange;

    [MinMaxSlider(0, 10, true)]
    public Vector2 SpawnCooldownRange;
    public List<Enemy> Enemies;


    public Enemy GetEnemyRandomByProbability()
    {
        float sum = 0;
        foreach(var enemy in Enemies)
        {
            sum += enemy.ProbabilityInWave;
        }

        if(sum == 0)
        {
            return Enemies[0];
        }

        float randomWeight = 0;
        do
        {
            if(sum == 0)
                return null;
            randomWeight = Random.Range(0, sum);
        }
        while(randomWeight == sum);

        foreach(var enemy in Enemies)
        {
            if(randomWeight < enemy.ProbabilityInWave)
                return enemy;
            randomWeight -= enemy.ProbabilityInWave;
        }

        return null;
    }

#if UNITY_EDITOR
    [Button("Get Player Attributes")]
    public void GetPlayerAttrs()
    {
        var damageCalc = new DamageCalculator();
        damageCalc.Wave = GameObject.FindObjectOfType<EnemySpawner>().Profile.GetIndexForWave(this);

        damageCalc.Weapon = GameObject.FindObjectOfType<PlayerMovement>().GetComponent<WeaponController>().GetCurrentWeapon();
        damageCalc.Calculate();
        UnityEditor.EditorUtility.DisplayDialog("Results", damageCalc.Result, "Ok");
    }
#endif
}

[System.Serializable]
public class Enemy
{
    public GameObject Prefab;

    public Vector2Int DamageRange;

    public Vector2Int HitPointsRange;

    public ProjectileData AdditionalAttackData;

    public int RewardPoints;

    [Range(0,1)]
    public float ProbabilityInWave = 0;

    internal Enemy ScaleParams()
    {
        var copy = (Enemy) this.MemberwiseClone();

        var hpRange= new Vector2Int();
        hpRange.x = Mathf.RoundToInt(HitPointsRange.x * DifficultyScaling.CurrentEnemyDynamicFactor);
        hpRange.y = Mathf.RoundToInt(HitPointsRange.y * DifficultyScaling.CurrentEnemyDynamicFactor);
        copy.HitPointsRange = hpRange;

        var dmgRange = new Vector2Int();
        dmgRange.x = Mathf.RoundToInt(DamageRange.x * DifficultyScaling.CurrentEnemyDynamicFactor);
        dmgRange.y = Mathf.RoundToInt(DamageRange.y * DifficultyScaling.CurrentEnemyDynamicFactor);
        copy.DamageRange = dmgRange;

        return copy;
    }
}
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PowerupDatabaseAsset : ScriptableObject
{
    public List<Powerup> Powerups;

    public Powerup GetPowerupRandomByProbability()
    {
        float sum = 0;
        foreach(var p in Powerups)
        {
            sum += p.ProbabilityRange;
        }

        float randomWeight = 0;
        do
        {
            if(sum == 0)
                return null;
            randomWeight = Random.Range(0, sum);
        }
        while(randomWeight == sum);

        foreach(var p in Powerups)
        {
            if(randomWeight < p.ProbabilityRange)
                return p;
            randomWeight -= p.ProbabilityRange;
        }

        return null;
    }
}

[System.Serializable]
public class Powerup
{
    [VerticalGroup]
    public string Title;

    [VerticalGroup, MultiLineProperty]
    public string Description;
    
    [VerticalGroup,PreviewField(100, ObjectFieldAlignment.Left)]
    public GameObject AssetPrefab;

    [Range(0,1)]
    public float ProbabilityRange;
}

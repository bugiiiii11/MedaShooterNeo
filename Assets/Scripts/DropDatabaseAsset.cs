using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// nefunguje v editore?
public class DropDatabaseAsset : ScriptableObject
{
    public List<Drop> Drops;
    public Drop GetByProbability()
    {
        float sum = 0;
        foreach (var p in Drops)
        {
            sum += p.Probability;
        }

        float randomWeight;
        do
        {
            if (sum == 0)
                return null;
            randomWeight = Random.Range(0, sum);
        }
        while (randomWeight == sum);

        foreach (var p in Drops)
        {
            if (randomWeight < p.Probability)
                return p;
            randomWeight -= p.Probability;
        }

        return null;
    }
}

[System.Serializable]
public class Drop
{
    [VerticalGroup]
    public string Title;

    [VerticalGroup, MultiLineProperty]
    public string Description;

    [Range(0, 1f)]
    public float Probability;

    [VerticalGroup,PreviewField(100, ObjectFieldAlignment.Left)]
    public GameObject AssetPrefab;
}

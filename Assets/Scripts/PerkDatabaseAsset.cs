using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;


public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField, HideInInspector]
	private List<TKey> keyData = new List<TKey>();
	
	[SerializeField, HideInInspector]
	private List<TValue> valueData = new List<TValue>();

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
		this.Clear();
		for (int i = 0; i < this.keyData.Count && i < this.valueData.Count; i++)
		{
			this[this.keyData[i]] = this.valueData[i];
		}
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
		this.keyData.Clear();
		this.valueData.Clear();

		foreach (var item in this)
		{
			this.keyData.Add(item.Key);
			this.valueData.Add(item.Value);
		}
    }
}

[System.Serializable]
public class RarityProbabilityDictionary : UnitySerializedDictionary<Rarity, float> { }

public class PerkDatabaseAsset : ScriptableObject
{
    public RarityProbabilityDictionary RarityProbabilities;
    public List<Perk> Perks;

    private Perk GetRandomByProb(IEnumerable<Perk> list)
    {
        list = list.Where(x => !x.IsDisabled);
        float sum = 0;
        foreach (var p in list)
        {
            sum += RarityProbabilities[p.Rarity];
        }

        float randomWeight;
        do
        {
            if (sum == 0)
                return null;
            randomWeight = UnityEngine.Random.Range(0, sum);
        }
        while (randomWeight == sum);

        foreach (var p in list)
        {
            if (randomWeight < RarityProbabilities[p.Rarity])
                return p;
            randomWeight -= RarityProbabilities[p.Rarity];
        }

        return null;
    }
    
    public Perk GetPowerupRandomByProbability(IEnumerable<Perk> except, IList<Rarity> allowedRarities)
    {
        if (except == null)
            return GetRandomByProb(Perks.Where(x => allowedRarities.Contains(x.Rarity) && !x.IsBuildup));
        else
            return GetRandomByProb(Perks.Where(x => allowedRarities.Contains(x.Rarity) && !x.IsBuildup).Except(except));
    }

    public Perk GetBuildupPerkByProbability(IEnumerable<Perk> except, IList<Rarity> allowedRarities)
    {
        if (except == null)
            return GetRandomByProb(Perks.Where(x => allowedRarities.Contains(x.Rarity) && x.IsBuildup));
        else
            return GetRandomByProb(Perks.Where(x => allowedRarities.Contains(x.Rarity) && x.IsBuildup).Except(except));
    }

    public Perk GetPowerupRandomByProbability(IEnumerable<Perk> except, IList<Rarity> allowedRarities, Rarity exceptRarity)
    {
        /*if (except == null)
            return GetRandomByProb(Perks.Except(Perks.Where(x => x.Rarity == exceptRarity)).Where(x => allowedRarities.Contains(x.Rarity)));
        else
            return GetRandomByProb(Perks.Except(Perks.Where(x => x.Rarity == exceptRarity)).Where(x => allowedRarities.Contains(x.Rarity) && !except.Contains(x)));*/

        var rarityExcepted = Perks.Where(x => x.Rarity != exceptRarity).ToList();
        var perksExcepted = rarityExcepted.Except(except).ToList();

        var allPerks = perksExcepted.Where(x => allowedRarities.Contains(x.Rarity)).ToList();

        return GetRandomByProb(allPerks);
    }

    internal Perk GetPassivePerk()
    {
        return Perks.Where(x => x.Behaviour is PassivePerkBehaviour).ToList().Random();
    }
}

public enum Rarity : byte
{
    Basic = 0,
    Rare = 1,
    Epic = 2,
}

[System.Serializable]
public class Perk
{
    [VerticalGroup]
    public string Title;

    [VerticalGroup, MultiLineProperty]
    public string Description;

    [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
    public Rarity Rarity;

    public PerkBehaviour Behaviour;
    
    [HorizontalGroup, InlineEditor(InlineEditorModes.FullEditor)]
    public Sprite Icon;
    public Sprite Background;

    public bool IsBuildup = false;
    public bool IsDisabled;
}
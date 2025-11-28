using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InventoryConfig : ScriptableObject
{
    public enum ConfigType
    {
        Heores,
        Weapons,
        Stats
    }

    public ConfigType Type;
    public string Name;
    public List<INft> Nfts { get; set; }

    public InventoryConfig()
    {
        Nfts = new List<INft>();
    }

    public static InventoryConfig Default
    {
        get
        {
            var conf = ScriptableObject.CreateInstance<InventoryConfig>();
            conf.Type = ConfigType.Heores;
            conf.Name = "Heroes";
            conf.Nfts = new List<INft>();
            return conf;
        }
    }

    internal void Clear()
    {
        Nfts.Clear();
    }

    internal void Add(INft hero)
    {
        Nfts.Add(hero);
    }
}

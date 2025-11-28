using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NftWeaponInventory
{
    //[JsonProperty("id")]
    //public long? Id { get; set; }

    //[JsonProperty("bc_id")]
    //public long? BcId { get; set; }

    //[JsonProperty("owner_address")]
    //public string OwnerAddress { get; set; }

    //[JsonProperty("contract_address")]
    //public string ContractAddress { get; set; }

    //[JsonProperty("weapon_tier")]
    //public long? WeaponTier { get; set; }

    //[JsonProperty("weapon_type")]
    //public long? WeaponType { get; set; }

    //[JsonProperty("weapon_subtype")]
    //public long? WeaponSubtype { get; set; }

    [JsonProperty("weapon_name")]
    public string WeaponName { get; set; }

    [JsonProperty("category_name")]
    public string CategoryName { get; set; }

    //[JsonProperty("subtype_name")]
    //public string SubtypeName { get; set; }

    //[JsonProperty("category")]
    //public long? Category { get; set; }

    //[JsonProperty("serial_number")]
    //public long? SerialNumber { get; set; }

    //[JsonProperty("token_game_exp")]
    //public long? TokenGameExp { get; set; }

    //[JsonProperty("token_trade_exp")]
    //public long? TokenTradeExp { get; set; }

    [JsonProperty("security")]
    public long? Security { get; set; }

    [JsonProperty("anonymity")]
    public long? Anonymity { get; set; }

    [JsonProperty("innovation")]
    public long? Innovation { get; set; }

    [JsonProperty("burned")]
    public bool Burned { get; set; }

    [JsonProperty("minted")]
    public bool Minted { get; set; }
    /*
    [JsonProperty("assets")]
    public List<WeaponAsset> Assets { get; set; }*/
}

public class WeaponAsset
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }
}
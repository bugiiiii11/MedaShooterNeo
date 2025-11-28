using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This will be serialized into a JSON Address object
public class Ability
{
    [JsonProperty("game_id")]
    public string GameId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("boost_value")]
    public float BoostValue { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class BoostPackage
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("abilities")]
    public List<Ability> Abilities { get; set; }

    [JsonProperty("duration")]
    public int Duration { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
}
using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cryptomeda.NFT.Json
{
    public partial class NftInventory
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("next")]
        public Uri Next { get; set; }

        [JsonProperty("previous")]
        public object Previous { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("bc_id")]
        public long? BcId { get; set; }

        [JsonProperty("mint_index")]
        public long? MintIndex { get; set; }

        [JsonProperty("item_count")]
        public long ItemCount { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("usage")]
        public string Usage { get; set; }

        [JsonProperty("quote")]
        public string Quote { get; set; }

        [JsonProperty("fraction")]
        public Fraction Fraction { get; set; }

        [JsonProperty("medawars_ability")]
        public string MedawarsAbility { get; set; }

        [JsonProperty("on_sale")]
        public bool OnSale { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("season")]
        public Season Season { get; set; }

        [JsonProperty("assets")]
        public List<Asset> Assets { get; set; }

        [JsonProperty("reward")]
        public Reward Reward { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public partial class Asset
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }

    public partial class Metadata
    {
        [JsonProperty("sec")]
        public long? Sec { get; set; }

        [JsonProperty("ano")]
        public long? Ano { get; set; }

        [JsonProperty("inn")]
        public long? Inn { get; set; }

        [JsonProperty("collection_itm_id")]
        public long CollectionItmId { get; set; }

        [JsonProperty("revolution")]
        public bool Revolution { get; set; }

        [JsonProperty("category")]
        public Category Category { get; set; }
    }

    public partial class Reward
    {
        [JsonProperty("power")]
        public long? Power { get; set; }

        [JsonProperty("total_amount")]
        public long TotalAmount { get; set; }

        [JsonProperty("daily_amount")]
        public long DailyAmount { get; set; }

        [JsonProperty("days_claimed")]
        public long DaysClaimed { get; set; }

        [JsonProperty("total_claimed")]
        public long TotalClaimed { get; set; }

        [JsonProperty("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [JsonProperty("amount_to_claim")]
        public long AmountToClaim { get; set; }
    }

    public partial class Season
    {
        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cmty_card_count")]
        public long? CmtyCardCount { get; set; }

        [JsonProperty("cmty_card_cap")]
        public long? CmtyCardCap { get; set; }

        [JsonProperty("rev_card_cap")]
        public long? RevCardCap { get; set; }

        [JsonProperty("start_time")]
        public DateTimeOffset? StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTimeOffset? EndTime { get; set; }

        [JsonProperty("revolution_mintable")]
        public bool RevolutionMintable { get; set; }
    }

    public enum Fraction { Goliath, Other, Renegade };

    public enum Category { Community, Influencer, Revolution };

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                FractionConverter.Singleton,
                CategoryConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class FractionConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Fraction) || t == typeof(Fraction?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "GOLIATH":
                    return Fraction.Goliath;
                case "OTHER":
                    return Fraction.Other;
                case "RENEGADE":
                    return Fraction.Renegade;
            }
            throw new Exception("Cannot unmarshal type Fraction");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Fraction)untypedValue;
            switch (value)
            {
                case Fraction.Goliath:
                    serializer.Serialize(writer, "GOLIATH");
                    return;
                case Fraction.Other:
                    serializer.Serialize(writer, "OTHER");
                    return;
                case Fraction.Renegade:
                    serializer.Serialize(writer, "RENEGADE");
                    return;
            }
            throw new Exception("Cannot marshal type Fraction");
        }

        public static readonly FractionConverter Singleton = new FractionConverter();
    }

    internal class CategoryConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Category) || t == typeof(Category?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "COMMUNITY":
                    return Category.Community;
                case "INFLUENCER":
                    return Category.Influencer;
                case "REVOLUTION":
                    return Category.Revolution;
            }
            throw new Exception("Cannot unmarshal type Category");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Category)untypedValue;
            switch (value)
            {
                case Category.Community:
                    serializer.Serialize(writer, "COMMUNITY");
                    return;
                case Category.Influencer:
                    serializer.Serialize(writer, "INFLUENCER");
                    return;
                case Category.Revolution:
                    serializer.Serialize(writer, "REVOLUTION");
                    return;
            }
            throw new Exception("Cannot marshal type Category");
        }

        public static readonly CategoryConverter Singleton = new CategoryConverter();
    }
}

using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cryptomeda.NFT.Json
{
    public partial class StakingInfo
    {
        [JsonProperty("standard")]
        public Standard Standard { get; set; }

        [JsonProperty("liquidity")]
        public Liquidity Liquidity { get; set; }
    }

    public partial class Liquidity
    {
        //[JsonProperty("locked_balance")]
        //public long? LockedBalance { get; set; }

        //[JsonProperty("unlocked_balance")]
        //public long? UnlockedBalance { get; set; }

        //[JsonProperty("locked_liquidity")]
        //public long? LockedLiquidity { get; set; }

        //[JsonProperty("unlocked_liquidity")]
        //public long? UnlockedLiquidity { get; set; }

        [JsonProperty("currently_staked")]
        public long? CurrentlyStaked { get; set; }

        //[JsonProperty("total_earned")]
        //public long? TotalEarned { get; set; }

        //[JsonProperty("cards_won")]
        //public long? CardsWon { get; set; }

        //[JsonProperty("contract_address")]
        //public string ContractAddress { get; set; }

        //[JsonProperty("stakes")]
        //public List<Stake> Stakes { get; set; }
    }

    public partial class Stake
    {
        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("bc_id")]
        public long? BcId { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("claimable_amount")]
        public double? ClaimableAmount { get; set; }
    }

    public partial class Standard
    {
        //[JsonProperty("locked_balance")]
        //public long? LockedBalance { get; set; }

        //[JsonProperty("unlocked_balance")]
        //public long? UnlockedBalance { get; set; }

        [JsonProperty("currently_staked")]
        public long? CurrentlyStaked { get; set; }

        //[JsonProperty("total_earned")]
        //public long? TotalEarned { get; set; }

        //[JsonProperty("cards_won")]
        //public long? CardsWon { get; set; }

        //[JsonProperty("contract_address")]
        //public string ContractAddress { get; set; }

        //[JsonProperty("stakes")]
        //public List<Stake> Stakes { get; set; }
    }
}
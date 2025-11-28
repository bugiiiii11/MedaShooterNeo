using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class JsonBuilder
{
    public static string BuildScore(uint score, GameStats stats, string address, int unityDuration, int serverDuration)
    {
        var addr = Asymmetric.RSA.Encrypt(address, DataEncryption.PUBLIC_KEY);
        var hash = Asymmetric.RSA.Encrypt(score.ToString(), DataEncryption.PUBLIC_KEY);

        var delta = Asymmetric.RSA.Encrypt(unityDuration.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var delta2 = Asymmetric.RSA.Encrypt(serverDuration.ToString(), DataEncryption.GLOB_PUBLIC_KEY);


        var parameter1 = Asymmetric.RSA.Encrypt(stats.EnemiesSpawned.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter2 = Asymmetric.RSA.Encrypt(stats.EnemiesKilled.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter3 = Asymmetric.RSA.Encrypt(stats.WavesCount.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter4 = Asymmetric.RSA.Encrypt( Mathf.RoundToInt(stats.DistanceTraveled).ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter5 = Asymmetric.RSA.Encrypt(stats.PerksCollected.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        
        var parameter6 = Asymmetric.RSA.Encrypt(UINumbersHandler.instance.FullCoins.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter7 = Asymmetric.RSA.Encrypt(stats.ShieldsCollected.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter8 = Asymmetric.RSA.Encrypt(stats.LongestKillingSpreeMult.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter9 = Asymmetric.RSA.Encrypt(stats.LongestKillingSpreeDuration.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter10 = Asymmetric.RSA.Encrypt(stats.MaxKillingSpree.ToString(), DataEncryption.GLOB_PUBLIC_KEY);

        var parameter11 = Asymmetric.RSA.Encrypt(stats.AttackSpeed.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter12 = Asymmetric.RSA.Encrypt(stats.MaxScorePerEnemy.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter13 = Asymmetric.RSA.Encrypt(stats.MaxScorePerEnemyScaled.ToString(), DataEncryption.GLOB_PUBLIC_KEY);
        var parameter14 = Asymmetric.RSA.Encrypt(stats.AbilityUseCount.ToString(), DataEncryption.GLOB_PUBLIC_KEY);

        var parameter15 = Asymmetric.RSA.Encrypt(stats.EnemiesKilledWhileKillingSpree.ToString(), DataEncryption.GLOB_PUBLIC_KEY); // enemies killed with killing spree

        var sb = new StringBuilder();

        sb.Append("{");

        sb.Append("\"hash\":\"").Append(hash).Append("\",");
        sb.Append("\"address\":\"").Append(addr).Append("\",");
        sb.Append("\"delta\":\"").Append(delta).Append("\",");
        sb.Append("\"parameter1\":\"").Append(parameter1).Append("\",");
        sb.Append("\"parameter2\":\"").Append(parameter2).Append("\",");
        sb.Append("\"parameter3\":\"").Append(parameter3).Append("\",");
        sb.Append("\"parameter4\":\"").Append(parameter4).Append("\",");
        sb.Append("\"parameter5\":\"").Append(parameter5).Append("\",");
        sb.Append("\"parameter6\":\"").Append(parameter6).Append("\",");
        sb.Append("\"parameter7\":\"").Append(parameter7).Append("\",");
        sb.Append("\"parameter8\":\"").Append(parameter8).Append("\",");
        sb.Append("\"parameter9\":\"").Append(parameter9).Append("\",");
        sb.Append("\"parameter10\":\"").Append(parameter10).Append("\",");
        sb.Append("\"parameter11\":\"").Append(parameter11).Append("\",");
        sb.Append("\"parameter12\":\"").Append(parameter12).Append("\",");
        sb.Append("\"parameter13\":\"").Append(parameter13).Append("\",");
        sb.Append("\"parameter14\":\"").Append(parameter14).Append("\",");
        sb.Append("\"parameter15\":\"").Append(parameter15).Append('\"');

        sb.Append("}");

        return sb.ToString();
        //return "{\"hash\":\""+hash+"\",\"address\":\""+ addr + "\",\"delta\":\""+delta+"\",\"parameter1\":\""+parameter1+"\",\"parameter2\":\""+ parameter2+ "\",\"parameter3\":\""+ parameter3+ "\",\"parameter4\":\""+ parameter4+ "\",\"parameter5\":\""+ parameter5 + "\"}";
    }

    internal static string BuildCheating(string addr)
    {
        var concatAddress = $"<address>{addr}</address>";
        var rsaAddress = Asymmetric.RSA.Encrypt(concatAddress, DataEncryption.GLOB_PUBLIC_KEY);
        return "{\"address\":\"" + rsaAddress + "\"}";
    }
}

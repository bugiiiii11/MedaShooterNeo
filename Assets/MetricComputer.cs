using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MetricComputer : MonoBehaviour
{
    private void Start()
    {
        InvokeRepeating(nameof(Measurements), 0, 60);
    }

    private void Measurements()
    {
        int score = UINumbersHandler.instance.realScoreValue;
        Debug.Log(BuildScore((uint)score, GameManager.instance.GameStats, 0, 0));
    }

    public static string BuildScore(uint score, GameStats stats, int unityDelta, int serverDelta)
    {
        var addr = "0x2222222222222222222222222222222222222222";
        var hash = score.ToString();
        var delta = unityDelta.ToString();
        var delta2 = serverDelta.ToString();
        var parameter1 = stats.EnemiesSpawned.ToString();
        var parameter2 = stats.EnemiesKilled.ToString();
        var parameter3 = stats.WavesCount.ToString();
        var parameter4 = Mathf.RoundToInt(stats.DistanceTraveled).ToString();
        var parameter5 = stats.PerksCollected.ToString();
        var parameter6 = UINumbersHandler.instance.FullCoins.ToString();
        var parameter7 = stats.ShieldsCollected.ToString();
        var parameter8 = stats.LongestKillingSpreeMult.ToString();
        var parameter9 = stats.LongestKillingSpreeDuration.ToString();
        var parameter10 = stats.MaxKillingSpree.ToString();

        var parameter11 = stats.AttackSpeed.ToString();
        var parameter12 = stats.MaxScorePerEnemy.ToString();
        var parameter13 = stats.MaxScorePerEnemyScaled.ToString();
        var parameter14 = stats.AbilityUseCount.ToString();

        var parameter15 = stats.EnemiesKilledWhileKillingSpree.ToString();

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
    }
}

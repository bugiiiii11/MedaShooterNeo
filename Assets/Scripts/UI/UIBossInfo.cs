using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBossInfo : UIGenericBar
{
    public Image bossHp;
    public TextMeshProUGUI bossPointsText;

    public override void SetValue([ParamRange(0, 1)] float value)
    {
        bossHp.fillAmount = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Sets the reward points display for the current boss
    /// </summary>
    public void SetBossPoints(int points)
    {
        if (bossPointsText != null)
        {
            bossPointsText.text = $"+{points:N0} pts";
        }
    }

    /// <summary>
    /// Calculates boss reward points based on wave number
    /// Wave 15 = 1000, Wave 20 = 2000, Wave 25 = 3000, etc.
    /// </summary>
    public static int CalculateBossPoints(int waveNumber)
    {
        int minibossIndex = (waveNumber - 10) / 5;
        return minibossIndex * 1000;
    }
}

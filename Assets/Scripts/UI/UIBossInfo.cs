using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBossInfo : UIGenericBar
{
    public Image bossHp;

    public override void SetValue([ParamRange(0, 1)] float value)
    {
        bossHp.fillAmount = Mathf.Clamp01(value);
    }
/*
    public void Bind(IBoss boss)
    {
        boss.OnHealthChanged += Boss_OnHealthChanged;
    }

    private void Boss_OnHealthChanged(float current, float max)
    {
        SetPercentage(current, max);
    }*/
}

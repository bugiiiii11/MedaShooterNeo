using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPlayerHealthBar : UIGenericBar
{
    protected override void Start() 
    {
        base.Start();
        GameManager.instance.EventManager.AddListener<PlayerHealthChangeEvent>( ev => 
        {
            SetPercentage(ev.CurrentHp, ev.MaxHp);
        });
    }

    public override void SetPercentage(float currentValue, float maxValue)
    {
        base.SetPercentage(currentValue, maxValue);
    }
}

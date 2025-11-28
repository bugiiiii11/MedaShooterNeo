using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIArmorBar : UIPlayerHealthBar
{
    protected override void Start() 
    {
        SetValue(0);
        GameManager.instance.EventManager.AddListener<PlayerArmorChangeEvent>( ev => 
        {
            base.SetPercentage(ev.CurrentArmor, ev.MaxArmor);
        });
    }
}

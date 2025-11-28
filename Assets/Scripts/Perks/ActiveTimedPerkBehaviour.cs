using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActiveTimedPerkBehaviour : ActivePerkBehaviour
{
    [Header("Duration in seconds")]
    public ObscuredFloat Duration = 30;
    private ObscuredFloat startTime = 0;

    public ObscuredFloat CurrentLenght => Time.time - startTime;

    public override void OnInitialize(PlayerMovement player)
    {
        startTime = Time.time;
    }

    public override void OnUpdate()
    {
        if (CurrentLenght >= Duration)
        {
            UIPerkManager.instance.CancelPerk(this);
        }
    }
}

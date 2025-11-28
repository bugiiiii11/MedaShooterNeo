using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWeaponCountdown : UIGenericBar
{
    public float CurrentTime = 0;
    internal float maxTime = 1;
    protected override void Start()
    {
        base.Start();
        GameManager.instance.EventManager.AddListener<WeaponPowerupTimedEvent>(OnPowerupWeaponCollected);

        gameObject.SetActive(false);
    }

    internal void OnPowerupWeaponCollected(WeaponPowerupTimedEvent obj)
    {
        if (obj.Time < 0)
        {
            CurrentTime = 0;
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        CurrentTime = obj.Time;
        maxTime = obj.Time;
    }

    private void Update()
    {
        if(CurrentTime > 0)
        {
            CurrentTime -= Time.deltaTime;
            SetPercentage(CurrentTime, maxTime);
        }
        else
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }

    internal void ChangeCooldown(float to)
    {
        CurrentTime = to;
    }
}

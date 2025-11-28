using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DotIcon : MonoBehaviour
{
    public Image CooldownFill;
    private bool countdown = false;
    private float currentLength, maxUptime;
    public void SetDotUptime(float uptime)
    {
        maxUptime = uptime;
        currentLength = Time.time;
        countdown = true;
    }

    private void Update()
    {
        if (countdown)
        {
            CooldownFill.fillAmount = 1 - ((Time.time - currentLength) / maxUptime);

            if(CooldownFill.fillAmount <= 0)
            {
                countdown = false;
            }
        }
    }

    internal void SetSprite(Sprite icon)
    {
        if(TryGetComponent<Image>(out var img))
        {
            img.sprite = icon;
        }
    }
}

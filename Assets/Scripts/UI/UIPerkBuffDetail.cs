using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class UIPerkBuffDetail : MonoBehaviour
{
    public TextMeshProUGUI Title, Description, Cooldown, Stacks;
    public void SetFrom(UIPerkBuff buff)
    {
        Title.text = buff.Title;
        Description.text = buff.Description;
        Cooldown.text = buff.Cooldown > 0 ? FormatTime(buff.Cooldown) : "";
        Stacks.text = buff.StackAmount > 1 ? $"{buff.StackAmount} stacks" : "1 stack";

        GetComponent<Image>().sprite = buff.BackgroundImage;
    }

    private string FormatTime(float time)
    {
        var minutes = Mathf.FloorToInt(time / 60);
        var seconds = time - minutes * 60;
        return $"{minutes}:{seconds:00}";
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class CardPreviewAbility : MonoBehaviour
{
    public string Name, HintTooltip;
    public TextMeshProUGUI incrementText;

    internal void SetActive(bool v)
    {
        gameObject.SetActive(v);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElRaccoone.Tweens;

public class Tooltip : MonoBehaviour
{
    public GameObject TooltipChild;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(ShowTooltip);
    }

    private void ShowTooltip()
    {
        TooltipChild.SetActive(!TooltipChild.activeSelf);
    }
}
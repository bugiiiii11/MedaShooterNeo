using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldCounter : MonoBehaviour
{
    private ObscuredFloat currentCount = 0;
    public TMPro.TextMeshPro CountingText;
    public void StartCount(float time)
    {
        // start counting
        currentCount = time;
        CountingText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if(currentCount > 0)
        {
            currentCount -= Time.deltaTime;
            CountingText.text = $"{currentCount:0.00}";

            if(currentCount <= 0)
            {
                CountingText.gameObject.SetActive(false);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITextVisualInput : MonoBehaviour
{
    public TextMeshProUGUI textMesh;

    public float Speed = 1;

    private int value = 0;

    public void StartTypeWriter(int value)
    {
        this.value = value;
        StartCoroutine(StartTypeWriterCo());
    }

    private IEnumerator StartTypeWriterCo()
    {
        var currentDisplayScore = 0f;
        var currentScore = 0f;
        var interpolationValue = 0f;
        while (currentDisplayScore < value)
        {
            currentScore = Mathf.Lerp(0, value, interpolationValue);

            interpolationValue = Mathf.Clamp01(interpolationValue + 1 / Speed);

            if (currentScore >= value)
            {
                textMesh.text = value.ToString("N0");
                yield break;
            }

            textMesh.text = Mathf.RoundToInt(currentScore).ToString("N0");

            yield return null;
        }
    }
}

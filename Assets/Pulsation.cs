using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulsation : MonoBehaviour
{
    public enum Method
    {
        Scale,
        PositionX,
        PositionY,
        PositionZ,
    }

    [Header("Only scale implemented for now!")]
    public float Freq;
    public float Amplitude;
    public Method PulseMethod;
    public Vector2 MinMaxRange;

    private Vector3 defaultValue;

    private void Start()
    {
        switch (PulseMethod)
        {
            case Method.Scale:
                defaultValue = transform.localScale;
                break;
        }
    }

    private void Update()
    {
        switch (PulseMethod)
        {
            case Method.Scale:
                transform.localScale = Freq * ((Mathf.Sin(Amplitude * Time.time)*(MinMaxRange.x*0.5f)) + MinMaxRange.y) * defaultValue;
                break;
        }
    }
}

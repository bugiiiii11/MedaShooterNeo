using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TimeCompute : MonoBehaviour
{
    private float currentTime = 0;
    protected float TimeSinceLastCompute => Time.time - currentTime;

    protected void ComputeTime()
    {
        currentTime = Time.time;
    }
}
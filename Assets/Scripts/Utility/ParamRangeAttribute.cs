using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
sealed class ParamRangeAttribute : System.Attribute
{
    // See the attribute guidelines at
    //  http://go.microsoft.com/fwlink/?LinkId=85236
    readonly float min, max;
    
    // This is a positional argument
    public ParamRangeAttribute(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
    
    public float Minimum
    {
        get { return min; }
    }

    public float Maximum
    {
        get { return max; }
    }
}

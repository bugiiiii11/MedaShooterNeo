using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
public class TweenInvoker : MonoBehaviour
{
    public enum TweenType
    {
        Scale,
        CanvasAlpha,
    }

    public TweenType Type;
    public Vector3 v_GoalValue;
    public float f_GoalValue;
    public float Duration, Delay;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        if (Type == TweenType.Scale)
            transform.TweenLocalScale(v_GoalValue, Duration).SetDelay(Delay);
        else if (Type == TweenType.CanvasAlpha)
            transform.TweenCanvasGroupAlpha(f_GoalValue, Duration).SetDelay(Delay);
    }
 }

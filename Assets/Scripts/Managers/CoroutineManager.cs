using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : Singleton<CoroutineManager>
{
    public static Coroutine InvokeAction(Action a, float afterTime)
    {
        return instance.StartCoroutine(instance.Coroutine(a, afterTime));
    }
    public static Coroutine InvokeAction(Action a, YieldInstruction yieldInstance)
    {
        return instance.StartCoroutine(instance.Coroutine(a, yieldInstance));
    }

    private IEnumerator Coroutine(Action a, float afterTime)
    {
        yield return new WaitForSeconds(afterTime);
        a();
    }

    private IEnumerator Coroutine(Action a, YieldInstruction ys)
    {
        yield return ys;
        a();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils
{
    public static string Stringify<T>(this IList<T> arr)
    {
        return $"[{string.Join(',', arr)}]";
    }

    public static string Stringify<T>(this T[] arr)
    {
        return $"[{string.Join(',', arr)}]";
    }

    public static string Stringify<T>(this IEnumerable<T> arr)
    {
        return $"[{string.Join(',', arr)}]";
    }

    public static T Random<T>(this List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T Random<T>(this T[] arr)
    {
        return arr[UnityEngine.Random.Range(0, arr.Length)];
    }

    public static float Random(this Vector2 vec)
    {
        return UnityEngine.Random.Range(vec.x, vec.y);
    }

    public static int Random(this Vector2Int vec)
    {
        return UnityEngine.Random.Range(vec.x, vec.y+1);
    }

    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static void ForEachChild(this Transform tr, Action<GameObject> action)
    {
        foreach(Transform t in tr)
        {
            action(t.gameObject);
        }
    }

    public static void RotateToVelocity(this Rigidbody2D rigid)
    {
        Vector2 v = rigid.velocity;
        var angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        rigid.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    internal static Vector3 GetRandomPointBetween(Vector3 lowerLeft, Vector3 upperRight)
    {
        return Vector3.Lerp(lowerLeft, upperRight, UnityEngine.Random.value);
    }
}

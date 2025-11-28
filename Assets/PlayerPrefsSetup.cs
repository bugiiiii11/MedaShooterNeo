using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerPrefKeyValue<T>
{
    public string key;
    public T value;
}

public class PlayerPrefsSetup : MonoBehaviour
{
    [Serializable]
    public class IntegerKeyValue: PlayerPrefKeyValue<int>
    { }

    public List<IntegerKeyValue> Integers;

    private void Start()
    {
        foreach (var kv in Integers)
        {
            PlayerPrefs.SetInt(kv.key, kv.value);
        }
    }
}

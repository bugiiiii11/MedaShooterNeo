using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDotBar : MonoBehaviour
{
    public GameObject DotIconPrefab;

    internal DotIcon InitializeDotIcon(DotBase dot)
    {
        var obj = Instantiate(DotIconPrefab, transform);
        obj.name = dot.Name;
        return obj.GetComponent<DotIcon>();
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    internal void Remove(string key)
    {
        foreach(Transform tr in transform)
        {
            if(tr.name == key)
                Destroy(tr.gameObject);
        }
    }
}

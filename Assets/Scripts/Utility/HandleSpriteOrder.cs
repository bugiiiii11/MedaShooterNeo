using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleSpriteOrder : MonoBehaviour
{
    public List<SpriteRenderer> AllSprites;
    private sbyte[] DefaultOrder;

    public Transform positionTracker;

    void Start()
    {
        DefaultOrder = new sbyte[AllSprites.Count];
        var i = 0;
        foreach(var sr in AllSprites)
        {
            DefaultOrder[i++] = (sbyte)sr.sortingOrder;
        }

        InvokeRepeating(nameof(InvokedUpdate), 0, 0.1f);
    }

    private void InvokedUpdate()
    {
        var i = 0;
        foreach(var sr in AllSprites)
        {
            sr.sortingOrder = DefaultOrder[i++] - Mathf.RoundToInt(positionTracker.position.y*100);
        }
    }
}

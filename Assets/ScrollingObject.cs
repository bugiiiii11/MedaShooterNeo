using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollingObject : MonoBehaviour
{
    private Transform tr;
    private void Start()
    {
        tr = transform;
    }
    void Update()
    {
        var pos = tr.position;
        pos.x -= GameConstants.Constants.GameSpeedMultiplier * Time.deltaTime;
        tr.position = pos;
    }
}

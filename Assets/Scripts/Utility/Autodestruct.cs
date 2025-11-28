using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Autodestruct : MonoBehaviour
{
    public float Time = 0;

    private void Start()
    {
        if(Time > 0)
        {
            Destroy(gameObject, Time);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepLocalScale : MonoBehaviour
{
    void Update()
    {
        transform.localScale = transform.localScale;
    }
}

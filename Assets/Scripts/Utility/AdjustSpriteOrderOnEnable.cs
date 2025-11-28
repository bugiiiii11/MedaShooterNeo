using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustSpriteOrderOnEnable : MonoBehaviour
{
    private void OnEnable()
    {
        var position = Mathf.RoundToInt(transform.position.y * 100);
        var sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder -= position;
    }
}

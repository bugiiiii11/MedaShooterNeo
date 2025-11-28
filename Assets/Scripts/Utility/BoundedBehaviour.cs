using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundedBehaviour : MonoBehaviour
{
    protected Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    protected virtual void Update()
    {
        if (_transform.position.x < LevelInfo.instance.LeftDespawnCoordinateX)
            Destroy(gameObject);
    }
}

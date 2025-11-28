using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILocalizable
{
    Transform _transform { get; }
}

public class DummyLocation : ILocalizable
{
    public Transform _transform { get; private set; }
    public DummyLocation(Transform t)
    {
        _transform = t;
    }
}
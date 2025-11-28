using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovingPowerup : IngamePowerup
{
    protected override void Update() 
    {
        if (GameManager.instance.IsGamePaused)
            return;

        _transform.position += GameManager.instance.GlobalSpeed * Time.deltaTime * Vector3.left;

        base.Update();
    }
}

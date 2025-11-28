using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallenAngelPerk : ActivePerkBehaviour
{
    public override void OnEnd()
    {
    }

    public override void OnInitialize(PlayerMovement player)
    {
        GameManager.instance.GameConstants.IsFallenAngelActive = true;
    }

    public override void OnUpdate()
    { }
}

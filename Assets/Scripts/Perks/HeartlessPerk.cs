using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartlessPerk : ActiveTimedPerkBehaviour
{
    public GameObject HeartlessSpawner;

    public override void OnEnd()
    {
        
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        Instantiate(HeartlessSpawner);    
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINftWeapon : UINftHero
{
    public override void Clicked()
    {
        //UICardPreview.instance.Equip(Nft as NftWeapon);
        UINftPreview.instance.Display(Nft, this);
    }
}

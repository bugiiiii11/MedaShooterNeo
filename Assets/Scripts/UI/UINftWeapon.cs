using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINftWeapon : UINftHero
{
    public override void Clicked()
    {
        Debug.Log($"⚔️ UINftWeapon.Clicked() - Nft: {Nft?.Name ?? "null"}, Type: {Nft?.GetType().Name ?? "null"}");
        //UICardPreview.instance.Equip(Nft as NftWeapon);
        UINftPreview.instance.Display(Nft, this);
    }
}

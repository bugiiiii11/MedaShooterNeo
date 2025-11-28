using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INft
{
    string OwnerWallet { get; set; }
    string Name { get; }
    Sprite Visualization { get; }

    /// <summary>
    /// Boost the nft by multiplier percent
    /// </summary>
    /// <param name="v"></param>
    /// <exception cref="NotImplementedException"></exception>
    void Boost(float multiplier);
}
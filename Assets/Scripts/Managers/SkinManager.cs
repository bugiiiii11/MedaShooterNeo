using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static F3DCharacterAvatar;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Skins;
    public List<CharacterArmature> Characters;

#if UNITY_EDITOR
    public Sprite GenericHands1, GenericHands2;
#endif

    private void Awake()
    {
        Skins = this;
    }

    public CharacterArmature this[SkinName skin] => Characters.Find(x => x.Name == skin);
}

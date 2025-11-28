using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAbility : MonoBehaviour
{
    public AbilityConfig Ability;
    public void Show()
    {
        AbilityInfo.instance.Show(Ability);
    }
}

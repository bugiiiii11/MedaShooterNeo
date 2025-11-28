using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PerkBehaviour : ScriptableObject
{

    public int Id;
    public ObscuredInt StackCount { get; set; }
    public virtual bool CanRemoveStacks => true;

    public string Title { get; internal set; }

    public bool IsPassive = false;

    public abstract void OnInitialize(PlayerMovement player);
    public abstract void OnUpdate();
    public abstract void OnEnd();
    public abstract bool Stack(PerkBehaviour other);
    public abstract bool RemoveStack();
}

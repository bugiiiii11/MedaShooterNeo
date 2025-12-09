using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IUiReferencable
{
    Transform GetTransform();
    void Select(SelectionIconType selected);
}

public enum SelectionIconType
{
    None,
    Selected,
    Equipped
}

public class UINftHero : MonoBehaviour, IUiReferencable
{
    internal INft Nft;
    protected bool owned;

    public virtual void Setup(INft nft, bool owned)
    {
        transform.GetChild(0).GetComponent<Image>().sprite = nft.Visualization;
        Nft = nft;
        this.owned = owned;
    }

    public virtual void Clicked()
    {
        Debug.Log($"🦸 UINftHero.Clicked() - Nft: {Nft?.Name ?? "null"}, Type: {Nft?.GetType().Name ?? "null"}");
        UINftPreview.instance.Display(Nft, this);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void Select(SelectionIconType type)
    {
        switch (type)
        {
            case SelectionIconType.None:
                GetComponent<Image>().sprite = UINftPreview.instance.NormalFrame;
                break;
            case SelectionIconType.Selected:
                GetComponent<Image>().sprite = UINftPreview.instance.SelectedFrame;
                break;
            case SelectionIconType.Equipped:
                GetComponent<Image>().sprite = UINftPreview.instance.EquippedFrame;
                break;
        }
    }
}

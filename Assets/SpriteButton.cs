using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpriteButton : UIBehaviour
{
    private Image image;
    public Sprite ActiveSprite, InactiveSprite;
    public bool StartActive = false;

    protected override void Start()
    {
        base.Start();
        image = GetComponent<Image>();

        GetComponent<Button>().onClick.AddListener(OnClicked);

        if (StartActive)
            OnClicked();
    }

    public void SetInactive()
    {
        image.sprite = InactiveSprite;
    }

    public void OnClicked()
    {
        image.sprite = ActiveSprite;
    }
}

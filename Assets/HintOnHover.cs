using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HintOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public CardPreviewAbilityDisplay Display;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPointerEnter(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Display.SetHintFor(transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Display.SetHintFor(null);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPointerExit(eventData);
    }
}

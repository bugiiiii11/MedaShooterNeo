using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPressSound : MonoBehaviour, IPointerEnterHandler
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnter();
    }

    protected virtual void Clicked()
    {
        OneShotAudioPool.SpawnOneShot(LevelProps.instance.Accept, 0.35f, pitch: 1f, secondsAlive: 0.6f);
    }

    protected virtual void PointerEnter()
    {
        OneShotAudioPool.SpawnOneShot(LevelProps.instance.SelectionClip, 0.45f, pitch:1.5f, secondsAlive: 0.3f);
    }
}

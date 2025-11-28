using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SendBroadcast: UnityEvent<string>
{ }
public class DropdownBroadcaster : MonoBehaviour
{
    public SendBroadcast Events;
    public TMPro.TMP_InputField field;
    private void Start()
    {
        var dropdown = GetComponent<TMPro.TMP_Dropdown>();
        dropdown.onValueChanged.AddListener((idx) =>
        {
            Events.Invoke(dropdown.options[idx].text);
/*
            switch(idx)
            {
                case 0:
                    field.text = "0x20b8fdacc8ed1a93c9c71c39e282b55152f5f664";
                    break;
                case 1:
                    field.text = "0x3c03b473c5c9c0055e6863d6fe148eb3850482de";
                    break;
                case 2:
                    field.text = "0x20b8fdacc8ed1a93c9c71c39e282b55152f5f664";
                    break;
            }*/
        });
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class RoundedButtonClickEvent : UnityEvent<string>
{

}

public class RoundedSelectionButton  : MonoBehaviour
{
    public string ActiveOption = "";
    public List<string> Options;

    public RoundedButtonClickEvent OptionSelected;

    private int currentIndex = 0;
    
    protected void Start()
    {
        if (OptionSelected == null)
            OptionSelected = new();

        if (PlayerPrefs.HasKey("control_method"))
        {
            var method = PlayerPrefs.GetString("control_method");
            ActiveOption = method;
            OnClicked();
        }
        
        currentIndex = Options.IndexOf(ActiveOption);

        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    public void OnClicked()
    {
        currentIndex = (currentIndex + 1) % Options.Count;

        ActiveOption = Options[currentIndex];
        OptionSelected.Invoke(ActiveOption);
        GetComponentInChildren<TextMeshProUGUI>().text = ActiveOption;

        PlayerPrefs.SetString("control_method", ActiveOption);
    }
}

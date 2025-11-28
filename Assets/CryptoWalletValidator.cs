using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class CryptoWalletValidator : MonoBehaviour
{
    public TMPro.TMP_InputField field;
    public Outline validationColor;
    public GameObject ContinueButton;

    private string validationPattern = @"^0x[a-f0-9]{40}$";
    public void OnEditInput(string input)
    {
        input = input.ToLower();
        field.text = input;

        var re = new Regex(validationPattern);

        var match = re.Match(input);

        if(match.Success)
        {
            validationColor.effectColor = Color.green;
            ContinueButton.SetActive(true);
        }
        else
        {
            validationColor.effectColor = Color.red;
            ContinueButton.SetActive(false);
        }
    }
}

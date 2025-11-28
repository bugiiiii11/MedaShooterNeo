using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum FillType: byte
{
    Horizontal,
    Vertical
}

public class UIGenericBar : MonoBehaviour
{
    public delegate void UIBarEventArgs(float value);

    //public Gradient FillGradient;
    public Transform HitPointsImage;
    public FillType FillType;

    protected virtual void Start() 
    {
        SetValue(1);    
    }
    
    public virtual void SetValue([ParamRange(0, 1)] float value)
    {
        var scale = HitPointsImage.localScale;

        if(FillType == FillType.Horizontal)
            scale.x = Mathf.Clamp01(value);
        else if(FillType == FillType.Vertical)
            scale.y = Mathf.Clamp01(value);

        HitPointsImage.localScale = scale;
      //  HitPointsImage.color = FillGradient.Evaluate(value);
    }

    public virtual void SetPercentage(float currentValue, float maxValue)
    {
        if(maxValue == 0)
        {
            SetValue(0);
            return;
        }
        
        var value = currentValue / maxValue;
        SetValue(value);
    }

    internal void Hide()
    {
        if(gameObject.activeSelf)
            gameObject.SetActive(false);
    }
    internal void Show()
    {
        gameObject.SetActive(true);
    }
}

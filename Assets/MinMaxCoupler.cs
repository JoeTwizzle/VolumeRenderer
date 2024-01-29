using MixedReality.Toolkit.UX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MinMaxCoupler : MonoBehaviour
{
    public Slider MinSlider;
    public Slider MaxSlider;

    public UnityEvent<float> OnMinValueChanged;
    public UnityEvent<float> OnMaxValueChanged;

    public UnityEvent<float, float> OnValueChanged;

    // Start is called before the first frame update
    void Start()
    {
        MinSlider.OnValueUpdated.AddListener(MinUpdated);
        MaxSlider.OnValueUpdated.AddListener(MaxUpdated);
    }

    void MinUpdated(SliderEventData eventData)
    {
        MaxSlider.MinValue = eventData.NewValue;
        if (MaxSlider.Value < MaxSlider.MinValue)
        {
            MaxSlider.Value = MaxSlider.MinValue;
        }
        OnMinValueChanged.Invoke(eventData.NewValue);
        OnValueChanged.Invoke(eventData.NewValue, MaxSlider.Value);
    }

    void MaxUpdated(SliderEventData eventData)
    {
        MinSlider.MaxValue = eventData.NewValue;
        if (MinSlider.Value > MinSlider.MaxValue)
        {
            MinSlider.Value = MaxSlider.Value;
        }
        OnMaxValueChanged.Invoke(eventData.NewValue);
        OnValueChanged.Invoke(MinSlider.Value, eventData.NewValue);
    }
}

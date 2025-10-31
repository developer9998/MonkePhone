using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace MonkePhone.Behaviours.UI;

public class PhoneTopBar : MonoBehaviour
{
    public Text   _timeText;
    public Slider _batteryLevel;
    public Image  _batteryGraphic;

    private DateTime Now     => DateTime.Now;
    private string   Current => Now.ToString("hh:mm tt");

    private float Battery
    {
        get
        {
            ControllerInputPoller.instance.headDevice.TryGetFeatureValue(CommonUsages.batteryLevel, out float value);

            return value;
        }
    }

    public void Awake()
    {
        _timeText       = transform.Find("Time Text").GetComponent<Text>();
        _batteryLevel   = transform.Find("Slider").GetComponent<Slider>();
        _batteryGraphic = transform.Find("Slider/Fill Area/Fill").GetComponent<Image>();
    }

    public void Update()
    {
        _timeText.text        = Current;
        _batteryLevel.value   = Battery;
        _batteryGraphic.color = Color.Lerp(Color.red, Color.green, Battery);
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BepInEx.Configuration;
using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.Apps
{
    public class ConfigurationApp : PhoneApp
    {
        public override string AppId => "Configuration";

        private IEnumerable<ConfigEntryBase> _configurationEntries;
        private int _currentEntry;

        private Text _section, _key, _description, _default, _navigationLabel;
        private PhoneSwitch _switch;
        private GameObject _slider, _selector;

        public override void Initialize()
        {
            _section = transform.Find("Section Label").GetComponent<Text>();
            _key = transform.Find("Key Label").GetComponent<Text>();
            _description = transform.Find("Description Label").GetComponent<Text>();
            _default = transform.Find("Default Label").GetComponent<Text>();
            _navigationLabel = transform.Find("Category Nav Label").GetComponent<Text>();

            _slider = transform.Find("Values/Slider").gameObject;
            _slider.gameObject.SetActive(false);

            _switch = (PhoneSwitch)GetObject("Switch", true);
            _switch.gameObject.SetActive(false);

            _selector = transform.Find("Values/Selector").gameObject;
            _selector.SetActive(false);
        }

        public override void AppOpened()
        {
            base.AppOpened();

            if (_configurationEntries == null)
            {
                var configFile = Configuration.File;
                _configurationEntries = configFile.Keys.Where(definition => definition.Key != "Wallpaper" && definition.Key != "Webhook Url").Select(definition => configFile[definition]);
            }

            RefreshApp();
        }

        private void RefreshApp()
        {
            var entry = _configurationEntries.ElementAt(_currentEntry);
            _navigationLabel.text = $"{_currentEntry + 1}/{_configurationEntries.Count()}";
            _section.text = entry.Definition.Section;
            _key.text = entry.Definition.Key;
            _description.text = entry.Description.Description;
            _default.text = entry.DefaultValue.ToString();

            bool isEnum = entry.SettingType.IsEnum;
            _slider.gameObject.SetActive(isEnum);

            bool isBool = entry.SettingType == typeof(bool);
            _switch.gameObject.SetActive(isBool);

            bool isNumber = entry.SettingType == typeof(int) || entry.SettingType == typeof(float);
            _selector.SetActive(isNumber);

            if (isEnum)
            {
                string currentEnum = entry.GetSerializedValue();
                DescriptionAttribute description = (DescriptionAttribute)entry.SettingType.GetMember(entry.GetSerializedValue())?.First()?.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault();
                string[] enumNames = Enum.GetNames(entry.SettingType);

                PhoneSlider slider = _slider.GetComponentInChildren<PhoneSlider>(false);
                slider.Parameters = new Vector3(0, enumNames.Length - 1, Array.IndexOf(enumNames, currentEnum));
                slider.Split = enumNames.Length;
                slider.UpdatePosition();

                _slider.transform.Find("Label").GetComponent<Text>().text = currentEnum;
                _slider.transform.Find("Description").GetComponent<Text>().text = description != null ? description.Description : string.Empty;
                slider.transform.localPosition = description != null ? new Vector3(12.9f, -31.9f, -0.2000008f) : new Vector3(12.9f, -26.5f, -0.2000008f);
                return;
            }

            if (isBool)
            {
                var current = (bool)entry.BoxedValue;
                _switch.IsActive = current;
                _switch.UpdateSwitch();
                return;
            }

            if (isNumber)
            {
                _selector.transform.Find("Default Label (1)").GetComponent<Text>().text = entry.GetSerializedValue();
            }
        }

        public override void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand)
        {
            base.ButtonClick(phoneUIObject, isLeftHand);

            switch (phoneUIObject.name)
            {
                case "Category Nav Left":
                    _currentEntry = MathEx.Wrap(_currentEntry - 1, 0, _configurationEntries.Count());

                    RefreshApp();
                    break;
                case "Category Nav Right":
                    _currentEntry = MathEx.Wrap(_currentEntry + 1, 0, _configurationEntries.Count());

                    RefreshApp();
                    break;
                case "Number Increase":
                case "Number Decrease":

                    bool decrease = phoneUIObject.name.EndsWith("Decrease");
                    ConfigEntryBase entry = _configurationEntries.ElementAt(_currentEntry);
                    double current = entry.SettingType == typeof(int)
                        ? (int)entry.BoxedValue + (decrease ? -1 : 1)
                        : Math.Round((float)entry.BoxedValue + (decrease ? -0.1f : 0.1f), 2);

                    entry.SetSerializedValue(current.ToString());

                    _selector.transform.Find("Default Label (1)").GetComponent<Text>().text = entry.GetSerializedValue();
                    break;
            }
        }

        public override void ButtonTicked(PhoneUIObject phoneUIObject, bool currentValue, bool isLeftHand)
        {
            base.ButtonTicked(phoneUIObject, currentValue, isLeftHand);

            ConfigEntryBase entry = _configurationEntries.ElementAt(_currentEntry);
            entry.SetSerializedValue(currentValue.ToString());
        }

        public override void SliderUpdated(PhoneUIObject phoneUIObject, float currentValue, bool isSelected)
        {
            base.SliderUpdated(phoneUIObject, currentValue, isSelected);

            ConfigEntryBase entry = _configurationEntries.ElementAt(_currentEntry);

            if (!entry.SettingType.IsEnum)
            {
                return;
            }

            string currentEnum = Enum.GetNames(entry.SettingType)[(int)currentValue];
            DescriptionAttribute description = (DescriptionAttribute)entry.SettingType.GetMember(currentEnum)?.First()?.GetCustomAttributes(typeof(DescriptionAttribute), false)?.FirstOrDefault();

            entry.SetSerializedValue(currentEnum);

            _slider.transform.Find("Label").GetComponent<Text>().text = currentEnum;
            _slider.transform.Find("Description").GetComponent<Text>().text = description != null ? description.Description : string.Empty;
            phoneUIObject.transform.localPosition = description != null ? new Vector3(12.9f, -31.9f, -0.2000008f) : new Vector3(12.9f, -26.5f, -0.2000008f);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

#if PLUGIN
using MonkePhone.Interfaces;
#endif

namespace MonkePhone.Behaviours.UI
{
    public class PhoneSwitch : PhoneUIObject
    {
        [Range(0.05f, 1f)]
        public float Debounce = 0.2f;

        [Space]

        public bool IsActive;

        public Color OffColour = new Color32(239, 120, 122, 255), OnColour = new Color32(114, 114, 200, 255);

        [Space]

        public Text Text;

        public string Format = "{0}";

        public string OffText = "OFF", OnText = "ON";

        [Space]

        public RectTransform Switch;

#if PLUGIN
        private Image _switchImage;

        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;

            _switchImage = Switch.GetComponent<Image>();

            UpdateSwitch();
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) || _lastActivation + Debounce > Time.realtimeSinceStartup || (!Phone.Held && !Phone.Leviating) || (!Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand))
            {
                return;
            }

            _lastActivation = Time.realtimeSinceStartup;

            Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
            PlaySound(IsActive ? "SwitchOn" : "SwitchOff", 0.1f);

            Activated(handIndicator.isLeftHand);
        }

        public void UpdateSwitch()
        {
            if (!Switch || !Text || !_switchImage)
            {
                return;
            }

            if (IsActive)
            {
                Switch.localPosition = Vector3.right * (Switch.sizeDelta.x / 2f);
                _switchImage.color = OnColour;

                Text.text = string.Format(Format, OnText);
                return;
            }

            Switch.localPosition = Vector3.left * (Switch.sizeDelta.x / 2f);
            _switchImage.color = OffColour;

            Text.text = string.Format(Format, OffText);
        }

        public void Activated(bool leftHand)
        {
            IsActive ^= true;
            UpdateSwitch();

            if (TryGetPhoneApp(out IPhoneApp app))
            {
                app.ButtonTicked(this, IsActive, leftHand);
            }
        }
#endif
    }
}

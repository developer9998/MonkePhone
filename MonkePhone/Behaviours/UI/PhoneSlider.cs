using UnityEngine;
using UnityEngine.UI;
#if PLUGIN
using System;
using MonkePhone.Interfaces;
#endif

namespace MonkePhone.Behaviours.UI
{
    public class PhoneSlider : PhoneUIObject
    {
        [Tooltip("x=min, y=max, z=current")] public Vector3 Parameters = Vector3.zero;

        [Tooltip("How much pieces the slider is split up into")]
        public int Split = 10;

        public Text Text;

        public string Format = "{0}";

        public Transform Slider, ExtentLeft, ExtentRight;

        public float Value
        {
            get => Parameters.z;
            set => Parameters.z = Mathf.InverseLerp(Parameters.x, Parameters.y, value);
        }

#if PLUGIN
        private GorillaTriggerColliderHandIndicator _handIndicator;

        private const float _debounce = 0.13f;

        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;

            UpdatePosition();
        }

        public void OnDisable()
        {
            if (!_handIndicator)
                return;

            OnTriggerExit(_handIndicator.GetComponent<Collider>());
        }

        public void UpdatePosition()
        {
            Slider.transform.localPosition =
                    Vector3.Lerp(ExtentLeft.localPosition, ExtentRight.localPosition,
                            Mathf.InverseLerp(Parameters.x, Parameters.y, Parameters.z));

            UpdateText();
        }

        private void UpdateText()
        {
            if (!Text)
                return;

            Text.text = string.Format(Format, Math.Round(Value, 2));
        }

        public void OnTriggerStay(Collider other)
        {
            if (!other.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) ||
                _lastActivation + _debounce > Time.realtimeSinceStartup || !Phone.Held && !Phone.Leviating ||
                !Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand ||
                _handIndicator && _handIndicator != handIndicator)
                return;

            Vector3 local = transform.InverseTransformPoint(handIndicator.transform.position);
            float updatedParameter =
                    Mathf.Lerp(Parameters.x, Parameters.y,
                            Mathf.RoundToInt(
                                    Mathf.Clamp01((local.x - ExtentLeft.localPosition.x) /
                                                  (ExtentRight.localPosition.x * 2f)) * (Split - 1)) /
                            (float)(Split - 1));

            bool difference = updatedParameter != Value;

            if (difference && _handIndicator)
            {
                PlaySound(updatedParameter > Value ? "SwitchOn" : "SwitchOff", 0.1f);
                Vibration(handIndicator.isLeftHand, 0.1f, 0.015f);
            }

            if (difference)
            {
                Parameters.z = updatedParameter;
                Adjusted(true);
                UpdatePosition();
            }

            if (!_handIndicator)
            {
                _handIndicator = handIndicator;
                Vibration(handIndicator.isLeftHand, 0.25f, 0.05f);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) ||
                handIndicator != _handIndicator) return;

            _lastActivation = Time.realtimeSinceStartup;
            _handIndicator  = null;
            Adjusted(false);

            PhoneManager.Instance.PlaySound("BasicTap");
        }

        public void Adjusted(bool isSelected)
        {
            if (TryGetPhoneApp(out IPhoneApp app))
                app.SliderUpdated(this, Value, isSelected);
        }
#endif
    }
}
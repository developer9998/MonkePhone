using System;
using UnityEngine;
using UnityEngine.UI;

#if PLUGIN
using MonkePhone.Interfaces;
#endif

namespace MonkePhone.Behaviours.UI
{
    public class PhoneCheckbox : PhoneUIObject
    {
        [Range(0.05f, 1f)]
        public float Debounce = 0.18f;

        public bool IsActive;

        public Image Check;

#if PLUGIN
        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;

            UpdateCheck();
        }

        public void OnEnable()
        {
            UpdateCheck();
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) || _lastActivation + Debounce > Time.realtimeSinceStartup || (!Phone.Held && !Phone.Leviating) || (!Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand))
            {
                return;
            }

            _lastActivation = Time.realtimeSinceStartup;

            Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
            PlaySound("BasicTap", 0.36f);
            PlaySound($"Scribble{IsActive.ToString().ToLower()}", 0.23f);

            Activated(handIndicator.isLeftHand);
        }

        public void UpdateCheck()
        {
            if (!Check) return;
            Check.enabled = IsActive;
        }

        public void Activated(bool leftHand)
        {
            IsActive ^= true;
            UpdateCheck();

            if (TryGetPhoneApp(out IPhoneApp app))
            {
                app.ButtonTicked(this, IsActive, leftHand);
            }
        }
#endif
    }
}

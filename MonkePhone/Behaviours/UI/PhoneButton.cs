using UnityEngine;
using System;


#if PLUGIN
using MonkePhone.Interfaces;
#endif

namespace MonkePhone.Behaviours.UI
{
    public class PhoneButton : PhoneUIObject
    {
        [Range(0.05f, 1f)]
        public float Debounce = 0.25f;

#if PLUGIN
        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) || _lastActivation + Debounce > Time.realtimeSinceStartup || (!Phone.Held && !Phone.Leviating) || (!Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand))
            {
                return;
            }

            _lastActivation = Time.realtimeSinceStartup;

            Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
            PlaySound(name == "HomeButton" ? "MenuTap" : "BasicTap", 0.36f);

            Activated(handIndicator.isLeftHand);
        }

        public void Activated(bool leftHand)
        {
            if (TryGetPhoneApp(out IPhoneApp app))
            {
                app.ButtonClick(this, leftHand);
                return;
            }

            switch (name)
            {
                case "HomeButton":
                    InvokeMethod(nameof(PhoneManager.SetHome));
                    break;

                case "PowerButton":
                    InvokeMethod(nameof(PhoneManager.TogglePower));
                    break;

                case "HidePromo":
                    PhoneManager.Instance.watchPromoObject.SetActive(false);
                    DateTime date = DateTime.UtcNow;
                    string key = $"IgnorePromo{date.Year}{date.Month}";
                    PlayerPrefs.SetInt(key, 1);
                    break;
            }
        }
#endif
    }
}
using UnityEngine;

namespace MonkePhone.Behaviours.UI
{
    public class PhoneAppIcon : PhoneUIObject
    {
        [Tooltip("The AppId of the app that will be opened when pressing the icon")]
        public string appId;

#if PLUGIN
        private const float _debounce = 0.13f;

        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) ||
                _lastActivation + _debounce > Time.realtimeSinceStartup || !Phone.Held && !Phone.Leviating ||
                !Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand)
                return;

            _lastActivation = Time.realtimeSinceStartup;

            Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f,
                    GorillaTagger.Instance.tapHapticDuration);

            PlaySound("BasicTap", 0.36f);

            InvokeMethod("OpenApp", appId);
        }

#endif
    }
}
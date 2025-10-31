using UnityEngine;

namespace MonkePhone.Behaviours.UI
{
    public class PhoneKey : PhoneUIObject
    {
        public string Key;

        [Range(0.05f, 0.5f)] public float Debounce = 0.08f;

#if PLUGIN
        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) ||
                _lastActivation + Debounce > Time.realtimeSinceStartup || !Phone.Held && !Phone.Leviating ||
                !Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand)
                return;

            _lastActivation = Time.realtimeSinceStartup;

            Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f,
                    GorillaTagger.Instance.tapHapticDuration);

            PlaySound("Key", 0.3f);
        }
#endif
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.UI
{
    public class PhoneTextBox : PhoneUIObject
    {
        public string CurrentText, DefaultText;
        public int TextCapacity = 76;
        public Color Colour = Color.white;

        [Space]
        public string PlaceholderText = "Click box to open keyboard";
        public Color PlaceholderColour = new Color32(200, 200, 200, 255);

        [Space]
        public Text Text;

#if PLUGIN
        public void Start()
        {
            gameObject.SetLayer(UnityLayer.GorillaInteractable);
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (!collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) || _lastActivation + 0.28f > Time.realtimeSinceStartup || (!Phone.Held && !Phone.Leviating) || (!Phone.Leviating && handIndicator.isLeftHand == Phone.LeftHand))
            {
                return;
            }

            _lastActivation = Time.realtimeSinceStartup;

            if (!PhoneManager.Instance.Keyboard.Active)
            {
                PlaySound("MenuTap", 0.36f);
                Vibration(handIndicator.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                PhoneManager.Instance.Keyboard.Active = true;
            }
        }
#endif
    }
}

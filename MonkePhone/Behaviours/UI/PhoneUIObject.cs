#if PLUGIN
using MonkePhone.Interfaces;
using MonkePhone.Tools;
using UnityEngine;
#endif

namespace MonkePhone.Behaviours.UI
{
    public class PhoneUIObject : PhoneBehaviour
    {
        internal static float _lastActivation = 1;

#if PLUGIN
        private IPhoneApp _phoneApp;

        public IPhoneApp GetPhoneApp()
        {
            if (_phoneApp != default)
            {
                return _phoneApp;
            }

            Transform parent = transform.parent;

            while (parent)
            {
                if (parent.TryGetComponent(out IPhoneApp app))
                {
                    _phoneApp = app;
                    break;
                }

                if (parent.GetComponent<Phone>())
                {
                    Logging.Warning($"PhoneApp not associated with object {name}");
                    return null;
                }

                parent = parent.parent;

                if (!parent)
                {
                    Logging.Warning($"PhoneApp not associated with object {name}");
                    return null;
                }
            }

            return _phoneApp;
        }

        public bool TryGetPhoneApp(out IPhoneApp app)
        {
            app = GetPhoneApp();
            return app != default; // https://docs.unity3d.com/ScriptReference/Object-operator_Object.html
        }
#endif
    }
}

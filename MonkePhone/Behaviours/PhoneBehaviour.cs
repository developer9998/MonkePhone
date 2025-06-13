using UnityEngine;

#if PLUGIN
using HarmonyLib;
using System.Reflection;
using MonkePhone.Tools;
#endif

namespace MonkePhone.Behaviours
{
    public class PhoneBehaviour : MonoBehaviour
    {
#if PLUGIN

        public void Vibration(bool isLeftHand, float amplitude, float duration)
        {
            if (!Configuration.AppHaptics.Value)
            {
                return;
            }

            GorillaTagger.Instance.StartVibration(isLeftHand, amplitude, duration);
        }

        public void PlaySound(string soundId, float volume = 1f) => InvokeMethod("PlaySound", soundId, volume);

        public void InvokeMethod(string methodName, params object[] parameters)
        {
            MethodInfo method = AccessTools.Method(typeof(PhoneHandler), methodName);
            method?.Invoke(PhoneHandler.Instance, parameters);
        }
#endif
    }
}

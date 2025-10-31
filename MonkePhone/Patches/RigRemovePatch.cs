using HarmonyLib;
using MonkePhone.Networking;
using UnityEngine;

namespace MonkePhone.Patches;

[HarmonyPatch(typeof(RigContainer), nameof(RigContainer.OnDisable))]
public class RigRemovePatch
{
    [HarmonyWrapSafe]
    public static void Postfix(RigContainer __instance)
    {
        if (__instance.TryGetComponent(out NetworkedPlayer networkedPlayer))
            Object.Destroy(networkedPlayer);
    }
}
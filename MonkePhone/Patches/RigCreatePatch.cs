using HarmonyLib;
using MonkePhone.Networking;

namespace MonkePhone.Patches;

[HarmonyPatch(typeof(RigContainer), "set_Creator")]
public class RigSetCreatorPatch
{
    [HarmonyWrapSafe]
    public static void Postfix(RigContainer __instance, NetPlayer value)
    {
        if (!__instance.GetComponent<NetworkedPlayer>())
        {
            NetworkedPlayer networkedPlayer = __instance.gameObject.AddComponent<NetworkedPlayer>();
            networkedPlayer.Rig   = __instance.Rig;
            networkedPlayer.Owner = value;
        }
    }
}
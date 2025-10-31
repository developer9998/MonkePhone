using HarmonyLib;

namespace MonkePhone.Patches;

[HarmonyPatch(typeof(ZoneManagement), "SetZones")]
[HarmonyWrapSafe]
public static class ZoneActivationPatch
{
    public static GTZone[] ActiveZones;

    public static void Prefix(GTZone[] newActiveZones)
    {
        ActiveZones = newActiveZones;
    }
}
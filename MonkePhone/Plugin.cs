using System;
using BepInEx;
using HarmonyLib;
using MonkePhone.Behaviours;
using MonkePhone.Networking;
using MonkePhone.Tools;
using UnityEngine;

namespace MonkePhone;

[BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        Logging.Logger = Logger;
        Configuration.Construct(Config);

        GorillaTagger.OnPlayerSpawned(Initialize);

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Constants.GUID);
    }

    public void Initialize()
    {
        try
        {
            new GameObject("MonkePhone", typeof(NetworkHandler), typeof(PhoneManager));
        }
        catch (Exception ex)
        {
            Logging.Error($"Error when initializing MonkePhone: {ex}");
        }
    }
}
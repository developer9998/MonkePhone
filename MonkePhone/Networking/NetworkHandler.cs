using ExitGames.Client.Photon;
using MonkePhone.Tools;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MonkePhone.Networking;

public class NetworkHandler : MonoBehaviourPunCallbacks
{
    public static NetworkHandler Instance { get; set; }

    public Action<NetPlayer, Hashtable> OnPlayerPropertiesChanged;

    private readonly byte eventCode = 176;

    private readonly int id = StaticHash.Compute(Constants.CustomProperty.GetStaticHash());

    private Hashtable _properties = [];
    private bool _isPropertiesReady;
    private float _propertySetTimer;

    private Player[] playerArray;

    public void Awake()
    {
        Instance = this;

        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new() { { Constants.CustomProperty, Constants.Version } });
    }

    public void Update()
    {
        _propertySetTimer = Mathf.Max(_propertySetTimer - Time.unscaledDeltaTime, 0f);

        if (_isPropertiesReady && _propertySetTimer <= 0)
        {
            _isPropertiesReady = false;
            _propertySetTimer = Constants.NetworkCooldown;

            try
            {
                SendProperties(_properties, [.. from player in playerArray where IsCompatiblePlayer(player) select player]);
            }
            catch (Exception ex)
            {
                Logging.Fatal("NetworkSolution failed to send player properties");
                Logging.Error(ex);
            }
        }
    }

    public void SetProperty(string key, object value)
    {
        if (_properties.ContainsKey(key)) _properties[key] = value;
        else _properties.Add(key, value);

        _isPropertiesReady = PhotonNetwork.InRoom || _isPropertiesReady;
    }

    public void RemoveProperty(string key)
    {
        if (_properties.ContainsKey(key)) _properties.Remove(key);

        _isPropertiesReady = PhotonNetwork.InRoom || _isPropertiesReady;
    }

    public void SetProperties(Hashtable properties)
    {
        _properties = properties;

        _isPropertiesReady = PhotonNetwork.InRoom || _isPropertiesReady;
    }

    public void NotifyPropertiesRecieved(Player player, Hashtable properties)
    {
        Logging.Info($"{player}: {string.Join(", ", properties)}");
        OnPlayerPropertiesChanged?.Invoke(player, properties);
    }

    public void SendProperties(Hashtable properties, Player[] targetPlayers)
    {
        object[] content = [id, properties];

        RaiseEventOptions raiseEventOptions = new()
        {
            TargetActors = [.. from player in targetPlayers select player.ActorNumber]
        };

        PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, SendOptions.SendReliable);
    }

    public bool IsCompatiblePlayer(Player player)
    {
        return true;
    }

    public sealed override async void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        playerArray = PhotonNetwork.PlayerListOthers;

        await Task.Delay(PhotonNetwork.GetPing());
        _isPropertiesReady = true;
    }

    public sealed override void OnLeftRoom()
    {
        base.OnLeftRoom();
        playerArray = null;
    }

    public sealed override async void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        playerArray = PhotonNetwork.PlayerListOthers;

        while (VRRigCache.rigsInUse.All(player => player.Key.ActorNumber != newPlayer.ActorNumber)) await Task.Delay(PhotonNetwork.GetPing());

        try
        {
            SendProperties(_properties, [newPlayer]);
        }
        catch (Exception ex)
        {
            Logging.Fatal("NetworkSolution failed to send player properties");
            Logging.Error(ex);
        }
    }

    public sealed override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        playerArray = PhotonNetwork.PlayerListOthers;
    }

    private void OnEvent(EventData data)
    {
        if (data.Code != eventCode) return;

        object[] eventData = (object[])data.CustomData;

        if (eventData.Length < 2 || eventData[0] is not int) return;

        int eventId = (int)eventData[0];
        if (eventId != id) return;

        Player player = PhotonNetwork.CurrentRoom.GetPlayer(data.Sender);
        NetPlayer netPlayer = NetworkSystem.Instance.GetPlayer(data.Sender);
        if (player.IsLocal || !VRRigCache.Instance.TryGetVrrig(netPlayer, out RigContainer playerRig) || !playerRig.TryGetComponent(out NetworkedPlayer networkedPlayer)) return;

        if (eventData[1] is Hashtable properties)
        {
            networkedPlayer.OnPlayerPropertyChanged(properties);
            NotifyPropertiesRecieved(player, properties);

            return;
        }
    }
}
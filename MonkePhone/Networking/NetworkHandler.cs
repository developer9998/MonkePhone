using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using MonkePhone.Tools;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MonkePhone.Networking
{
    public class NetworkHandler : MonoBehaviourPunCallbacks
    {
        public static volatile NetworkHandler Instance;

        public Action<NetPlayer, Dictionary<string, object>> OnPlayerPropertyChanged;

        private readonly Dictionary<string, object> properties = [];
        private bool set_properties = false;
        private float properties_timer;

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            if (NetworkSystem.Instance && NetworkSystem.Instance is NetworkSystemPUN)
            {
                SetProperty("Version", Constants.Version);

                PhotonNetwork.AddCallbackTarget(this);
                Application.quitting += () => PhotonNetwork.RemoveCallbackTarget(this);
                return;
            }

            enabled = false; // either no netsys or not in a pun environment - i doubt fusion will ever come
        }

        public void FixedUpdate()
        {
            properties_timer -= Time.fixedDeltaTime;

            if (set_properties && properties.Count > 0 && properties_timer <= 0)
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(new()
                {
                    {
                        Constants.CustomProperty,
                        new Dictionary<string, object>(properties)
                    }
                });

                set_properties = false;
                properties_timer = 0.225f;
            }
        }

        public void SetProperty(string key, object value)
        {
            if (properties.ContainsKey(key))
            {
                properties[key] = value;
                Logging.Info($"Updated network key - {key}: {value}");
            }
            else
            {
                properties.Add(key, value);
                Logging.Info($"Added network key - {key}: {value}");
            }
            set_properties = true;
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            NetPlayer netPlayer = NetworkSystem.Instance.GetPlayer(targetPlayer.ActorNumber);

            if (netPlayer.IsLocal || !VRRigCache.Instance.TryGetVrrig(netPlayer, out RigContainer playerRig) || !playerRig.TryGetComponent(out NetworkedPlayer networkedPlayer))
                return;

            if (changedProps.TryGetValue(Constants.CustomProperty, out object props_object) && props_object is Dictionary<string, object> properties)
            {
                networkedPlayer.HasMonkePhone = true;

                Logging.Info($"Recieved properties from {netPlayer.NickName}: {string.Join(", ", properties.Select(prop => $"[{prop.Key}: {prop.Value}]"))}");
                OnPlayerPropertyChanged?.Invoke(netPlayer, properties);
            }
        }

        /*
        public async Task RegisterPlayer(NetPlayer player, VRRig rig)
        {
            Logging.Info($"RegisterPlayer {player.NickName}");

            if (player.IsLocal) return;

            var prtime_player = player.GetPlayerRef() ?? PhotonNetwork.CurrentRoom?.GetPlayer(player.ActorNumber);

            if (!rig.GetComponent<NetPhone>())
            {
                NetPhone phone = rig.gameObject.GetOrAddComponent<NetPhone>();
                phone.Player = prtime_player;
                await Task.Delay(200);
                PropertiesUpdate(player, prtime_player?.CustomProperties ?? []);
            }
        }

        public void UnregisterPlayer(VRRig rig)
        {
            if (rig.TryGetComponent(out NetPhone phone) && !phone.Player.IsLocal)
            {
                Logging.Info($"UnregisterPlayer {phone.Player}");

                Destroy(phone.Phone);
                Destroy(phone);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

            if (targetPlayer.IsLocal) return;

            NetPlayer player = NetworkSystem.Instance.GetPlayer(targetPlayer.ActorNumber);
            PropertiesUpdate(player, changedProps);
        }

        private void PropertiesUpdate(NetPlayer player, Hashtable changedProps)
        {
            try
            {
                if (player == null) return;

                if (changedProps.TryGetValue(Constants.CustomProperty, out object value) && value is string str)
                {
                    PhoneNetworkContent content = str.FromJson<PhoneNetworkContent>();
                    Logging.Info($"{player.NickName}: hand {content.IsHeld} lev {content.Levitate}");
                    var rig = GorillaGameManager.StaticFindRigForPlayer(player);
                    if (rig && rig.TryGetComponent(out NetPhone phone))
                    {
                        phone.UpdateNetworkContent(content);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when handling updated properties for {player.NickName}: {ex}");
            }
        }
        */
    }
}
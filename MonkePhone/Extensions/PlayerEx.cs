using System.Linq;
using ExitGames.Client.Photon;
using GorillaNetworking;
using MonkePhone.Behaviours;
using Photon.Pun;
using Photon.Realtime;

namespace MonkePhone.Extensions
{
    public static class PlayerEx
    {
        public static string GetName(this NetPlayer player, VRRig vrRig = null, bool includePhone = true)
        {
            if (player.GetPlayerRef() != null)
            {
                return GetName(player.GetPlayerRef(), vrRig, includePhone);
            }
            int actor = player.ActorNumber;
            if (player.InRoom && PhotonNetwork.InRoom)
            {
                var punPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actor);
                if (punPlayer != null)
                {
                    return GetName(punPlayer, vrRig, includePhone);
                }
            }
            return "n/a";
        }

        public static string GetName(this Player player, VRRig vrRig = null, bool includePhone = true)
        {
            string safetyCheckName = PlayFabAuthenticator.instance.GetSafety() ? player.DefaultName : player.NickName;
            safetyCheckName = safetyCheckName.ToUpper();

            if (safetyCheckName.Length > 12)
            {
                safetyCheckName.Substring(0, 12);
            }

            if (!vrRig) return safetyCheckName;

            Hashtable customProperties = player.CustomProperties;
            CosmeticsController.CosmeticSet cosmeticSet = vrRig.cosmeticSet;
            string wornCosmetics = (cosmeticSet != null && cosmeticSet.items != null) ? cosmeticSet.items
                .Where(item => !item.isNullItem && item.itemName != CosmeticsController.instance.nullItem.itemName)
                .Select(item => item.itemName)
                .Where(vrRig.IsItemAllowed)
                .Concat() : "";

            string summaryAppendage = "";

            if (includePhone && customProperties.ContainsKey(Constants.CustomProperty))
            {
                summaryAppendage += PhoneHandler.Instance.Data.phoneEmoji;
            }

            if (wornCosmetics != "")
            {
                var usedCosmeticEmoji = PhoneHandler.Instance.Data.cosmeticEmoji.Where(emoji => wornCosmetics.Contains(emoji.cosmeticId));
                foreach (var emoji in usedCosmeticEmoji)
                {
                    summaryAppendage += emoji.emoji;
                }
            }

            return summaryAppendage == "" ? safetyCheckName : $"{safetyCheckName}{summaryAppendage}";
        }
    }
}

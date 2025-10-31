using System;
using System.IO;
using System.Threading.Tasks;
using MonkePhone.Behaviours;
using MonkePhone.Utilities;
using UnityEngine.Networking;

namespace MonkePhone.Models;

[Serializable]
public class PhoneOnlineData
{
    public string          version;
    public string          invite;
    public string          phoneEmoji;
    public CosmeticEmoji[] cosmeticEmoji;
    public Song[]          songs;
}

[Serializable]
public class CosmeticEmoji
{
    public string cosmeticId;
    public string emoji;
}

[Serializable]
public class Song
{
    public enum DownloadState
    {
        None,
        Awaiting,
        Downloaded,
        Failed,
    }

    public string title;
    public string coverUrl;
    public string fileName;
    public string fileDownloadUrl;

    [NonSerialized] public DownloadState currentState;

    public string FilePath     => Path.Combine(PhoneManager.Instance.MusicPath, fileName);
    public bool   IsDownloaded => File.Exists(FilePath);

    public async Task<DownloadState> Download()
    {
        if (currentState != DownloadState.None) return DownloadState.None;

        currentState = DownloadState.Awaiting;

        UnityWebRequest request = UnityWebRequest.Get(fileDownloadUrl);
        await YieldUtils.Yield(request);

        if (request.result == UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(FilePath, request.downloadHandler.data);
            currentState = DownloadState.Downloaded;

            return DownloadState.Downloaded;
        }

        currentState = DownloadState.None;

        return DownloadState.Failed;
    }
}
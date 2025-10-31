using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonkePhone.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonkePhone.Tools;

public static class AssetLoader
{
    private static bool        _bundleLoaded;
    private static AssetBundle _storedBundle;

    private static          Task                       _loadingTask;
    private static readonly Dictionary<string, Object> _assetCache = [];

    private static async Task LoadBundle()
    {
        Stream                   stream = typeof(Plugin).Assembly.GetManifestResourceStream(Constants.BundleName);
        AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromStreamAsync(stream);

        // AssetBundleCreateRequest is a YieldInstruction !!
        await YieldUtils.Yield(bundleLoadRequest);

        _storedBundle = bundleLoadRequest.assetBundle;
        _bundleLoaded = true;
    }

    public static async Task<T> LoadAsset<T>(string name) where T : Object
    {
        if (_assetCache.TryGetValue(name, out Object _loadedObject)) return _loadedObject as T;

        if (!_bundleLoaded)
        {
            _loadingTask ??= LoadBundle();
            await _loadingTask;
        }

        AssetBundleRequest assetLoadRequest = _storedBundle.LoadAssetAsync<T>(name);

        // AssetBundleRequest is a YieldInstruction !!
        await YieldUtils.Yield(assetLoadRequest);

        T asset = assetLoadRequest.asset as T;
        _assetCache.Add(name, asset);
        Logging.Log($"Loaded asset {name} of type {typeof(T).Name}");

        return asset;
    }
}
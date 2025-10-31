using System.Linq;
using System.Threading.Tasks;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonkePhone.Extensions;

// https://discussions.unity.com/t/how-to-check-if-a-point-is-seeable-by-a-camera/5557/6
public static class CameraEx
{
    public static bool PointInCameraView(this Camera camera,          Vector3 point, bool useLayers = false,
                                         UnityLayer  targetLayer = 0, int     layerMask = -5)
    {
        Vector3 viewport        = camera.WorldToViewportPoint(point);
        bool    inCameraFrustum = viewport.x.Is01() && viewport.y.Is01();
        bool    inFrontOfCamera = viewport.z > 0;

        bool objectBlockingPoint = false;

        float distance = Vector3.Distance(camera.transform.position, point);

        if (useLayers)
        {
            Vector3 directionBetween = point - camera.transform.position;
            directionBetween = directionBetween.normalized;

            if (Physics.Raycast(camera.transform.position, directionBetween, out RaycastHit depthCheck,
                        distance + 0.05f, layerMask, QueryTriggerInteraction.Collide) &&
                depthCheck.transform.gameObject.layer != (int)targetLayer)
                objectBlockingPoint = true;
        }

        return inCameraFrustum && inFrontOfCamera && !objectBlockingPoint && distance < 30f;
    }

    public static async Task<Texture2D> GetTexture(this Camera camera)
    {
        await YieldUtils.Yield(new WaitForEndOfFrame());

        Texture2D texture;

        RenderTexture targetTexture = camera.targetTexture;

        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Logging.Warning("AsyncGPUReadback is not supported");

            await YieldUtils.Yield(new WaitForEndOfFrame());

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = targetTexture;

            texture = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA64, false, true);
            texture.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
            texture.SetPixels([.. texture.GetPixels().Select(c => new Color(c.r, c.g, c.b, 1f)),]);
            texture.Apply();

            RenderTexture.active = active;

            return texture;
        }

        int width = targetTexture.width, height = targetTexture.height;

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);

        camera.targetTexture = renderTexture;
        camera.Render();

        TaskCompletionSource<AsyncGPUReadbackRequest> taskCompletionSource = new();
        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, taskCompletionSource.SetResult);
        AsyncGPUReadbackRequest request = await taskCompletionSource.Task;

        camera.targetTexture = targetTexture;
        RenderTexture.ReleaseTemporary(renderTexture);

        if (request.hasError)
        {
            Logging.Fatal("AsyncGPUReadback got error. It just has an error, that can't be disclosed, :P");
            Logging.Error("So returning null");
            Logging.Info("Hope this helps ;3");

            return null;
        }

        NativeArray<byte> data = request.GetData<byte>();
        texture = new Texture2D(width, height, TextureFormat.RGB24, false)
        {
                filterMode = FilterMode.Point,
        };

        texture.LoadRawTextureData(data);
        texture.Apply();

        return texture;
    }
}
using GorillaTagScripts.ModIO;
using ModIO;
using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Models;
using MonkePhone.Patches;
using MonkePhone.Tools;
using Photon.Pun;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.Apps
{
    public class MonkeGramApp : PhoneApp
    {
        public override string AppId => "MonkeGram";

        public bool CameraFlipped => ((PhoneCheckbox)GetObject("Camera Flip")).IsActive;
        public float CameraZoom => ((PhoneSlider)GetObject("FOV Slider")).Value;

        public bool Posted;

        private byte[] _photoByteArray;
        private bool _shutterActivated, _wasShutterActivated = true;
        private readonly float _imagesUploaded;
        private bool _isCameraFlipped;

        public Camera Camera;

        private RenderTexture renderTexture;
        private Texture2D _finalTexture, _previewTexture;

        private string _finalContents;

        private float shutter_elapsed_time = 0f;

        private bool isProcessingPhoto;

        public override void Initialize()
        {
            Camera = transform.Find("cam").GetComponent<Camera>();
            Camera.cullingMask = 1224081207;

            renderTexture = (RenderTexture)transform.Find("Background").GetComponent<RawImage>().material.mainTexture;
            renderTexture.graphicsFormat = GraphicsFormat.B8G8R8A8_SRGB;

            AdjustCameraQuality((int)Configuration.CameraResolution.Value);
        }

        public void Update()
        {
            Camera.nearClipPlane = Constants.NearClipPlane * GorillaLocomotion.GTPlayer.Instance.scale;
            Camera.farClipPlane = Camera.main.farClipPlane;

            float value = Phone.LeftHand ? ControllerInputPoller.instance.leftControllerIndexFloat : ControllerInputPoller.instance.rightControllerIndexFloat;
            _shutterActivated = value > 0.7f;

            if (value < 0.7f)
            {
                _wasShutterActivated = false;
            }

            if (_shutterActivated && !_wasShutterActivated && Phone.Held && !Phone.Leviating && !isProcessingPhoto)
            {
                PlaySound("Photo");
                Shutter();
            }

            // lazy vscode

            if (_shutterActivated && !Phone.Held && Phone.Leviating && Mathf.Approximately(shutter_elapsed_time, 0))
            {
                shutter_elapsed_time += Time.deltaTime;
            }

            shutter_elapsed_time = Mathf.Min(Mathf.Approximately(shutter_elapsed_time, 0) ? 0 : shutter_elapsed_time + Time.deltaTime, 1);

            if (Mathf.Approximately(shutter_elapsed_time, 1))
            {
                shutter_elapsed_time = 0f;
                if (!Phone.Held && Phone.Leviating && !_shutterActivated && !isProcessingPhoto)
                {
                    PlaySound("Photo");
                    Shutter();
                }
            }
        }

        public override void ButtonTicked(PhoneUIObject phoneUIObject, bool currentValue, bool isLeftHand)
        {
            switch (phoneUIObject.name)
            {
                case "Camera Flip":
                    FlipCamera(currentValue);
                    UpdateProperties();
                    break;
            }
        }

        public override void SliderUpdated(PhoneUIObject phoneUIObject, float currentValue, bool isSelected)
        {
            switch (phoneUIObject.name)
            {
                case "FOV Slider":
                    Camera.fieldOfView = Constants.FieldOfView / currentValue;
                    UpdateProperties();
                    break;
            }
        }

        public void AdjustCameraQuality(int resolution)
        {
            if (!Camera || !Camera.targetTexture) return;

            Camera.targetTexture.Release();
            Camera.targetTexture.width = resolution;
            Camera.targetTexture.height = resolution;
        }

        public void FlipCamera(bool flip)
        {
            _isCameraFlipped = flip;
            Camera.transform.localPosition = _isCameraFlipped ? Constants.CameraBackward.Position : Constants.CameraForward.Position;
            Camera.transform.localRotation = _isCameraFlipped ? Constants.CameraBackward.Rotation : Constants.CameraForward.Rotation;
        }

        public async void Shutter()
        {
            _wasShutterActivated = true;

            string currentName = PhotonNetwork.LocalPlayer.GetName(GorillaTagger.Instance.offlineVRRig, false); // we already know our player has the phone, they're taking the photo
            string currentMap = ZoneActivationPatch.ActiveZones.First().ToTitleCase().ToUpper();

            if (ModIOManager.IsLoggedIn())
            {
                currentMap = GTZone.customMaps.ToTitleCase().ToUpper();

                ModId currentRoomMap = CustomMapManager.GetRoomMapId();
                if (currentRoomMap != ModId.Null)
                {
                    ModIOManager.GetModProfile(currentRoomMap, (ModIORequestResultAnd<ModProfile> result) =>
                    {
                        if (result.result.success) // second login check, just to be safe!
                        {
                            currentMap = $"{result.data.name} ({result.data.creator.username})";
                        }
                        else
                        {
                            Logging.Warning(result.result.message);
                        }
                    });
                }
            }

            _finalContents = $"{currentName} has taken a photo ";

            if (Configuration.PlayerMention.Value && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                int supportiveLayerMask = LayerMask.GetMask
                (
                    UnityLayer.Default.ToString(),
                    UnityLayer.GorillaTagCollider.ToString(),
                    UnityLayer.GorillaObject.ToString(),
                    UnityLayer.NoMirror.ToString()
                );

                var supportiveRigs = GorillaParent.instance.vrrigs.Where(rig => rig.Creator != null && !rig.Creator.IsLocal && Camera.PointInCameraView(rig.tagSound.transform.position, true, UnityLayer.GorillaTagCollider, supportiveLayerMask));

                bool localInView = Camera.PointInCameraView(GorillaLocomotion.GTPlayer.Instance.headCollider.transform.position);

                _finalContents += supportiveRigs.Any() ? $"{(localInView ? "with" : "of")} {supportiveRigs.Select(rig => rig.Creator.GetName(rig)).ListElements()}{(Configuration.RevealMap.Value ? $" in {currentMap}" : "")}" : (Configuration.RevealMap.Value ? $"in {currentMap}" : "");
            }
            else
            {
                _finalContents += Configuration.RevealMap.Value ? $"in {currentMap}" : "";
            }

            _finalContents += ":";

            isProcessingPhoto = true;
            _finalTexture = await Camera.GetTexture();
            isProcessingPhoto = false;

            if (_finalTexture is not null)
            {
                _photoByteArray = _finalTexture.EncodeToPNG();

                string name = $"Photo_{DateTime.Now:yy_MM-dd_HH_mm_ss_ff}.png";
                var path = Path.Combine(PhoneManager.Instance.PhotosPath, name);

                await File.WriteAllBytesAsync(path, _photoByteArray);

                GetApp<GalleryApp>().RelativePhotos.Add(new Photo()
                {
                    Name = name,
                    Bytes = _photoByteArray,
                    Summary = _finalContents
                });

                Destroy(_finalTexture);
            }
        }

        public void UpdateProperties()
        {
            PhoneManager.Instance.Phone.UpdateProperties();
        }
    }
}

using MonkePhone.Interfaces;
using MonkePhone.Models;
using MonkePhone.Patches;
using MonkePhone.Tools;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MonkePhone.Networking
{
    [RequireComponent(typeof(VRRig)), DisallowMultipleComponent]
    public class NetworkedPlayer : MonoBehaviour, IPhoneAnimation
    {
        public NetPlayer Owner;

        public VRRig Rig;

        public bool InRange => Vector3.Distance(Camera.main.transform.position, transform.position) < 5f;

        public bool HasMonkePhone;

        public ObjectGrabbyState State { get; set; } = ObjectGrabbyState.Ignore;
        public bool UseLeftHand { get; set; }
        public float InterpolationTime { get; set; }
        public Vector3 GrabPosition { get; set; }
        public Quaternion GrabQuaternion { get; set; }

        private bool _isLeftHand;

        public byte GrabData;

        public float Zoom;

        public bool Flipped;

        public GameObject Phone;
        private MeshRenderer _meshRenderer;

        // Camera
        private RenderTexture _renderTexture;
        private RawImage _background;
        private Camera _camera;

        private Task createPhoneTask;

        public void Start()
        {
            NetworkHandler.Instance.OnPlayerPropertyChanged += OnPlayerPropertyChanged;
            RigLocalInvisiblityPatch.OnSetInvisibleToLocalPlayer += OnLocalInvisibilityChanged;

            if (!HasMonkePhone && Owner is PunNetPlayer punPlayer && punPlayer.PlayerRef is Player playerRef)
                NetworkHandler.Instance.OnPlayerPropertiesUpdate(playerRef, playerRef.CustomProperties);
        }

        public void OnDestroy()
        {
            NetworkHandler.Instance.OnPlayerPropertyChanged -= OnPlayerPropertyChanged;
            RigLocalInvisiblityPatch.OnSetInvisibleToLocalPlayer -= OnLocalInvisibilityChanged;

            if (HasMonkePhone)
            {
                HasMonkePhone = false;
                Rig.OnColorChanged -= OnColourChanged;
                Destroy(Phone);
            }
        }

        public async void OnPlayerPropertyChanged(NetPlayer player, Dictionary<string, object> properties)
        {
            if (player == Owner)
            {
                Logging.Info($"{player.NickName} got properties: {string.Join(", ", properties.Select(prop => $"[{prop.Key}: {prop.Value}]"))}");

                if (Phone is null)
                {
                    createPhoneTask ??= CreateMonkePhone();
                    await createPhoneTask;
                }

                if (properties.TryGetValue("Grab", out object objectForGrab) && objectForGrab is byte grab)
                {
                    GrabData = grab;
                }

                if (properties.TryGetValue("Zoom", out object objectForZoom) && objectForZoom is float zoom)
                {
                    Zoom = zoom;
                }

                if (properties.TryGetValue("Flip", out object objectForFlipped) && objectForFlipped is bool flip)
                {
                    Flipped = flip;
                }

                ConfigurePhone();
            }
        }

        private void OnLocalInvisibilityChanged(VRRig targetRig, bool isInvisible)
        {
            if (targetRig is null || Phone is null || targetRig != Rig)
                return;

            Phone.SetActive(!isInvisible);
        }

        public async Task CreateMonkePhone()
        {
            Phone = Instantiate(await AssetLoader.LoadAsset<GameObject>(Constants.NetPhoneName));
            Phone.SetActive(!Rig.IsInvisibleToLocalPlayer);
            Phone.transform.localEulerAngles = Vector3.zero;

            _meshRenderer = Phone.transform.Find("Model").GetComponent<MeshRenderer>();
            _meshRenderer.material = new Material(_meshRenderer.material);

            try
            {
                // get the background for the phone that will display our unique photo
                _background = Phone.transform.Find("Canvas/Background").GetComponent<RawImage>();

                // make our new unique photo
                RenderTexture baseRT = (RenderTexture)_background.material.mainTexture;
                _renderTexture = new RenderTexture(baseRT);
                _renderTexture.filterMode = FilterMode.Point;

                // update our camera
                _camera = Phone.transform.Find("Canvas/cam").GetComponent<Camera>();
                _camera.targetTexture = _renderTexture;
                _camera.cullingMask = 1224081207;
                _camera.gameObject.SetActive(true);

                // update our background
                _background.material = new(_background.material)
                {
                    mainTexture = _renderTexture
                };
                _background.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when attempting to prepare unique camera texture for {Rig.Creator.NickName}'s NetPhone: {ex}");
            }

            OnColourChanged(Rig.playerColor);
            Rig.OnColorChanged += OnColourChanged;
        }

        public void ConfigurePhone()
        {
            bool phoneConfigured = Phone.transform.parent is not null;

            bool isHeld = GrabData > 0 && GrabData < 3;
            bool inLeftHand = (GrabData % 2) == 1;
            bool levitate = GrabData == 3;

            try
            {
                State = isHeld ? ObjectGrabbyState.InHand : (levitate ? ObjectGrabbyState.Ignore : ObjectGrabbyState.Mounted);
                _isLeftHand = inLeftHand;
                InterpolationTime = 0f;

                var ik = Rig.myIk ?? Rig.GetComponent<GorillaIK>();
                Phone.transform.SetParent(isHeld ? (inLeftHand ? ik.leftHand : ik.rightHand) : (levitate ? null : (ik.bodyBone.Find("body") ?? ik.bodyBone.GetChild(0))));

                GrabPosition = Phone.transform.localPosition;
                GrabQuaternion = Phone.transform.localRotation;

                if (!phoneConfigured)
                {
                    Phone.transform.localScale = new Vector3(0.05f, 0.048f, 0.05f);
                }
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when updating network-content for phone of {Rig.Creator.NickName}: {ex}");
            }

            try
            {
                _camera.fieldOfView = Constants.FieldOfView / Zoom;
                _camera.transform.localRotation = Flipped ? Constants.CameraBackward.Rotation : Constants.CameraForward.Rotation;
                _camera.transform.localPosition = Flipped ? Constants.CameraBackward.Position : Constants.CameraForward.Position;
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when updating network-content for camera of {Rig.Creator.NickName}: {ex}");
            }
        }

        public void OnColourChanged(Color colour)
        {
            _meshRenderer.material.color = colour;
        }

        public void FixedUpdate()
        {
            if (Phone is null)
                return;

            if (InRange && !_camera.gameObject.activeSelf)
            {
                _camera.gameObject.SetActive(true);
                _background.gameObject.SetActive(true);
            }
            else if (!InRange && _camera.gameObject.activeSelf)
            {
                _camera.gameObject.SetActive(false);
                _background.gameObject.SetActive(false);
            }

            _camera.nearClipPlane = Constants.NearClipPlane * Rig.scaleFactor;
            _camera.farClipPlane = Camera.main.farClipPlane;

            HandlePhoneState();
        }

        public void HandlePhoneState()
        {
            switch (State)
            {
                case ObjectGrabbyState.Mounted:
                    Phone.transform.localPosition = Vector3.Lerp(GrabPosition, Constants.Waist.Position, InterpolationTime);
                    Phone.transform.localRotation = Quaternion.Lerp(GrabQuaternion, Constants.Waist.Rotation, InterpolationTime);
                    InterpolationTime += Time.deltaTime * 5f;
                    break;

                case ObjectGrabbyState.InHand:
                    Phone.transform.localPosition = Vector3.Lerp(GrabPosition, _isLeftHand ? Constants.LeftHandBasic.Position : Constants.RightHandBasic.Position, InterpolationTime);
                    Phone.transform.localRotation = Quaternion.Lerp(GrabQuaternion, _isLeftHand ? Constants.LeftHandBasic.Rotation : Constants.RightHandBasic.Rotation, InterpolationTime);
                    InterpolationTime += Time.deltaTime * 5f;
                    break;
            }
        }
    }
}

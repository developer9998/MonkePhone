using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using MonkePhone.Behaviours.Apps;
using MonkePhone.Interfaces;
using MonkePhone.Models;
using MonkePhone.Networking;
using MonkePhone.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace MonkePhone.Behaviours;

public class Phone : HoldableObject, IPhoneAnimation
{
    public bool InHand, InLeftHand;

    public List<PhoneHandDependentObject> HandDependentObjects;

    public bool levitate_device;

    private Vector3 _initialPosition;

    private       bool _isSwapped, _wasSwappedLeft;
    public static bool Held     => PhoneManager.Instance.Phone.InHand;
    public static bool LeftHand => PhoneManager.Instance.Phone.InLeftHand;

    public static bool Leviating => PhoneManager.Instance.Phone.levitate_device;

    public void Awake()
    {
        HandDependentObjects = [];

        transform.localScale = new Vector3(0.05f, 0.048f, 0.05f);

        if (Configuration.InitialPosition.Value == Configuration.EInitialPhoneLocation.Levitate)
        {
            transform.position = new Vector3(-66.8901f, 11.9f, -82.6056f);
            _initialPosition   = transform.position;

            return;
        }

        if (Configuration.InitialPosition.Value == Configuration.EInitialPhoneLocation.Table)
        {
            Vector3 position = new(-65.7592f, 12f, -80.0929f);
            transform.eulerAngles = new Vector3(0f, 287.8041f, 270f);

            LayerMask layerMask = LayerMask.GetMask(LayerMask.LayerToName(UnityLayer.Default.ToLayerIndex()),
                    LayerMask.LayerToName(UnityLayer.GorillaObject.ToLayerIndex()));

            if (Physics.Raycast(new Ray(position, Vector3.down), out RaycastHit hit, 1f, layerMask,
                        QueryTriggerInteraction.UseGlobal))
                transform.position = hit.point;
            else
                transform.position = position;

            return;
        }

        InterpolationTime = 1f;
        State             = ObjectGrabbyState.Mounted;
        transform.SetParent(GorillaTagger.Instance.offlineVRRig.headMesh.transform.parent);
    }

    public void Update()
    {
        Vector3 currentLeftControllerPosition  = GTPlayer.Instance.leftHand.handFollower.position;
        Vector3 currentRightControllerPosition = GTPlayer.Instance.rightHand.handFollower.position;
        Vector3 currentHandheldPosition        = transform.position + transform.rotation * Vector3.zero;

        bool leftGrip         = ControllerInputPoller.GetGrab(XRNode.LeftHand);
        bool rightGrip        = ControllerInputPoller.GetGrab(XRNode.RightHand);
        bool leftGrabRelease  = ControllerInputPoller.GetGrabRelease(XRNode.LeftHand);
        bool rightGrabRelease = ControllerInputPoller.GetGrabRelease(XRNode.RightHand);

        float grabDistance = Constants.GrabDistance * GTPlayer.Instance.scale;

        if (_isSwapped && (!_wasSwappedLeft ? leftGrabRelease : rightGrabRelease))
            _isSwapped = false;

        bool isHoldingLeftPiece = BuilderPieceInteractor.instance.heldPiece.ElementAtOrDefault(0) is not null ||
                                  GamePlayerLocal.instance.gamePlayer.GetGameEntityId(true).IsValid();

        bool isHoldingRightPiece = BuilderPieceInteractor.instance.heldPiece.ElementAtOrDefault(1) is not null ||
                                   GamePlayerLocal.instance.gamePlayer.GetGameEntityId(false).IsValid();

        bool isGrabbingLeft =
                leftGrip && Vector3.Distance(currentLeftControllerPosition, currentHandheldPosition) < grabDistance &&
                !InHand  && EquipmentInteractor.instance.leftHandHeldEquipment == null && !isHoldingLeftPiece &&
                !_isSwapped;

        bool isSwappingLeft = Configuration.HandSwapping.Value && InHand && leftGrip && rightGrip && !_isSwapped &&
                              Vector3.Distance(currentLeftControllerPosition, currentHandheldPosition) < grabDistance &&
                              !_wasSwappedLeft && EquipmentInteractor.instance.leftHandHeldEquipment == null &&
                              !isHoldingLeftPiece;

        if (isGrabbingLeft || isSwappingLeft)
        {
            _isSwapped      = isSwappingLeft;
            _wasSwappedLeft = true;
            InLeftHand      = true;
            InHand          = true;

            transform.SetParent(GorillaTagger.Instance.offlineVRRig.leftHandTransform.parent);

            Vibration(true, 0.1f, 0.05f);
            EquipmentInteractor.instance.leftHandHeldEquipment = this;

            if (_isSwapped)
                EquipmentInteractor.instance.rightHandHeldEquipment = null;

            Grabbed();
        }
        else if (leftGrabRelease && InHand && InLeftHand)
        {
            InLeftHand = true;
            InHand     = false;
            transform.SetParent(null);

            EquipmentInteractor.instance.leftHandHeldEquipment = null;
            Dropped();
        }

        bool isGrabbingRight = rightGrip &&
                               Vector3.Distance(currentRightControllerPosition, currentHandheldPosition) <
                               grabDistance && !InHand && EquipmentInteractor.instance.rightHandHeldEquipment == null &&
                               !isHoldingRightPiece && !_isSwapped;

        bool isSwappingRight = Configuration.HandSwapping.Value && InHand && leftGrip && rightGrip && !_isSwapped &&
                               Vector3.Distance(currentRightControllerPosition, currentHandheldPosition) <
                               grabDistance                                                && _wasSwappedLeft &&
                               EquipmentInteractor.instance.rightHandHeldEquipment == null && !isHoldingRightPiece;

        if (isGrabbingRight || isSwappingRight)
        {
            _isSwapped      = isSwappingRight;
            _wasSwappedLeft = false;
            InLeftHand      = false;
            InHand          = true;

            transform.SetParent(GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent);

            Vibration(false, 0.1f, 0.05f);
            EquipmentInteractor.instance.rightHandHeldEquipment = this;

            if (_isSwapped)
                EquipmentInteractor.instance.leftHandHeldEquipment = null;

            Grabbed();
        }
        else if (rightGrabRelease && InHand && !InLeftHand)
        {
            InLeftHand = false;
            InHand     = false;
            transform.SetParent(null);

            EquipmentInteractor.instance.rightHandHeldEquipment = null;
            Dropped();
        }
    }

    public void FixedUpdate()
    {
        HandlePhoneState();
    }

    public ObjectGrabbyState State             { get; set; }
    public bool              UseLeftHand       { get; set; }
    public float             InterpolationTime { get; set; }
    public Vector3           GrabPosition      { get; set; }
    public Quaternion        GrabQuaternion    { get; set; }

    public void HandlePhoneState()
    {
        switch (State)
        {
            case ObjectGrabbyState.Mounted:
                if (!levitate_device)
                {
                    transform.localPosition = Vector3.Lerp(GrabPosition, Constants.Waist.Position, InterpolationTime);
                    transform.localRotation =
                            Quaternion.Lerp(GrabQuaternion, Constants.Waist.Rotation, InterpolationTime);

                    InterpolationTime += Time.deltaTime * 5f;
                }

                break;

            case ObjectGrabbyState.InHand:
                transform.localPosition = Vector3.Lerp(GrabPosition,
                        InLeftHand ? Constants.LeftHandBasic.Position : Constants.RightHandBasic.Position,
                        InterpolationTime);

                transform.localRotation = Quaternion.Lerp(GrabQuaternion,
                        InLeftHand ? Constants.LeftHandBasic.Rotation : Constants.RightHandBasic.Rotation,
                        InterpolationTime);

                InterpolationTime += Time.deltaTime * 5f;

                break;

            case ObjectGrabbyState.Awake:
                if (Configuration.InitialPosition.Value == Configuration.EInitialPhoneLocation.Levitate)
                {
                    transform.position =
                            _initialPosition + Vector3.up * (Mathf.Sin(Time.frameCount * 0.017453292f) / 10f);

                    transform.Rotate(Vector3.up * 10f / 0.017453292f / 5f * Time.fixedDeltaTime, Space.World);
                }

                break;
        }
    }

    public void Vibration(bool isLeftHand, float amplitude, float duration)
    {
        if (!Configuration.ObjectHaptics.Value)
            return;

        GorillaTagger.Instance.StartVibration(isLeftHand, amplitude, duration);
    }

#region Physical Interaction

    public void Grabbed()
    {
        levitate_device = false;

        if (State == ObjectGrabbyState.Awake && !PhoneManager.Instance.IsOutdated)
            PhoneManager.Instance.PlaySound("InitialGrab", 0.7f);

        if (State == ObjectGrabbyState.Awake && !PhoneManager.Instance.IsPowered)
            PhoneManager.Instance.SetPower(true);

        InterpolationTime = 0f;
        State             = ObjectGrabbyState.InHand;
        GrabPosition      = transform.localPosition;
        GrabQuaternion    = transform.localRotation;

        UpdateProperties();

        foreach (PhoneHandDependentObject component in HandDependentObjects)
            component.SetFlip(!InLeftHand);
    }

    public void Dropped()
    {
        levitate_device = LeftHand  && ControllerInputPoller.instance.leftControllerPrimaryButton ||
                          !LeftHand && ControllerInputPoller.instance.rightControllerPrimaryButton;

        if (levitate_device)
        {
            transform.SetParent(null);
        }
        else
        {
            transform.SetParent(GorillaTagger.Instance.offlineVRRig.headMesh.transform.parent);
            GrabPosition   = transform.localPosition;
            GrabQuaternion = transform.localRotation;
        }

        InterpolationTime = 0f;
        State             = ObjectGrabbyState.Mounted;

        HandlePhoneState();
        UpdateProperties();
    }

#endregion

#region Custom Properties

    public void UpdateProperties()
    {
        NetworkHandler networkHandler = NetworkHandler.Instance;

        MonkeGramApp monkeGram = PhoneManager.Instance.GetApp<MonkeGramApp>();

        networkHandler.SetProperty("Grab", (byte)(!InHand ? levitate_device ? 3 : 0 : InLeftHand ? 1 : 2));
        networkHandler.SetProperty("Zoom", monkeGram.CameraZoom);
        networkHandler.SetProperty("Flip", monkeGram.CameraFlipped);
    }

    public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
    {
        //throw new System.NotImplementedException();
    }

    public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
    {
        //throw new System.NotImplementedException();
    }

    public override void DropItemCleanup()
    {
        //throw new System.NotImplementedException();
    }

#endregion
}
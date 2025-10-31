using MonkePhone.Models;
using UnityEngine;

namespace MonkePhone.Interfaces;

public interface IPhoneAnimation
{
    ObjectGrabbyState State             { get; set; }
    bool              UseLeftHand       { get; set; }
    float             InterpolationTime { get; set; }
    Vector3           GrabPosition      { get; set; }
    Quaternion        GrabQuaternion    { get; set; }
    void              HandlePhoneState();
}
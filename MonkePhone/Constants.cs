using MonkePhone.Models;
using UnityEngine;

namespace MonkePhone
{
    internal class Constants
    {
        // Plugin

        /// <summary>
        /// The GUID (globally unique identifier) used when registering the plugin
        /// </summary>
        public const string GUID = "dev.monkephone";

        /// <summary>
        /// The name of the plugin
        /// </summary>
        public const string Name = "MonkePhone";

        /// <summary>
        /// The version of the plugin (formatted into <see cref="System.Version"/>)
        /// </summary>
        public const string Version = "1.0.5";

        // Assets

        public const string BundleName = "MonkePhone.Content.phone";

        public const string LocalPhoneName = "phone";

        public const string NetPhoneName = "dummyphone";

        // Networking

        public const string CustomProperty = "MonkePhone"; // monkephone 2025

        public const float PhoneVisibilityDistance = 2f;

        public const float NetworkCooldown = 0.2f;

        public const int NetworkWaitTime = 300;

        // Camera

        public const float FieldOfView = 70f;
        public const float NearClipPlane = 0.036f;

        public static readonly ObjectPosition CameraForward = new()
        {
            Position = new Vector3(0f, 57.6f, -1.1f),
            Rotation = Quaternion.AngleAxis(180f, Vector3.up)
        };

        public static readonly ObjectPosition CameraBackward = new()
        {
            Position = new Vector3(0f, 68.5f, 7.5f),
            Rotation = Quaternion.AngleAxis(0f, Vector3.up)
        };

        // Holdable

        public const float GrabDistance = 0.12f;

        public static readonly ObjectPosition Waist = new()
        {
            Position = new Vector3(-0.1737f, 0.0733f, 0.04f),
            Rotation = Quaternion.Euler(284.7374f, 197.9889f, 0f)
        };

        public static readonly ObjectPosition LeftHandBasic = new()
        {
            Position = new Vector3(-0.0963f, 0.087f, 0.0238f),
            Rotation = Quaternion.Euler(41.609f, 75.893f, 71.86f)
        };

        public static readonly ObjectPosition RightHandBasic = new()
        {
            Position = new Vector3(0.0994f, 0.0902f, 0.0402f),
            Rotation = Quaternion.Euler(318.182f, 90.532f, 85.883f)
        };

        public static readonly ObjectPosition LeftHandWeird = new()
        {
            Position = new Vector3(-0.08280078f, 0.08130081f, 0.07060041f),
            Rotation = Quaternion.Euler(440f, 84f, 83f)
        };

        public static readonly ObjectPosition RightHandWeird = new()
        {
            Position = new Vector3(0.0828f, 0.0813f, 0.04f),
            Rotation = Quaternion.Euler(280f, 96f, 83f)
        };
    }
}

using System.ComponentModel;
using BepInEx.Configuration;
using MonkePhone.Behaviours;
using MonkePhone.Behaviours.Apps;

namespace MonkePhone.Tools
{
    public class Configuration
    {
        public static ConfigFile File;

        public static ConfigEntry<string> WebhookEndpoint;

        // Appearance //

        public static ConfigEntry<string> WallpaperName;

        public static ConfigEntry<EPhoneResolution> CameraResolution;

        // Auditory //

        public static ConfigEntry<float> VolumeMultiplier;

        public static ConfigEntry<float> MusicMultiplier;

        public static ConfigEntry<bool> UseSpatialBlend;

        // Behaviour //

        public static ConfigEntry<bool> AutoPowered;

        public static ConfigEntry<EInitialPhoneLocation> InitialPosition;

        public static ConfigEntry<bool> HandSwapping;

        public static ConfigEntry<bool> PlayerMention;

        public static ConfigEntry<bool> RevealMap;

        // Tactile //

        public static ConfigEntry<bool> ObjectHaptics;

        public static ConfigEntry<bool> AppHaptics;

        public static ConfigEntry<bool> SoundHaptics;

        public static ConfigEntry<bool> MusicHaptics;

        public static void Construct(ConfigFile file)
        {
            File = file;

            WebhookEndpoint = File.Bind
            (
                Constants.Name,
                "Webhook Url",
                "<INSERT WEBHOOK HERE>",
                "The user-defined webhook for posting captured photos. Please be cautious with the distribution of your webhook."
            );

            WallpaperName = File.Bind
            (
                "Appearance",
                "Wallpaper",
                "",
                new ConfigDescription("The photo currently being used as the wallpaper")
            );

            CameraResolution = File.Bind
            (
                "Appearance",
                "Camera Resolution",
                EPhoneResolution.High,
                new ConfigDescription("The image resolution used by the phone cameras")
            );

            CameraResolution.SettingChanged += (object sender, System.EventArgs e) =>
            {
                var resolution = CameraResolution.Value;
                PhoneHandler.Instance?.GetApp<MonkeGramApp>()?.AdjustCameraQuality((int)resolution);
            };

            VolumeMultiplier = File.Bind
            (
                "Auditory",
                "Sound Volume",
                1f,
                new ConfigDescription("The volume for sound produced by the phone", new AcceptableValueRange<float>(0f, 1f))
            );

            MusicMultiplier = File.Bind
            (
                "Auditory",
                "Music Volume",
                1f,
                new ConfigDescription("The volume for music produced by the phone", new AcceptableValueRange<float>(0f, 1f))
            );

            MusicMultiplier.SettingChanged += (object sender, System.EventArgs e) =>
            {
                PhoneHandler.Instance?.GetApp<MusicApp>()?.SetVolumeMultiplier(MusicMultiplier.Value);
            };

            UseSpatialBlend = File.Bind
            (
                "Auditory",
                "Use Spatial Blend",
                true,
                new ConfigDescription("If music produced by the phone will be affected by spatialisation calculations")
            );

            UseSpatialBlend.SettingChanged += (object sender, System.EventArgs e) =>
            {
                PhoneHandler.Instance?.GetApp<MusicApp>()?.SetSpatialBlend(UseSpatialBlend.Value);
            };

            AutoPowered = File.Bind
            (
                "Behaviour",
                "Auto Powered",
                true,
                new ConfigDescription("If the phone is powered when opening the game")
            );

            InitialPosition = File.Bind
            (
                "Behaviour",
                "Initial Location",
                EInitialPhoneLocation.Table,
                new ConfigDescription("Where the phone is located when opening the game")
            );

            HandSwapping = File.Bind
            (
                "Behaviour",
                "Phone Swapping",
                true,
                new ConfigDescription("If the phone can be swapped from hands when being held")
            );

            PlayerMention = File.Bind
            (
                "Behaviour",
                "Player Mentioning",
                true,
                new ConfigDescription("If any players visible in a photo are mentioned when uploaded")
            );

            RevealMap = File.Bind
            (
                "Behaviour",
                "Reveal Map",
                true,
                new ConfigDescription("whether the current map should be disclosed in posts")
            );

            ObjectHaptics = File.Bind
            (
                "Tactile",
                "Object Vibrations",
                true,
                new ConfigDescription("If the phone object can produce vibrations")
            );

            AppHaptics = File.Bind
            (
                "Tactile",
                "App Vibrations",
                true,
                new ConfigDescription("If any phone app can produce vibrations")
            );

            SoundHaptics = File.Bind
            (
                "Tactile",
                "Sound Vibrations",
                false,
                new ConfigDescription("If sound produced by the phone can replicate vibrations")
            );

            MusicHaptics = File.Bind
            (
                "Tactile",
                "Music Vibrations",
                false,
                new ConfigDescription("If music produced by the phone can replicate vibrations")
            );
        }

        public enum EInitialPhoneLocation
        {
            [Description("The phone will be placed on the table in stump")]
            Table,
            [Description("The phone will levitate in the centre of stump")]
            Levitate,
            [Description("The phone will be mounted to the waist of the player")]
            Waist
        }

        public enum EPhoneResolution
        {
            [Description("128p 2^7")]
            Poor = 128,
            [Description("256p 2^8")]
            Low = 256,
            [Description("512p 2^9")]
            Medium = 512,
            [Description("1k 2^10")]
            High = 1024,
            [Description("2k 2^11")]
            Ultra = 2048,
            [Description("4k 2^12")]
            Profesional = 4096
        }
    }
}

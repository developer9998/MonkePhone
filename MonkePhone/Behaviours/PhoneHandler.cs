using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using MonkePhone.Behaviours.Apps;
using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Interfaces;
using MonkePhone.Models;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MonkePhone.Behaviours
{
    public class PhoneHandler : MonoBehaviour
    {
        public static volatile PhoneHandler Instance;

        public bool Initialized;

        public string PhotosPath => Path.Combine(Paths.BepInExRootPath, "MonkePhone", "Photos");
        public string MusicPath => Path.Combine(Paths.BepInExRootPath, "MonkePhone", "Music");
        public bool InHomeScreen => _openedApps.Count == 0;

        public PhoneOnlineData Data;

        public Phone Phone;
        public Keyboard Keyboard;

        public bool IsPowered = true, IsOutdated;

        private readonly List<PhoneApp> _openedApps = [], _storedApps = [];
        private readonly List<Sound> _sounds = [];
        private readonly List<AudioSource> _audioSourceCache = [];

        private GameObject _homeMenuObject, _outdatedMenuObject;
        private RawImage _genericWallpaper, _customWallpaper;

        public async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            if (!Directory.Exists(PhotosPath))
            {
                Directory.CreateDirectory(PhotosPath);
            }

            if (!Directory.Exists(MusicPath))
            {
                Directory.CreateDirectory(MusicPath);
            }

            await Initialize();
            Initialized = true;

            try
            {
                CreateSound("Photo", "365711__biancabothapure__taking-photos");
                CreateSound("InitialGrab", "457518__graham_makes__chord-alert-notification");
                CreateSound("BasicTap", "582903__ironcross32__snap-click-04");
                CreateSound("MenuTap", "582905__ironcross32__snap-click-08");
                CreateSound("PadShow", "582895__ironcross32__short-woosh-04");
                CreateSound("PadHide", "582896__ironcross32__short-woosh-03");
                CreateSound("RequestSuccess", "582890__ironcross32__short-crackle-03");
                CreateSound("RequestDenied", "582632__ironcross32__permission-denied");
                CreateSound("Delete", "496152__aiwha__paper-crumple");
                CreateSound("MailReceived", "582636__ironcross32__long-lowering-tones-01");
                CreateSound("SwitchOn", "SwitchOn");
                CreateSound("SwitchOff", "SwitchOff");
                CreateSound("Scribbletrue", "451647__toddcircle__pencil-on-paper-scribble-1");
                CreateSound("Scribblefalse", "451647__toddcircle__pencil-on-paper-scribble-2");
                CreateSound("Key", "561678__mattruthsound__keyboard-computer-mechanical-typing-press-button-click-tap-three-key-presses-rapidly_96khz_mono_zoomh4n_nt5");
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when creating sound effets: {ex}");
                return;
            }

            try
            {
                Phone.transform.Find("Model").GetComponent<MeshRenderer>().material.color = GorillaTagger.Instance.offlineVRRig.playerColor;
                Keyboard.Mesh.material.color = GorillaTagger.Instance.offlineVRRig.playerColor;

                GorillaTagger.Instance.offlineVRRig.OnColorChanged += UpdateColour;
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when applying changes to phone: {ex}");
                return;
            }

            if (!Configuration.AutoPowered.Value)
            {
                SetPower(false);
            }
        }

        public async Task Initialize()
        {
            Logging.Info("Starting init");
            try
            {
                await AssetLoader.LoadAsset<GameObject>(Constants.NetPhoneName);

                GameObject phoneObject = Instantiate(await AssetLoader.LoadAsset<GameObject>(Constants.LocalPhoneName));
                Phone = phoneObject.AddComponent<Phone>();
                Keyboard = phoneObject.transform.Find("Keyboard").AddComponent<Keyboard>();

                foreach (Transform t in Phone.transform.Find("Canvas"))
                {
                    // Logging.Log(t.name);
                    switch (t.name)
                    {
                        case "Home Screen":
                            _homeMenuObject = t.gameObject;
                            _genericWallpaper = t.Find("GenericBackground").GetComponent<RawImage>();
                            _customWallpaper = t.Find("PictureBackground").GetComponent<RawImage>();
                            break;

                        case "WrongVersionScreen":
                            _outdatedMenuObject = t.gameObject;
                            break;

                        case "MonkeGramApp":
                            CreateApp<MonkeGramApp>(t.gameObject);
                            break;

                        case "ConfigurationApp":
                            CreateApp<ConfigurationApp>(t.gameObject);
                            break;

                        case "GalleryApp":
                            CreateApp<GalleryApp>(t.gameObject);
                            break;

                        case "MusicApp":
                            CreateApp<MusicApp>(t.gameObject);
                            break;

                        case "ScoreboardApp":
                            CreateApp<ScoreboardApp>(t.gameObject);
                            break;

                        case "MessagingApp":
                            CreateApp<MessagingApp>(t.gameObject);
                            break;

                        case "Top Bar":
                            t.AddComponent<PhoneTopBar>();
                            break;

                        default:
                            if (t.TryGetComponent(out PhoneHandDependentObject component))
                            {
                                Phone.HandDependentObjects.Add(component);
                            }
                            break;
                    }
                    // Logging.Log("nice");
                }

                Logging.Info("passed loading phone");
            }

            catch (Exception ex)
            {
                Logging.Error($"Error when loading phone object: {ex}");
                return;
            }

            UnityWebRequest versionWebRequest = UnityWebRequest.Get("https://pastebin.com/raw/cWxAUZ8M");
            await YieldUtils.Yield(versionWebRequest);

            if (versionWebRequest.result == UnityWebRequest.Result.Success)
            {
                Data = versionWebRequest.downloadHandler.text.FromJson<PhoneOnlineData>();
                /*
                if (Data != null && Data.version != Constants.Version)
                {
                    Logging.Warning($"Outdated build! Current build is {Constants.Version}, expected {Data.version}");
                    IsOutdated = true;
                    _outdatedMenuObject.SetActive(true);
                    _outdatedMenuObject.transform.Find("DiscordURL").GetComponent<Text>().text = Data.invite;
                    _homeMenuObject.SetActive(false);
                    return;
                }
                */
                Logging.Log($"Correct build, version data exists: {Data != null}");
            }
            else
            {
                Logging.Error($"Error when checking version (maybe pastebin is gone?): {versionWebRequest.downloadHandler.text}");

                IsOutdated = true;
                _outdatedMenuObject.SetActive(true);
                _outdatedMenuObject.transform.Find("DiscordURL").GetComponent<Text>().text = "discord.gg/monkephone";
                _homeMenuObject.SetActive(false);
                return;
                //idk what this does
            }

            Data.songs.ForEach(song => song.currentState = song.IsDownloaded ? Song.DownloadState.Downloaded : 0);

            try
            {
                string wallpaperName = Configuration.WallpaperName.Value;
                if (!wallpaperName.All(char.IsWhiteSpace) && File.Exists(Path.Combine(PhotosPath, wallpaperName)))
                {
                    var wallpaper = new Texture2D(2, 2);
                    wallpaper.LoadImage(File.ReadAllBytes(Path.Combine(PhotosPath, wallpaperName)));
                    wallpaper.Apply();
                    wallpaper.filterMode = FilterMode.Point;

                    _genericWallpaper.gameObject.SetActive(false);
                    _customWallpaper.gameObject.SetActive(true);
                    _customWallpaper.material.mainTexture = wallpaper;
                }
                else
                {
                    _genericWallpaper.gameObject.SetActive(true);
                    _customWallpaper.gameObject.SetActive(false);
                }

                Logging.Log("passed applying wallpaper");
            }

            catch (Exception ex)
            {
                Logging.Error($"Error when applying wallpaper to phone: {ex}");
                return;
            }

            try
            {
                Phone.UpdateProperties();
            }

            catch (Exception ex)
            {
                Logging.Error($"Error when setting custom properties: {ex}");
                return;
            }
        }

        public void LateUpdate()
        {
            if (!Initialized || !Phone.InHand) return;

            HandleSoundHaptics();
            HandleMusicHaptics();
        }

        public void HandleSoundHaptics()
        {
            if (Configuration.SoundHaptics.Value)
            {
                IEnumerable<AudioSource> playingAudios = _audioSourceCache.Where(audio => audio.isPlaying);

                if (playingAudios.Any())
                {
                    float totalLoudness = 0f;

                    playingAudios.ForEach(audio => totalLoudness += audio.GetLoudness());

                    totalLoudness = Mathf.Clamp(totalLoudness, 0, 30);

                    GorillaTagger.Instance.StartVibration(Phone.InLeftHand, totalLoudness / 8f / 30f, Time.deltaTime);
                }
            }
        }

        public void HandleMusicHaptics()
        {
            if (Configuration.MusicHaptics.Value && GetApp<MusicApp>().MusicSource.isPlaying)
            {
                float totalLoudness = Mathf.Clamp(GetApp<MusicApp>().MusicSource.GetLoudness(), 0, 30);
                GorillaTagger.Instance.StartVibration(Phone.InLeftHand, totalLoudness / 8f / 30f, Time.deltaTime);
            }
        }

        public void ApplyWallpaper(bool useGenericWallpaper, string customImageName)
        {
            _genericWallpaper.gameObject.SetActive(useGenericWallpaper);
            _customWallpaper.gameObject.SetActive(!useGenericWallpaper);

            if (useGenericWallpaper)
            {
                return;
            }

            string name = Path.Combine(PhotosPath, customImageName);

            if (!File.Exists(name))
            {
                Logging.Warning($"Custom wallpaper cannot be applied with missing file: {customImageName}");
                return;
            }

            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(name));
            tex.Apply();

            Configuration.WallpaperName.Value = customImageName;
            _customWallpaper.material.mainTexture = tex;
        }

        #region Sounds

        public async void CreateSound(string soundId, string assetName)
        {
            Sound sound = GetSound(soundId);
            if (SoundExists(sound))
            {
                Logging.Warning($"A sound with the Id {soundId} has already been created");
                return;
            }

            AudioClip audio = await AssetLoader.LoadAsset<AudioClip>(assetName);
            if (audio == null)
            {
                Logging.Warning($"An AudioClip with the name {assetName} is not included in the phone bundle");
                return;
            }

            sound = new Sound()
            {
                Id = soundId,
                Audio = audio
            };

            _sounds.Add(sound);
        }

        public void PlaySound(string soundId, float volume = 1f)
        {
            Sound sound = GetSound(soundId);
            if (!SoundExists(sound))
            {
                Logging.Warning($"A sound with the Id {soundId} does not exist, and therefore cannot be played");
                return;
            }

            AudioSource audio = _audioSourceCache.Count == 0 ? null : _audioSourceCache.FirstOrDefault(audio => audio && !audio.isPlaying);

            if (!audio)
            {
                var audioObject = new GameObject($"Audio Cache #{_audioSourceCache.Count + 1}", typeof(AudioSource));
                audioObject.transform.SetParent(Phone.transform);
                audioObject.transform.localPosition = Vector3.zero;

                audio = audioObject.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 1f;
                audio.dopplerLevel = 0f;
                _audioSourceCache.Add(audio);
            }

            audio.clip = sound.Audio;
            audio.volume = 0.6f * (Mathf.Clamp01(volume) * Configuration.VolumeMultiplier.Value);
            audio.Play();
        }

        public bool SoundExists(Sound sound) => !sound.Equals(default(Sound));
        public Sound GetSound(string soundId) => _sounds.FirstOrDefault(sound => sound.Id == soundId);

        public struct Sound
        {
            public string Id;
            public AudioClip Audio;
        }

        #endregion

        #region Apps

        public T CreateApp<T>(GameObject appObject) where T : PhoneApp => (T)CreateApp(typeof(T), appObject);

        public PhoneApp CreateApp(Type appType, GameObject appObject)
        {
            if (!TryGetComponent(appType, out Component app))
            {
                app = appObject.AddComponent(appType);
                PhoneApp phoneApp = (PhoneApp)app;

                try
                {
                    phoneApp.Initialize();
                }
                catch (Exception ex)
                {
                    Logging.Error($"App with type {appType.FullName} could not be initialized: {ex}");
                }

                _storedApps.Add(phoneApp);
            }

            return (PhoneApp)app;
        }

        public void OpenApp(string appId)
        {
            PhoneApp app = GetApp(appId);
            if (!AppExists(app) || AppOpened(app))
            {
                Logging.Warning($"{appId} is already opened");
                return;
            }

            _openedApps.Add(app);
            _homeMenuObject.SetActive(false);

            app.gameObject.SetActive(true);
            app.AppOpened();
        }

        public void CloseApp(string appId) => CloseApp_Local(appId, true);

        private void CloseApp_Local(string appId, bool fullClosure)
        {
            PhoneApp app = GetApp(appId);
            if (!AppExists(app) || !AppOpened(app))
            {
                Logging.Warning($"{appId} is already closed");
                return;
            }

            app.gameObject.SetActive(false);
            app.AppClosed();

            if (fullClosure)
            {
                _openedApps.Remove(app);
                _homeMenuObject.SetActive(InHomeScreen);
            }
        }

        public bool AppOpened(string appId) => AppOpened(GetApp(appId));

        public bool AppOpened(PhoneApp app) => AppExists(app) && _openedApps.Contains(app);
        public bool AppExists(PhoneApp app) => app;

        public T GetApp<T>() where T : PhoneApp, IPhoneApp
        {
            if (!_storedApps.Any())
            {
                Logging.Warning($"App with type {typeof(T).Name} could not be found (no apps have been created)");
                return default;
            }

            foreach (var app in _storedApps)
            {
                if (app.TryGetComponent(typeof(T), out var component))
                {
                    return (T)component;
                }
            }

            Logging.Warning($"App with type {typeof(T).Name} could not be found (app with type has not been created)");
            return default;
        }

        public PhoneApp GetApp(string appId) => !_storedApps.Any() ? null : _storedApps.FirstOrDefault(app => app.AppId == appId);

        #endregion

        public void UpdateColour(Color colour)
        {
            if (!Initialized)
            {
                Logging.Warning("Phone is not initialized, and therefore can not update colour");
                return;
            }

            Color playerColour = new(Mathf.Clamp01(colour.r), Mathf.Clamp01(colour.g), Mathf.Clamp01(colour.b));
            Phone.transform.Find("Model").GetComponent<MeshRenderer>().material.color = playerColour;
            Keyboard.Mesh.material.color = playerColour;
        }

        public void SetHome()
        {
            if (InHomeScreen)
            {
                Logging.Warning("Phone is already at the home screen");
                return;
            }

            _openedApps.ForEach(app => CloseApp_Local(app.AppId, false));
            _openedApps.Clear();

            _homeMenuObject.SetActive(InHomeScreen && !IsOutdated);
            _outdatedMenuObject.SetActive(IsOutdated);
            Keyboard.Active = false;
        }

        public void TogglePower() => SetPower(!IsPowered);

        public void SetPower(bool usePoweredState)
        {
            if (IsPowered == usePoweredState)
            {
                Logging.Warning($"Phone power is already set to {usePoweredState}");
                return;
            }

            IsPowered = usePoweredState;

            _homeMenuObject.SetActive(IsPowered && InHomeScreen && !IsOutdated);
            _outdatedMenuObject.SetActive(IsPowered && IsOutdated);

            foreach (PhoneApp app in _openedApps)
            {
                app.gameObject.SetActive(IsPowered);
            }

            PlaySound(IsPowered ? "PadShow" : "PadHide", 0.4f);
        }
    }
}

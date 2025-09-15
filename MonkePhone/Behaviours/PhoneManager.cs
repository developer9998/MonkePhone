using BepInEx;
using MonkePhone.Behaviours.Apps;
using MonkePhone.Extensions;
using MonkePhone.Interfaces;
using MonkePhone.Models;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MonkePhone.Behaviours
{
    public class PhoneManager : MonoBehaviour
    {
        public static volatile PhoneManager Instance;

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

        public GameObject watchPromoObject;

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

        /*
#if DEBUG

        public void OnGUI()
        {
            bool flag = GUI.Button(new Rect(128f, 128f, 150f, 35f), "Post Sample");
            if (flag)
            {
                byte[] array = Convert.FromBase64String("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[...string is too long...]");
                Photo photo = new()
                {
                    Name = string.Concat(name, ".png"),
                    Bytes = array,
                    Summary = "bleh"
                };
                GetApp<GalleryApp>().RelativePhotos.Add(photo);
                GetApp<GalleryApp>().SendWebhook(photo.Summary, photo.Name, photo.Bytes);
            }
        }

#endif
        */

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
                            watchPromoObject = t.Find("InfoWatchPromo").gameObject;
                            if (PlayerPrefs.GetInt("IgnorePromo", 0) == 1) watchPromoObject.SetActive(false);
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

                        /*
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
                        */

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

            UnityWebRequest versionWebRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/developer9998/MonkePhone/main/FreeCloudStorageGalore.json");
            await YieldUtils.Yield(versionWebRequest);

            if (versionWebRequest.result == UnityWebRequest.Result.Success)
            {
                Data = versionWebRequest.downloadHandler.text.FromJson<PhoneOnlineData>();

                if (Data != null && Version.TryParse(Constants.Version, out Version installed) && Version.TryParse(Data.version, out Version version) && version > installed)
                {
                    Logging.Warning($"Outdated build! Current build is {installed}, expected {version}");
                    IsOutdated = true;
                    _outdatedMenuObject.SetActive(true);
                    _outdatedMenuObject.transform.Find("DiscordURL").GetComponent<Text>().text = Data.invite;
                    _homeMenuObject.SetActive(false);
                    return;
                }

                Logging.Log($"Correct build, version data exists: {Data != null}");
            }
            else
            {
                Logging.Error($"Error when checking version (maybe pastebin is gone?): {versionWebRequest.downloadHandler.text}");

                IsOutdated = true;
                _outdatedMenuObject.SetActive(true);
                _outdatedMenuObject.transform.Find("DiscordURL").GetComponent<Text>().text = "discord.gg/dev9998";
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

            Configuration.WallpaperName.Value = customImageName;

            if (useGenericWallpaper) return;

            string name = Path.Combine(PhotosPath, customImageName);

            if (!File.Exists(name))
            {
                Logging.Warning($"Custom wallpaper cannot be applied with missing file: {customImageName}");
                ApplyWallpaper(true, string.Empty);
                return;
            }

            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(name));
            tex.Apply();

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

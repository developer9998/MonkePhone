using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Models;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.Apps
{
    public class MusicApp : PhoneApp
    {
        public override string AppId => "Music";

        private List<string> _musicList;
        private Dictionary<string, AudioClip> _musicComparison = [];

        private PhoneSlider _timelineSlider;
        private Text _songTitle, _songTimePosition;

        public AudioSource MusicSource;

        private bool _isLoadingMusic;
        private bool _wasPlaying = true;

        private bool _inDownloadView = true;
        private int _currentPage;

        private List<StreamableMusicComponent> _streambleMusicComponents = [];

        public override void Initialize()
        {
            MusicSource = PhoneHandler.Instance.Phone.transform.Find("Music Source").GetComponent<AudioSource>();

            _songTitle = transform.Find("CurrentlyPlayingContents/AudioTitle").GetComponent<Text>();
            _songTimePosition = transform.Find("CurrentlyPlayingContents/Timeline/Slider/Text (Legacy)").GetComponent<Text>();
            _timelineSlider = (PhoneSlider)GetObject("Timeline");

            SetVolumeMultiplier(Configuration.MusicMultiplier.Value);
            SetSpatialBlend(Configuration.UseSpatialBlend.Value);

            EvaluateMusicList();
            _inDownloadView = _musicList.Count == 0;
        }

        public void Update()
        {
            if (MusicSource && MusicSource.isPlaying)
            {
                float progress = MusicSource.time / MusicSource.clip.length;

                _timelineSlider.Parameters.z = progress * 100;
                _timelineSlider.UpdatePosition();

                _songTimePosition.text = TimeSpan.FromSeconds(MusicSource.time).ToString(@"mm\:ss");
            }
        }

        public override void AppOpened()
        {
            EvaluateMusicList();
            RefreshSuitableContainer();
        }

        private void EvaluateMusicList()
        {
            var current = _musicList;

            _musicList = Directory.GetFiles(PhoneHandler.Instance.MusicPath).Where(file => file.ToLower().EndsWith(".mp3") || file.ToLower().EndsWith(".ogg") || file.ToLower().EndsWith(".wav")).ToList();

            var missingItems = current.Where(str => !_musicList.Contains(str) && _musicComparison.ContainsKey(str));

            foreach (var str in missingItems)
            {
                _musicComparison.Remove(str);
            }
        }

        /// <summary>
        /// Refreshes the current / suitable container, if we are viewing the download view, the downloadable container will be refreshed, vice versa.
        /// </summary>
        private void RefreshSuitableContainer()
        {
            transform.Find("MusicDownloadContainer").gameObject.SetActive(_inDownloadView);
            transform.Find("MusicPlayerContainer").gameObject.SetActive(!_inDownloadView);

            if (_inDownloadView)
            {
                RefreshDownloadables();
            }
            else
            {
                RefreshSongList();
            }
        }

        private void RefreshDownloadables()
        {
            _currentPage = MathEx.Wrap(_currentPage, 0, Mathf.CeilToInt(PhoneHandler.Instance.Data.songs.Length / 3f));
            Song[] songs = { PhoneHandler.Instance.Data.songs.ElementAtOrDefault((_currentPage * 3) + 0), PhoneHandler.Instance.Data.songs.ElementAtOrDefault((_currentPage * 3) + 1), PhoneHandler.Instance.Data.songs.ElementAtOrDefault((_currentPage * 3) + 2) };
            Transform table = transform.Find("MusicDownloadContainer/Table");
            _streambleMusicComponents.Clear();

            for (int i = 0; i < table.childCount; i++)
            {
                Transform item = table.GetChild(i);
                Song song = songs.ElementAtOrDefault(i);

                bool noSong = song == null || song == default;
                item.gameObject.SetActive(!noSong);

                if (noSong) continue;

                if (item.TryGetComponent(out StreamableMusicComponent component))
                {
                    Destroy(component);
                }

                component = item.AddComponent<StreamableMusicComponent>();
                component.song = song;
                component.coverArt = item.Find("AlbumCover").GetComponent<RawImage>();
                component.nameText = item.Find("AudioTitle").GetComponent<Text>();
                component.buttonText = item.FindChildRecursive("download").GetComponentInChildren<Text>();
                _streambleMusicComponents.Add(component);
            }
        }

        private void RefreshSongList()
        {
            Transform table = transform.Find("MusicPlayerContainer/Table");
            Transform noSongs = transform.Find("MusicPlayerContainer/NoMusic");

            table.gameObject.SetActive(_musicList.Count > 0);
            noSongs.gameObject.SetActive(_musicList.Count == 0);

            if (_musicList.Count == 0) return;

            _currentPage = MathEx.Wrap(_currentPage, 0, Mathf.CeilToInt(_musicList.Count / 3f));
            string[] songs = { _musicList.ElementAtOrDefault((_currentPage * 3) + 0), _musicList.ElementAtOrDefault((_currentPage * 3) + 1), _musicList.ElementAtOrDefault((_currentPage * 3) + 2) };

            for (int i = 0; i < table.childCount; i++)
            {
                Transform item = table.GetChild(i);
                string song = songs.ElementAtOrDefault(i);

                bool noSong = song == null || song == default;
                item.gameObject.SetActive(!noSong);

                if (noSong) continue;

                async void HandleButton()
                {
                    if (!_musicComparison.ContainsKey(song))
                    {
                        await LoadTrack(song);
                    }

                    item.Find("AudioTitle").GetComponent<Text>().text = Path.GetFileNameWithoutExtension(song);
                    item.Find("Format Label").GetComponent<Text>().text = Path.GetExtension(song).Replace(".", "").ToUpper();
                    item.Find("Length Label").GetComponent<Text>().text = TimeSpan.FromSeconds(_musicComparison[song].length).ToString(@"mm\:ss");
                }

                HandleButton();
            }
        }

        public async override void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand)
        {
            if (phoneUIObject.name.StartsWith("download"))
            {
                int index = int.Parse(phoneUIObject.name[phoneUIObject.name.Length - 1].ToString()) - 1;
                var component = _streambleMusicComponents.ElementAtOrDefault(index);

                if (!component) return;

                component.UpdateText(component.song.currentState != Song.DownloadState.Downloaded ? Song.DownloadState.Awaiting : Song.DownloadState.Downloaded);
                var state = await component.song.Download();

                if (state == Song.DownloadState.None) return;

                EvaluateMusicList();

                component.UpdateText(state);
                PlaySound(state == Song.DownloadState.Downloaded ? "RequestSuccess" : "RequestDenied");

                return;
            }

            switch (phoneUIObject.name)
            {
                case "tab library":
                    _inDownloadView = false;
                    _currentPage = 0;
                    RefreshSuitableContainer();
                    break;
                case "tag catalog":
                    _inDownloadView = true;
                    _currentPage = 0;
                    RefreshSuitableContainer();
                    break;
                case "pageleft":
                    _currentPage--;
                    RefreshSuitableContainer();
                    break;
                case "pageright":
                    _currentPage++;
                    RefreshSuitableContainer();
                    break;
            }


            /*
            switch (phoneUIObject.name)
            {
                case "Music Last":
                    _currentMusic--;
                    RefreshApp();
                    break;
                case "Music Next":
                    _currentMusic++;
                    RefreshApp();
                    break;
                case "Music Toggle":

                    if (!_isLoadingMusic && (MusicSource.clip == null || MusicSource.clip.name != Path.GetFileName(_musicList[_currentMusic])))
                    {
                        _isLoadingMusic = true;
                        SetTrack(_musicList[_currentMusic]);
                        return;
                    }

                    if (MusicSource.isPlaying)
                    {
                        _wasPlaying = false;
                        MusicSource.Pause();
                    }
                    else
                    {
                        float position = MusicSource.time;
                        _wasPlaying = true;
                        MusicSource.Play();
                        MusicSource.time = position;
                    }
                    break;
            }
            */
        }

        public void RefreshApp()
        {
            try
            {
                /*
                _songMissing.gameObject.SetActive(_musicList.Count == 0);
                if (_musicList.Count == 0)
                {
                    _songTitle.text = "";
                    _timelineSlider.gameObject.SetActive(false);
                    return;
                }

                _currentMusic = MathEx.Wrap(_currentMusic, 0, _musicList.Count);
                _songTitle.text = Path.GetFileNameWithoutExtension(_musicList[_currentMusic]);
                _timelineSlider.gameObject.SetActive(true);

                if (_timelineSlider.Value != 0 && MusicSource.clip != null && MusicSource.clip.name != Path.GetFileNameWithoutExtension(_musicList[_currentMusic]))
                {
                    _timelineSlider.Value = 0;
                    _timelineSlider.UpdatePosition();
                    _songTimePosition.text = "00:00";
                }
                */

            }
            catch (Exception ex)
            {
                Logging.Error($"Error when loading music: {ex}");
            }
        }

        public async Task LoadTrack(string path)
        {
            if (_musicComparison.ContainsKey(path)) return;

            AudioType audioType = AudioType.UNKNOWN;

            if (path.EndsWith(".mp3"))
            {
                audioType = AudioType.MPEG;
            }
            else if (path.EndsWith(".ogg"))
            {
                audioType = AudioType.OGGVORBIS;
            }
            else if (path.EndsWith(".wav"))
            {
                audioType = AudioType.WAV;
            }

            var downloadHandler = new DownloadHandlerAudioClip(path, audioType);
            var webRequest = new UnityWebRequest(path, "GET", downloadHandler, null);

            await YieldUtils.Yield(webRequest);

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                _musicComparison.Add(path, downloadHandler.audioClip);
            }
        }

        public async void SetTrack(string path)
        {
            AudioType audioType = AudioType.UNKNOWN;

            if (path.EndsWith(".mp3"))
            {
                audioType = AudioType.MPEG;
            }
            else if (path.EndsWith(".ogg"))
            {
                audioType = AudioType.OGGVORBIS;
            }
            else if (path.EndsWith(".wav"))
            {
                audioType = AudioType.WAV;
            }

            var downloadHandler = new DownloadHandlerAudioClip(path, audioType);
            var webRequest = new UnityWebRequest(path, "GET", downloadHandler, null);

            await YieldUtils.Yield(webRequest);

            _isLoadingMusic = false;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                downloadHandler.audioClip.name = Path.GetFileName(path);
                MusicSource.clip = downloadHandler.audioClip;

                if (_wasPlaying)
                {
                    MusicSource.Play();
                    MusicSource.time = 0f;
                }
            }
            else
            {
                Logging.Error(webRequest.downloadHandler.text);
            }
        }

        public void SetVolumeMultiplier(float multiplier) => MusicSource.volume = 0.1f * multiplier;

        public void SetSpatialBlend(bool useSpatialBlend) => MusicSource.spatialBlend = useSpatialBlend ? 1f : 0f;

        public class StreamableMusicComponent : MonoBehaviour
        {
            public Song song;

            public Text nameText, buttonText;
            public RawImage coverArt;

            public async void Start()
            {
                nameText.text = song.title;
                coverArt.texture = null;

                if (!song.IsDownloaded && song.currentState == Song.DownloadState.Downloaded)
                {
                    song.currentState = Song.DownloadState.None;
                }

                UpdateText(song.currentState);

                if (string.IsNullOrEmpty(song.coverUrl) || string.IsNullOrWhiteSpace(song.coverUrl)) return;

                UnityWebRequest request = UnityWebRequestTexture.GetTexture(song.coverUrl);
                await YieldUtils.Yield(request);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    coverArt.texture = tex;
                }
            }

            public void UpdateText(Song.DownloadState dlState)
            {
                switch (dlState)
                {
                    case Song.DownloadState.None:
                        buttonText.text = "Download";
                        break;
                    case Song.DownloadState.Awaiting:
                        buttonText.text = "<color=grey>Progressing..</color>";
                        break;
                    case Song.DownloadState.Downloaded:
                        buttonText.text = $"<color=green>Saved {song.title}!</color>";
                        break;
                    case Song.DownloadState.Failed:
                        buttonText.text = "<color=red>Music could not be saved.</color>";
                        break;
                }
            }

            public void Download()
            {

            }
        }
    }
}

using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Models;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.Apps
{
    public class GalleryApp : PhoneApp
    {
        public override string AppId => "Gallery";

        public Dictionary<string, string> _photoComparison;
        public int _currentPhoto;

        public Image _galleryPhoto;
        public Text _galleryText, _gallerySelection, _galleryWarning;
        private GameObject _deleteButton, _uploadButton;

        public List<Photo> RelativePhotos = [];

        public override void Initialize()
        {
            _galleryPhoto = transform.Find("Preview").GetComponent<Image>();
            _galleryText = transform.Find("Photo Label").GetComponent<Text>();
            _gallerySelection = transform.Find("Preview/Text (Legacy)").GetComponent<Text>();
            _galleryWarning = transform.Find("Header (1)").GetComponent<Text>();
            _deleteButton = transform.Find("Photos Trash").gameObject;
            _uploadButton = transform.Find("Photos Post").gameObject;
        }

        public override void AppOpened()
        {
            bool initialized = _photoComparison != null;

            _photoComparison = Directory.GetFiles(PhoneHandler.Instance.PhotosPath).Where(file => file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg") || file.EndsWith(".gif")).ToDictionary(photo => photo, photo => Path.GetFileName((photo)));

            if (!initialized)
            {
                _currentPhoto = _photoComparison.Count - 1;
            }

            RefreshApp();
        }

        public override void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand)
        {
            switch (phoneUIObject.name)
            {
                case "Photos Last":

                    _currentPhoto--;

                    RefreshApp();

                    break;

                case "Photos Next":

                    _currentPhoto++;

                    RefreshApp();

                    break;

                case "Photos Trash":

                    if (_photoComparison.Count == 0)
                    {
                        return;
                    }

                    var path = Path.Combine(Path.Combine(PhoneHandler.Instance.PhotosPath, _photoComparison.ElementAt(_currentPhoto).Value));

                    try
                    {
                        if (!FileEx.RecycleFile(path))
                            File.Delete(path);
                    }
                    catch
                    {
                        File.Delete(path);
                    }

                    _photoComparison.Remove(_photoComparison.ElementAt(_currentPhoto).Key);
                    _currentPhoto--;

                    PlaySound("Delete", 0.66f);

                    RefreshApp();

                    break;

                case "Photos Post":

                    if (_photoComparison.Count == 0)
                    {
                        return;
                    }

                    var fileName = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key);
                    var photo = RelativePhotos.FirstOrDefault(photo => photo.Name == fileName);

                    if (photo == null || photo.UploadState > 0)
                    {
                        return;
                    }

                    photo.UploadState = 1;

                    SendWebhook(photo.Summary, photo.Bytes);

                    break;
            }
        }

        public void RefreshApp()
        {
            try
            {
                _galleryWarning.enabled = _photoComparison.Count == 0;
                if (_photoComparison.Count == 0)
                {
                    _galleryText.text = "";
                    _galleryPhoto.enabled = false;
                    _gallerySelection.text = "";
                    _deleteButton.SetActive(false);
                    _uploadButton.SetActive(true);
                    return;
                }

                _currentPhoto = MathEx.Wrap(_currentPhoto, 0, _photoComparison.Count);

                _deleteButton.SetActive(true);

                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(PhoneHandler.Instance.PhotosPath, _photoComparison.ElementAt(_currentPhoto).Value)));
                tex.Apply();
                tex.filterMode = FilterMode.Point;

                var fileName = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key);

                _uploadButton.SetActive(RelativePhotos.Count > 0 && RelativePhotos.Any(photo => photo.Name == fileName));
                _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.black;

                _galleryText.text = $"{_currentPhoto + 1}/{_photoComparison.Count}";
                _galleryPhoto.enabled = true;
                _galleryPhoto.material.mainTexture = tex;
                _gallerySelection.text = fileName;

                Photo relativePhoto = RelativePhotos.Any() ? RelativePhotos.FirstOrDefault(photo => photo.Name == fileName) : null;

                if (relativePhoto == null || string.IsNullOrEmpty(Configuration.WebhookEndpoint.Value) || string.IsNullOrWhiteSpace(Configuration.WebhookEndpoint.Value))
                {
                    _uploadButton.SetActive(false);
                    return;
                }

                _uploadButton.SetActive(true);
                _uploadButton.transform.Find("Image").GetComponent<Image>().color = relativePhoto.UploadState switch
                {
                    0 => Color.black,
                    1 => Color.grey,
                    2 => Color.green,
                    _ => Color.black
                };
            }
            catch (Exception ex)
            {
                Logging.Error($"Error when loading photos: {ex}");
            }
        }

        private async void SendWebhook(string message, byte[] image)
        {
            var fileName = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key);

            _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.grey;

            var form = new WWWForm();

            form.AddField("username", $"MonkeGram - {PhotonNetwork.LocalPlayer.GetName()}");
            form.AddField("content", $"{message}:");
            form.AddBinaryData("file", image, "image.png", "image/png");

            UnityWebRequest webRequest = UnityWebRequest.Post(Configuration.WebhookEndpoint.Value, form);
            await YieldUtils.Yield(webRequest);

            Photo photo = RelativePhotos.First(photo => photo.Name == fileName);
            bool isRelevant = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key) == fileName;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Logging.Log("A photo has been successfully uploaded to the website");

                PlaySound("RequestSuccess");

                photo.UploadState = 2;

                if (!isRelevant) return;
                _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.green;
            }
            else
            {
                Logging.Error($"A photo was unable to be uploaded to the website: {webRequest.error}");
                Logging.Warning($"Server response: {webRequest.downloadHandler.text}");

                PlaySound("RequestDenied");

                photo.UploadState = 0;

                if (!isRelevant) return;
                _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.red;
            }
        }
    }
}

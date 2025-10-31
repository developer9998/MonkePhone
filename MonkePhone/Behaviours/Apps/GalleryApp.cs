using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkePhone.Behaviours.UI;
using MonkePhone.Extensions;
using MonkePhone.Models;
using MonkePhone.Tools;
using MonkePhone.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MonkePhone.Behaviours.Apps;

public class GalleryApp : PhoneApp
{
    public int _currentPhoto;

    public  Image      _galleryPhoto;
    public  Text       _galleryText,  _gallerySelection, _galleryWarning;
    private GameObject _deleteButton, _uploadButton,     thumbnailButton;

    public Dictionary<string, string> _photoComparison;

    public          List<Photo> RelativePhotos = [];
    public override string      AppId => "Gallery";

    public override void Initialize()
    {
        _galleryPhoto     = transform.Find("Preview").GetComponent<Image>();
        _galleryText      = transform.Find("Photo Label").GetComponent<Text>();
        _gallerySelection = transform.Find("Preview/Text (Legacy)").GetComponent<Text>();
        _galleryWarning   = transform.Find("Header (1)").GetComponent<Text>();
        _deleteButton     = transform.Find("Photos Trash").gameObject;
        _uploadButton     = transform.Find("Photos Post").gameObject;
        thumbnailButton   = transform.Find("SetThumbnail").gameObject;
    }

    public override void AppOpened()
    {
        bool initialized = _photoComparison != null;

        _photoComparison = Directory.GetFiles(PhoneManager.Instance.PhotosPath)
                                    .Where(file => file.EndsWith(".png")  || file.EndsWith(".jpg") ||
                                                   file.EndsWith(".jpeg") || file.EndsWith(".gif"))
                                    .ToDictionary(photo => photo, photo => Path.GetFileName(photo));

        if (!initialized)
            _currentPhoto = _photoComparison.Count - 1;

        RefreshApp();
    }

    public override void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand)
    {
        string fileName;

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
                    return;

                string path = Path.Combine(PhoneManager.Instance.PhotosPath,
                        _photoComparison.ElementAt(_currentPhoto).Value);

                try
                {
                    if (!path.RecycleFile()) File.Delete(path);
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
                    return;

                fileName = _photoComparison.ElementAt(_currentPhoto).Value;
                Photo photo = RelativePhotos.FirstOrDefault(photo => photo.Name == fileName);

                if (photo == null || photo.UploadState > 0)
                    return;

                photo.UploadState = 1;

                SendWebhook(photo.Summary, photo.Name, photo.Bytes);

                break;

            case "SetThumbnail":

                if (_photoComparison.Count == 0) return;

                fileName = _photoComparison.ElementAt(_currentPhoto).Value;
                if (Configuration.WallpaperName.Value == fileName)
                    PhoneManager.Instance.ApplyWallpaper(true,   string.Empty);
                else PhoneManager.Instance.ApplyWallpaper(false, fileName);

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
                _galleryText.text      = "";
                _galleryPhoto.enabled  = false;
                _gallerySelection.text = "";
                //_deleteButton.SetActive(false);
                _uploadButton.SetActive(false);
                thumbnailButton.SetActive(false);

                return;
            }

            _currentPhoto = MathEx.Wrap(_currentPhoto, 0, _photoComparison.Count);

            //_deleteButton.SetActive(true);
            thumbnailButton.SetActive(true);

            Texture2D tex = new(2, 2);
            tex.LoadImage(File.ReadAllBytes(Path.Combine(PhoneManager.Instance.PhotosPath,
                    _photoComparison.ElementAt(_currentPhoto).Value)));

            tex.Apply();
            tex.filterMode = FilterMode.Point;

            string fileName = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key);

            _uploadButton.SetActive(RelativePhotos.Count > 0 && RelativePhotos.Any(photo => photo.Name == fileName));
            _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.black;

            _galleryText.text                  = $"{_currentPhoto + 1}/{_photoComparison.Count}";
            _galleryPhoto.enabled              = true;
            _galleryPhoto.material.mainTexture = tex;
            _gallerySelection.text             = fileName;

            Photo relativePhoto = RelativePhotos.Any()
                                          ? RelativePhotos.FirstOrDefault(photo => photo.Name == fileName)
                                          : null;

            bool validKey = !string.IsNullOrEmpty(Configuration.UploadUrl.Value) &&
                            (Configuration.UploadMethod.Value == Configuration.EUploadMethod.Webhook ||
                             !string.IsNullOrEmpty(Configuration.UploadKey.Value));

            if (relativePhoto == null || !validKey)
            {
                _uploadButton.SetActive(false);

                return;
            }

            _uploadButton.SetActive(true);
            _uploadButton.transform.Find("Image").GetComponent<Image>().color = relativePhoto.UploadState switch
                {
                        0     => Color.black,
                        1     => Color.grey,
                        2     => Color.green,
                        var _ => Color.black,
                };
        }
        catch (Exception ex)
        {
            Logging.Error($"Error when loading photos: {ex}");
        }
    }

    public async void SendWebhook(string message, string image_name, byte[] image)
    {
        string fileName = image_name;

        _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.grey;

        WWWForm         form = new();
        UnityWebRequest webRequest;
        Photo           photo;
        bool            isRelevant;

        switch (Configuration.UploadMethod.Value)
        {
            case Configuration.EUploadMethod.Webhook:
                form.AddField("username", Constants.Name);
                form.AddField("content",  message);
                form.AddBinaryData("file", image, "image.png", "image/png");

                webRequest = UnityWebRequest.Post(Configuration.UploadUrl.Value, form);
                await YieldUtils.Yield(webRequest);

                photo      = RelativePhotos.First(photo => photo.Name == fileName);
                isRelevant = Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key) == fileName;

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

                break;

            case Configuration.EUploadMethod.CustomServer:
                form.AddField("username", Constants.Name);
                form.AddField("content",  message);
                form.AddBinaryData("file1", image, image_name, "image/png");

                webRequest = UnityWebRequest.Post(Configuration.UploadUrl.Value, form);
                webRequest.SetRequestHeader("Authorization", Configuration.UploadKey.Value);

                await YieldUtils.Yield(webRequest);

                photo = RelativePhotos.First(photo => photo.Name == fileName);
                isRelevant = _photoComparison                                                != null &&
                             Path.GetFileName(_photoComparison.ElementAt(_currentPhoto).Key) == fileName;

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Logging.Log("A photo has been successfully uploaded to the website");

                    PlaySound("RequestSuccess");

                    photo.UploadState = 2;

                    if (!isRelevant) return;
                    _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.green;
                    _gallerySelection.text                                            = "Photo uploaded!";
                }
                else
                {
                    Logging.Error(
                            $"A photo was unable to be uploaded to the website: {webRequest.error} ({webRequest.responseCode})");

                    Logging.Warning($"Server response: {webRequest.downloadHandler.text}");

                    PlaySound("RequestDenied");

                    photo.UploadState = 0;

                    if (!isRelevant) return;
                    _uploadButton.transform.Find("Image").GetComponent<Image>().color = Color.red;
                    _gallerySelection.text =
                            $"Error {webRequest.responseCode}{(!string.IsNullOrEmpty(webRequest.error) && !string.IsNullOrWhiteSpace(webRequest.error) ? $": {webRequest.error}" : string.Empty)}";
                }

                break;
        }
    }
}
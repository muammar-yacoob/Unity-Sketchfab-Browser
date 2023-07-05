using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace SparkGames.SketchfabBrowser.Editor
{
    public class ModelDownloader : IModelDownloader
    {
        private string apiToken;
        private string downloadsFullPath;
        private string downloadsRelativePath;
        
        private Model currentModel;
        private const string SketchfabTokenKey = "SketchfabTokenKey";
        private const string SketchfabDownloadsPath = "SketchfabDownloadsPath";
        public string ApiToken => apiToken;

        private static ModelDownloader instance;
        public static ModelDownloader Instance => instance ?? new ModelDownloader();

        ModelDownloader()
        {
            apiToken = PlayerPrefs.GetString(SketchfabTokenKey, "your-sketchfab-api-token"); //https://sketchfab.com/settings/password
            downloadsFullPath = PlayerPrefs.GetString(SketchfabDownloadsPath, Path.Combine(Application.dataPath,"Sketchfab Models"));
            downloadsRelativePath = downloadsFullPath.Replace(Application.dataPath, "Assets");
            Debug.Log($"Download path set to: {downloadsFullPath}");
        }

        public void SetToken(string apiToken)
        {
            this.apiToken = apiToken;
            PlayerPrefs.SetString(SketchfabTokenKey, apiToken);
            PlayerPrefs.Save();
        }

        public async UniTask DownloadModel(Model model, Action<float> onDownloadProgress = null)
        {
            model.IsDownloading = true;
            currentModel = await DescribeModel(model.uid);
            Debug.Log($"Downloading {currentModel.name}...");
        
            string downloadRequest = $"https://api.sketchfab.com/v3/models/{model.uid}/download";
            using var request = UnityWebRequest.Get(downloadRequest);
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401) throw new UnauthorizedAccessException("Invalid API token");

                var downloadInfo = JsonUtility.FromJson<ModelDownloadInfo>(request.downloadHandler.text);
                await DownloadModelFromUrl(downloadInfo.gltf.url, onDownloadProgress);
                model.IsDownloaded = true;
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
            model.IsDownloading = false;
        }

        private async UniTask<Model> DescribeModel(string modelId)
        {
            string downloadUrl = $"https://api.sketchfab.com/v3/models/{modelId}";
            using var request = UnityWebRequest.Get(downloadUrl);
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401) throw new UnauthorizedAccessException("Invalid API token");

                var model = JsonUtility.FromJson<Model>(request.downloadHandler.text);
                return model;
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }

            return default;
        }
    
        private async UniTask DownloadModelFromUrl(string url, Action<float> onDownloadProgress = null)
        {
            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
    
            while (!operation.isDone)
            {
                onDownloadProgress?.Invoke(request.downloadProgress);
                await UniTask.DelayFrame(1);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] modelBytes = request.downloadHandler.data;

                string fileName = currentModel.name + $".zip";
                
                Directory.CreateDirectory(downloadsFullPath);
                string savePath = Path.Combine(downloadsFullPath, fileName);
                File.WriteAllBytes(savePath, modelBytes);
                await Unzip(savePath);
            }
            else
            {
                Debug.LogError("Download failed. " + request.error);
            }
        }


        private async Task Unzip(string savePath, Action<float> onUnpackProgress = null)
        {
            string unpackPath = $"{downloadsRelativePath}/{currentModel.name}";
            if (Directory.Exists(unpackPath)) Directory.Delete(unpackPath, true);
            Directory.CreateDirectory(unpackPath);

            try
            {
                await UniTask.RunOnThreadPool(() => { ZipFile.ExtractToDirectory(savePath, unpackPath); });
                //AssetDatabase.Refresh();
            }
            catch (IOException e)
            {
                Debug.LogError("Failed to unzip the model: " + e.Message);
            }

            File.Delete(savePath);
            Debug.Log($"Model is downloaded to: " + savePath.Replace(".zip", "").Replace("\\", "/"));
            
            AssetDatabase.Refresh();

            var targetFilePath = Directory.EnumerateFiles(unpackPath).FirstOrDefault();
            if (targetFilePath != null)
            {
                var targetObject = AssetDatabase.LoadAssetAtPath<Object>(targetFilePath);
                if (targetObject != null)
                {
                    Selection.activeObject = targetObject;
                    EditorGUIUtility.PingObject(targetObject);
                }
            }
        }
    }
}


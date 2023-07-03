using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Package.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

//[Injectable]

namespace SparkGames.Sketchfab.Package.Editor
{
    [CreateAssetMenu(fileName = "ModelDownloader", menuName = "Sketchfab Browser/ModelDownloader", order = 1)]
    public class ModelDownloader : ScriptableObject, IModelDownloader
    {
        [NonSerialized] private string apiToken;
        [NonSerialized] private string downloadPath;
        
        private Model currentModel;
        private const string SketchfabTokenKey = "SketchfabTokenKey";

        private static ModelDownloader instance;
        public static ModelDownloader Instance => instance ??= Resources.Load<ModelDownloader>("ModelDownloader");
        public string ApiToken => apiToken;

        private void OnEnable()
        {
            apiToken = PlayerPrefs.GetString(SketchfabTokenKey, "your-sketchfab-api-token"); //https://sketchfab.com/settings/password
            downloadPath = Application.dataPath;
        }

        public void SetToken(string apiToken)
        {
            this.apiToken = apiToken;
            PlayerPrefs.SetString(SketchfabTokenKey, apiToken);
            PlayerPrefs.Save();
        }

        public void SetDownloadPath(string downloadPath) => this.downloadPath = downloadPath;

        public async UniTaskVoid DownloadModel(string modelId, Action<float> onDownloadProgress = null)
        {
            currentModel = await DescribeModel(modelId);
            Debug.Log($"Downloading {currentModel.name}...");
        
            string downloadRequest = $"https://api.sketchfab.com/v3/models/{modelId}/download";
            using var request = UnityWebRequest.Get(downloadRequest);
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401) throw new UnauthorizedAccessException("Invalid API token");

                var downloadInfo = JsonUtility.FromJson<ModelDownloadInfo>(request.downloadHandler.text);
                await DownloadModelFromUrl(downloadInfo.FBXFile.url, onDownloadProgress);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
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
                Directory.CreateDirectory(downloadPath);
                string savePath = Path.Combine(downloadPath, fileName);
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
            string unpackPath = $"{downloadPath}/{currentModel.name}";
            if (Directory.Exists(unpackPath)) Directory.Delete(unpackPath, true);
            Directory.CreateDirectory(unpackPath);

            try
            {
                await UniTask.RunOnThreadPool(() => { ZipFile.ExtractToDirectory(savePath, unpackPath); });
                AssetDatabase.Refresh();
            }
            catch (IOException e)
            {
                Debug.LogError("Failed to unzip the model: " + e.Message);
            }

            File.Delete(savePath);
            Debug.Log($"Model is downloaded to: " + savePath.Replace(".zip", ""));

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


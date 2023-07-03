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

namespace SparkGames.Sketchfab.Package.Editor
{
    //[CreateAssetMenu(fileName = "ModelDownloader", menuName = "Sketchfab Browser/ModelDownloader", order = 1)]
    public class ModelDownloader : ScriptableObject, IModelDownloader
    {
        [NonSerialized] private string apiToken;
        [NonSerialized] private string absoluteDownloadPath;
        private string relativeDownloadPath;
        
        private Model currentModel;
        private const string SketchfabTokenKey = "SketchfabTokenKey";

        private static ModelDownloader instance;
        public static ModelDownloader Instance => instance ??= Resources.Load<ModelDownloader>("ModelDownloader");
        public string ApiToken => apiToken;

        private void OnEnable()
        {
            apiToken = PlayerPrefs.GetString(SketchfabTokenKey, "your-sketchfab-api-token"); //https://sketchfab.com/settings/password
            absoluteDownloadPath ??= Application.dataPath + "/Sketchfab Models";
            relativeDownloadPath = absoluteDownloadPath.Substring(absoluteDownloadPath.IndexOf("Assets"));

        }

        public void SetToken(string apiToken)
        {
            this.apiToken = apiToken;
            PlayerPrefs.SetString(SketchfabTokenKey, apiToken);
            PlayerPrefs.Save();
        }

        public void SetDownloadPath(string downloadPath) => this.absoluteDownloadPath = downloadPath;

        public async UniTaskVoid DownloadModel(Model model, Action<float> onDownloadProgress = null)
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
                Directory.CreateDirectory(absoluteDownloadPath);
                string savePath = Path.Combine(absoluteDownloadPath, fileName);
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
            string unpackPath = $"{relativeDownloadPath}/{currentModel.name}";
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
            Debug.Log($"Model is downloaded to: " + savePath.Replace(".zip", ""));
            
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


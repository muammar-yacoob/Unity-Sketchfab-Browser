using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class SketchfabBrowser : EditorWindow
{
    private string modelId = "564e02a97528499388ca00d3c6bdb044";
    private string apiToken = "your-sketchfab-api-token"; //https://sketchfab.com/settings/password
    private bool connected;
    private string accountName;
    private ModelInfo currentModelInfo;
    public string uri; // URL of the model's page
    private Texture2D windowIcon;
    private GUIStyle hyperlinkStyle;
    private Texture2D thumb;
    private string defaultSavePath = "Assets/SketchfabModels/";

    [MenuItem("Assets/Sketchfab Browser")]
    public static void ShowWindow()
    {
        SketchfabBrowser window = GetWindow<SketchfabBrowser>();
        window.titleContent = new GUIContent("Sketchfab Browser", window.windowIcon);
    }

    private void OnEnable()
    {
        windowIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Package/Editor/res/sketchfab-logo.png", typeof(Texture2D));
        titleContent.image = windowIcon;
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label($"Unavailable in play mode.", EditorStyles.boldLabel);
            return;
        }
        if (hyperlinkStyle == null)
        {
            hyperlinkStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } };
        }
        if (!connected)
        {
            apiToken = EditorGUILayout.TextField("API Token", apiToken);
            
            if (GUILayout.Button("Get your API Token",hyperlinkStyle))
            {
                Application.OpenURL("https://sketchfab.com/settings/password");
            }
            if (string.IsNullOrEmpty(apiToken) || apiToken.Length != 32)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Connect to Sketchfab"))
            {
                ConnectToSketchfab().Forget();
            }
            GUI.enabled = true;
        }
        else
        { 
            GUILayout.Space(20);
            modelId = EditorGUILayout.TextField("Model ID", modelId);

            if (string.IsNullOrEmpty(modelId) || modelId.Length < 13)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Fetch Model"))
            {
                FetchAndDownloadModel().Forget();
            }

            GUI.enabled = true;
            
            if (currentModelInfo != null)
            {
                EditorGUILayout.HelpBox($"Model Name: {currentModelInfo.name}\nDescription: {currentModelInfo.description}\nDownloadable: {(currentModelInfo.isDownloadable ? "Yes" : "No")}", MessageType.Info);
                
                if (thumb == null)
                {
                    GetThumbnail().Forget();
                }
                else
                {
                    GUILayout.Label(thumb, GUILayout.Width(256));
                }
                
                if (GUILayout.Button("Goto Model",hyperlinkStyle))
                {
                    Application.OpenURL(currentModelInfo.viewerUrl);
                }
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Connected as {accountName}", EditorStyles.boldLabel);
        }
    }

    private async UniTaskVoid GetThumbnail()
    {
        // if (string.IsNullOrEmpty(currentModelInfo.thumbnails.images[0].url))
        // {
        //     thumb = null;
        //     return;
        // }

        thumb = await DownloadImage(currentModelInfo.thumbnails.images[3].url);
         Repaint();
    }

    async UniTaskVoid ConnectToSketchfab()
    {
        await Connect(apiToken);
        currentModelInfo = null;
    }

    async UniTask Connect(string token)
    {
        string url = "https://api.sketchfab.com/v3/me";
        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Token {token}");
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                connected = true;
                AccountInfo accountInfo = JsonUtility.FromJson<AccountInfo>(request.downloadHandler.text);
                accountName = accountInfo.username;
                //avatar = accountInfo.avatar.images[1].url;
            }
            else
            {
                Debug.LogError("Failed to connect to Sketchfab: " + request.error);
            }
        }
    }

    async UniTaskVoid FetchAndDownloadModel()
    {
        await FetchModelInfo();
        await DownloadModel();
    }

    async UniTask FetchModelInfo()
    {
        string modelInfoUrl = $"https://api.sketchfab.com/v3/models/{modelId}";
        using (var request = UnityWebRequest.Get(modelInfoUrl))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                currentModelInfo = JsonUtility.FromJson<ModelInfo>(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Failed to fetch model metadata: " + request.error);
            }
        }
    }

    async UniTask<Texture2D> DownloadImage(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerTexture.GetContent(request);
                }
                else
                {
                    Debug.LogError("Failed to download image: " + request.error);
                    return null;
                }
            }
        }
        else
        {
            Debug.LogError("Thumbnail URL is empty or null.");
            return null;
        }
    }

    async UniTask DownloadModel()
    {
        string downloadUrl = $"https://api.sketchfab.com/v3/models/{modelId}/download";
        using (var request = UnityWebRequest.Get(downloadUrl))
        {
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else if (request.result == UnityWebRequest.Result.Success)
            {
                if(request.responseCode == 401)
                {
                    Debug.LogError("Unauthorized: Invalid API token.");
                }
                else
                {
                    ModelDownloadInfo downloadInfo = JsonUtility.FromJson<ModelDownloadInfo>(request.downloadHandler.text);
                    
                    
                    await DownloadModelFromUrl(downloadInfo.gltf.url, ModelFormatExtension.gltf);
                    //await DownloadModelFromUrl(downloadInfo.source.url, ModelFormatExtension.fbx);
                }
            }
        }
    }


    async UniTask DownloadModelFromUrl(string url, ModelFormatExtension ext)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] modelBytes = request.downloadHandler.data;

                string fileName = currentModelInfo.name + $".zip"; 
                //string savePath = EditorUtility.SaveFilePanel("Save Model", "", modelId, "");
                Directory.CreateDirectory(defaultSavePath);
                string savePath = Path.Combine(defaultSavePath, fileName);

                File.WriteAllBytes(savePath, modelBytes);

                Unzip(savePath);
            }
            else
            {
                Debug.LogError("Failed to download the model: " + request.error);
            }
        }
    }

    private void Unzip(string savePath)
    {
        string unpackPath = $"{defaultSavePath}/{currentModelInfo.name}";
        if (Directory.Exists(unpackPath))
        {
            Directory.Delete(unpackPath, true);
        }

        Directory.CreateDirectory(unpackPath);

        try
        {
            ZipFile.ExtractToDirectory(savePath, unpackPath);
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to unzip the model: " + e.Message);
        }

        File.Delete(savePath);
        Debug.Log("Model downloaded and saved to: " + defaultSavePath);
    }


    [System.Serializable]
    private class ModelInfo
    {
        public string name;
        public string description;
        public string uri; // URL of the model's page
        public Thumbnails thumbnails; // URLs and images of the model's thumbnails
        public bool isDownloadable;
        public string viewerUrl;
    }

    [System.Serializable]
    public class Thumbnails
    {
        public Thumbnail[] images;

        [System.Serializable]
        public class Thumbnail
        {
            public string url;
        }
    }

    [System.Serializable]
    private class AccountInfo
    {
        public string username;
    }
    
    [System.Serializable]
    public class ModelDownloadInfo
    {
        public ModelFormat gltf;
        [FormerlySerializedAs("fbx")] public ModelFormat source;

        [System.Serializable]
        public class ModelFormat
        {
            public string url;
            public int size;
            public int expires;
        }
    }

    public enum ModelFormatExtension
    {
        gltf,
        fbx
    }
}

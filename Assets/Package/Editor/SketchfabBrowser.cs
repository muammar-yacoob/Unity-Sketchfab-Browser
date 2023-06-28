using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SketchfabBrowser : EditorWindow
{
    private string modelId = "564e02a97528499388ca00d3c6bdb044";
    private string apiToken; 
    private bool connected;
    private string accountName;
    public Model CurrentModel = new Model();
    private Texture2D windowIcon;
    private GUIStyle hyperlinkStyle;
    private Texture2D thumb;
    private string defaultSavePath = "Assets/SketchfabModels/";
    private bool moreInfo;
    private PageModels currentPageModels;
    private string searchKeyword = "milk";
    
    public PageModels pageModels;
    private List<SearchThumb> searchThumbs = new();
    private bool isSearching;
    private const string SketchfabTokenKey = "SketchfabTokenKey";
    
    private Vector2 scrollPosition;
    private GridPanel panelDrawer;


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
        apiToken = PlayerPrefs.GetString(SketchfabTokenKey, "your-sketchfab-api-token"); //https://sketchfab.com/settings/password
        panelDrawer = new GridPanel();
        Instance = this;
    }

    public static SketchfabBrowser Instance { get; private set; }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label($"Unavailable in play mode.", EditorStyles.boldLabel);
            return;
        }
        
        if (!connected)
        {
            DrawConnectionUI();
        }
        else
        {
            GUILayout.Label($"Connected as {accountName}", EditorStyles.boldLabel);
            
            #region ٍSearch Button
            GUILayout.Space(20);

            string searchKewordInput = "SearchKeywordInput";
            GUI.SetNextControlName(searchKewordInput);
            searchKeyword = EditorGUILayout.TextField("keyword", searchKeyword);
            GUI.enabled = !isSearching || string.IsNullOrEmpty(searchKeyword);
            if (GUILayout.Button("Search"))
            {
                Search24(searchKeyword).Forget();
            }
            GUI.enabled = true;

            if (GUI.GetNameOfFocusedControl() == searchKewordInput && Event.current.keyCode == KeyCode.Return)
            {
                Search24(searchKeyword).Forget();
            }
            #endregion
            
            if (pageModels.results.Length > 0)
            {
                DisplayResults();
                return;
            }
            
            GUILayout.Label("No models available.");
        }
    }

    private void DisplayResults()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        panelDrawer.Draw(position.width, pageModels.results, searchThumbs);
        EditorGUILayout.EndScrollView();
    }

    private void DrawModelDetails()
    {
        { 
            GUILayout.Space(20);
            modelId = EditorGUILayout.TextField("Model ID", modelId);

            if (string.IsNullOrEmpty(modelId) || modelId.Length < 13)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Fetch Model Details"))
            {
                FetchInfo().Forget();
                Repaint();
            }

            GUI.enabled = true;
            
            if (CurrentModel != null)
            {

                var dd = DateTime.ParseExact(CurrentModel.updatedAt, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                string updateShortDate = dd.ToString("dd-MMMM-yyyy");
                
                string desc = CurrentModel.description.Length > 20 ? CurrentModel.description.Substring(0, 20): CurrentModel.description;
                EditorGUILayout.HelpBox($"Model Name: {CurrentModel.name}\nDescription: {desc}\n"
                                        + $"Updated at: {updateShortDate}"
                                        , MessageType.Info);
                //Load thumbnail only once
                if (thumb == null) GetThumbnail().Forget();
                else GUILayout.Label(thumb, GUILayout.Width(256));

                //Download model
                if (CurrentModel.isDownloadable)
                {
                    GUILayout.Label($"License: {CurrentModel.license.label}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Download Model")) DownloadModel(modelId, CurrentModel.name).Forget();
                }
                else
                {
                    var price = (CurrentModel.price/100).ToString("N2");
                    GUILayout.Label($"Price: {price}", EditorStyles.boldLabel);
                    
                }
                
                GUILayout.Space(10);
            }
            
            moreInfo = EditorGUILayout.BeginFoldoutHeaderGroup(moreInfo, "More info", EditorStyles.foldoutHeader);

            if (moreInfo && CurrentModel != null)
            {
                GUILayout.Label($"Mesh Data", EditorStyles.boldLabel);
                EditorGUI.indentLevel += 50;
                
                GUILayout.Label($"Vertex Count: {CurrentModel.vertexCount.ToString("N0")}");
                GUILayout.Label($"Face Count: {CurrentModel.faceCount.ToString("N0")}");
                EditorGUI.indentLevel -= 50;
                GUILayout.Label($"Materials: {CurrentModel.materialCount}");
                GUILayout.Label($"Textures: {CurrentModel.textureCount}");
                
                if(CurrentModel.animationCount > 0)
                {
                    GUILayout.Label($"Animations: {CurrentModel.animationCount.ToString("N0")}");
                }
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Goto Model", hyperlinkStyle)) Application.OpenURL(CurrentModel.viewerUrl);
        }
    }

    private void DrawConnectionUI()
    {
        hyperlinkStyle ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.cyan } };

        string tokenFieldName = "apiTokenField";
        GUI.SetNextControlName(tokenFieldName);
        apiToken = EditorGUILayout.TextField("API Token", apiToken);

        if (GUILayout.Button("Get your API Token", hyperlinkStyle))
        {
            Application.OpenURL("https://sketchfab.com/settings/password");
        }

        if (string.IsNullOrEmpty(apiToken) || apiToken.Length != 32)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Connect to Sketchfab"))
        {
            GUI.FocusControl(null);
            ConnectToSketchfab().Forget();
        }

        GUI.enabled = true;
        
        if (GUI.GetNameOfFocusedControl() == tokenFieldName && Event.current.keyCode == KeyCode.Return)
        {
            GUI.FocusControl(null);
            ConnectToSketchfab().Forget();
        }
    }

    private async UniTaskVoid GetThumbnail()
    {
        thumb = await DownloadImage(CurrentModel.thumbnails.images[0].url);
         Repaint();
    }

    async UniTaskVoid ConnectToSketchfab()
    {
        await Connect(apiToken);
        CurrentModel = null;
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
                Repaint();
                
                PlayerPrefs.SetString(SketchfabTokenKey, token);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("Failed to connect to Sketchfab: " + request.error);
            }
        }
    }


    async UniTaskVoid FetchInfo() => await FetchModelInfo(modelId);
    async UniTaskVoid Download() => await DownloadModel(modelId, CurrentModel.name);

    async UniTask Search24(string keyword, string after= null)
    {
        isSearching = true;
        GUI.FocusControl(null);

        string searchRequest = $"https://api.sketchfab.com/v3/search?type=models&downloadable=true&purchasable=true&tags={keyword}&name={keyword}&description={keyword}&sort_by=-likeCount&per_page=24&after={after}";
        using (var request = UnityWebRequest.Get(searchRequest))
        {
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + request.error);
                isSearching = false;
                return;
            }

            pageModels = JsonUtility.FromJson<PageModels>(request.downloadHandler.text);
            Debug.LogWarning($"Search Finished with {pageModels.results.Length} results!");
            isSearching = false;
            await LoadSearchThumbs();
        }

        isSearching = false;
        Repaint();
    }

    private async Task LoadSearchThumbs()
    {
        if (pageModels.results.Length > 0)
        {
            searchThumbs.Clear();
            foreach (var model in pageModels.results)
            {
                if (model.thumbnails.images.Length > 2)
                {
                    var thumb = await DownloadImage(model.thumbnails.images[3].url);
                    if (thumb != null)
                    {
                        searchThumbs.Add(new SearchThumb(model.uid, thumb));
                    }
                    else
                    {
                        Debug.LogError("Failed to download thumbnail for model: " + model.uid);
                    }
                }
            }
        }
        isSearching = false;
    }


    public async UniTask FetchModelInfo(string modelId)
    {
        string modelInfoUrl = $"https://api.sketchfab.com/v3/models/{modelId}";
        using (var request = UnityWebRequest.Get(modelInfoUrl))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                thumb = null;
                CurrentModel = JsonUtility.FromJson<Model>(request.downloadHandler.text);
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
            return default;
        }
    }

    public async UniTask DownloadModel(string modelId, string modelName)
    {
        CurrentModel = new Model();
        CurrentModel.name = modelName;
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

                string fileName = CurrentModel.name + $".zip"; 
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

    private async Task Unzip(string savePath)
    {
        string unpackPath = $"{defaultSavePath}/{CurrentModel.name}";
        if (Directory.Exists(unpackPath))
        {
            Directory.Delete(unpackPath, true);
        }

        Directory.CreateDirectory(unpackPath);

        try
        {
            await UniTask.Run(() =>
            {
                ZipFile.ExtractToDirectory(savePath, unpackPath);
            });
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to unzip the model: " + e.Message);
        }

        File.Delete(savePath);
        Debug.Log($"Model is downloaded to: " + defaultSavePath);
        
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

    [System.Serializable]
    public class PageModels
    {
        public Model[] results;
        public Pagination pagination;
    }
    
    [System.Serializable]
    public class Pagination
    {
        public string next;
    }

    [System.Serializable]
    public class Model
    {
        public string name;
        public string description;
        public string uri; // URL of the model's page
        public Thumbnails thumbnails; // URLs and images of the model's thumbnails
        public bool isDownloadable;
        public string viewerUrl;
        public int vertexCount;
        public int textureCount;
        public int materialCount;
        public int faceCount;
        public int animationCount;
        
        public float price;
        public string updatedAt;
        public License license;
        public string uid;
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
    public class License
    {
        public string label;
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

public class SearchThumb
{
    public readonly string uid;
    public readonly Texture2D thumb;

    public SearchThumb(string modelUid, Texture2D texture2D)
    {
        uid = modelUid;
        thumb = texture2D;
    }
}


using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
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
    private Model _currentModel;
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
    }

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
        //if (searchThumbs == null || searchThumbs.Count == 0) return;
        
        int rowCount = 6;
        int columnCount = 2;
        float panelWidth = (position.width -60) / columnCount;
        float panelHeight = 60f;
        float padding = 10f;

        GUILayout.Space(padding);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int thumbIndex=0;
        for (int row = 0; row < rowCount; row++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding);
            for (int col = 0; col < columnCount; col++)
            {
                if (thumbIndex >= searchThumbs.Count) break;
                Rect panelRect = GUILayoutUtility.GetRect(panelWidth, panelHeight);
                //panelDrawer.Draw(panelRect, row, col); //TODO: take model drawing logic here

                var m = pageModels.results[thumbIndex];
                var thumb = searchThumbs[thumbIndex];

                GUILayout.BeginVertical();
                GUILayout.Label(m.name, GUILayout.Width(266));
                GUILayout.Box(thumb.thumb, GUILayout.Width(266), GUILayout.Height(100));
                GUILayout.EndVertical();
                GUILayout.Space(padding);
                thumbIndex++;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(padding);
        }

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
            
            if (_currentModel != null)
            {

                var dd = DateTime.ParseExact(_currentModel.updatedAt, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                string updateShortDate = dd.ToString("dd-MMMM-yyyy");
                
                string desc = _currentModel.description.Length > 20 ? _currentModel.description.Substring(0, 20): _currentModel.description;
                EditorGUILayout.HelpBox($"Model Name: {_currentModel.name}\nDescription: {desc}\n"
                                        + $"Updated at: {updateShortDate}"
                                        , MessageType.Info);
                //Load thumbnail only once
                if (thumb == null) GetThumbnail().Forget();
                else GUILayout.Label(thumb, GUILayout.Width(256));

                //Download model
                if (_currentModel.isDownloadable)
                {
                    GUILayout.Label($"License: {_currentModel.license.label}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Download Model")) DownloadModel().Forget();
                }
                else
                {
                    var price = (_currentModel.price/100).ToString("N2");
                    GUILayout.Label($"Price: {price}", EditorStyles.boldLabel);
                    
                }
                
                GUILayout.Space(10);
            }
            
            moreInfo = EditorGUILayout.BeginFoldoutHeaderGroup(moreInfo, "More info", EditorStyles.foldoutHeader);

            if (moreInfo && _currentModel != null)
            {
                GUILayout.Label($"Mesh Data", EditorStyles.boldLabel);
                EditorGUI.indentLevel += 50;
                
                GUILayout.Label($"Vertex Count: {_currentModel.vertexCount.ToString("N0")}");
                GUILayout.Label($"Face Count: {_currentModel.faceCount.ToString("N0")}");
                EditorGUI.indentLevel -= 50;
                GUILayout.Label($"Materials: {_currentModel.materialCount}");
                GUILayout.Label($"Textures: {_currentModel.textureCount}");
                
                if(_currentModel.animationCount > 0)
                {
                    GUILayout.Label($"Animations: {_currentModel.animationCount.ToString("N0")}");
                }
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Goto Model", hyperlinkStyle)) Application.OpenURL(_currentModel.viewerUrl);
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
        thumb = await DownloadImage(_currentModel.thumbnails.images[0].url);
         Repaint();
    }

    async UniTaskVoid ConnectToSketchfab()
    {
        await Connect(apiToken);
        _currentModel = null;
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
    async UniTaskVoid Download() => await DownloadModel();

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


    async UniTask FetchModelInfo(string modelId)
    {
        string modelInfoUrl = $"https://api.sketchfab.com/v3/models/{modelId}";
        using (var request = UnityWebRequest.Get(modelInfoUrl))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                thumb = null;
                _currentModel = JsonUtility.FromJson<Model>(request.downloadHandler.text);
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

                string fileName = _currentModel.name + $".zip"; 
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
        string unpackPath = $"{defaultSavePath}/{_currentModel.name}";
        if (Directory.Exists(unpackPath))
        {
            Directory.Delete(unpackPath, true);
        }

        Directory.CreateDirectory(unpackPath);

        try
        {
            ZipFile.ExtractToDirectory(savePath, unpackPath);
            
            var targetObject = AssetDatabase.LoadAssetAtPath<Object>(savePath);
            if (targetObject != null)
            {
                Selection.activeObject = targetObject;
                EditorGUIUtility.PingObject(targetObject);
            }
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to unzip the model: " + e.Message);
        }

        File.Delete(savePath);
        Debug.Log($"Model is downloaded to: " + defaultSavePath);
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

internal class SearchThumb
{
    public readonly string uid;
    public readonly Texture2D thumb;

    public SearchThumb(string modelUid, Texture2D texture2D)
    {
        uid = modelUid;
        thumb = texture2D;
    }
}


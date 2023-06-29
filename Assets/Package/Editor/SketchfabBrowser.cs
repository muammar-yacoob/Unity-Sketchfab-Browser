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
using Package.Runtime;
using UnityEngine.Networking;
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
        windowIcon =
            (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Package/Editor/res/sketchfab-logo.png", typeof(Texture2D));
        titleContent.image = windowIcon;
        apiToken = PlayerPrefs.GetString(SketchfabTokenKey,
            "your-sketchfab-api-token"); //https://sketchfab.com/settings/password
        panelDrawer = new GridPanel();
        Instance = this;
    }

    public static SketchfabBrowser Instance { get; private set; }
    public bool IsDownloading { get; set; }

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

    async UniTask Search24(string keyword, string after = null)
    {
        isSearching = true;
        GUI.FocusControl(null);

        string searchRequest =
            $"https://api.sketchfab.com/v3/search?type=models&downloadable=true&purchasable=true&tags={keyword}&name={keyword}&description={keyword}&sort_by=-likeCount&per_page=24&after={after}";
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
            Debug.Log($"Search Finished with {pageModels.results.Length} results!");
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
        IsDownloading = true;
        CurrentModel = new Model();
        CurrentModel.name = modelName;
        CurrentModel.IsDownloading = true;
        string downloadUrl = $"https://api.sketchfab.com/v3/models/{modelId}/download";
        using (var request = UnityWebRequest.Get(downloadUrl))
        {
            request.SetRequestHeader("Authorization", $"Token {apiToken}");
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401)
                {
                    Debug.LogError("Unauthorized: Invalid API token.");
                }
                else
                {
                    ModelDownloadInfo downloadInfo =
                        JsonUtility.FromJson<ModelDownloadInfo>(request.downloadHandler.text);


                    await DownloadModelFromUrl(downloadInfo.gltf.url, ModelFormatExtension.gltf);
                    //await DownloadModelFromUrl(downloadInfo.source.url, ModelFormatExtension.fbx);
                }
            }
        }

        IsDownloading = false;
        CurrentModel.IsDownloading = false;
        Repaint();
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
            await UniTask.Run(() => { ZipFile.ExtractToDirectory(savePath, unpackPath); });
            AssetDatabase.Refresh(); //to avoid warnings Import Error Code(4)
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

    
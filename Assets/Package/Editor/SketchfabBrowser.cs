using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Package.Runtime;
using SparkGames.Sketchfab.Package.Editor;
using UnityEngine.Networking;

public class SketchfabBrowser : EditorWindow
{
    private string modelId = "564e02a97528499388ca00d3c6bdb044";
    string searchKewordInputControl = "Cupcake";

    private string apiToken;
    private bool connected;
    private string accountName;
    public Model CurrentModel = new Model();
    private Texture2D windowIcon;
    private GUIStyle hyperlinkStyle;
    private Texture2D thumb;
    private bool moreInfo;
    private PageModels currentPageModels;
    private string searchKeyword;

    public PageModels pageModels;
    private List<SearchThumb> searchThumbs = new();
    private bool isSearching;

    private Vector2 scrollPosition;
    private GridPanel panelDrawer;



    [MenuItem("Assets/Sketchfab Browser")]
    public static void ShowWindow()
    {
        SketchfabBrowser window = GetWindow<SketchfabBrowser>();
        window.titleContent = new GUIContent("Sketchfab Browser", window.windowIcon);
        window.minSize = new Vector2(500, 500);
        window.maxSize = new Vector2(1000, 1000);

    }

    private void OnEnable()
    {
        windowIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Package/Editor/res/sketchfab-logo.png", typeof(Texture2D));
        titleContent.image = windowIcon;
        panelDrawer = new GridPanel();
        apiToken = ModelDownloader.Instance.ApiToken;
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Unavailable in play mode.", MessageType.Error);
            return;
        }

        if (!connected)
        {
            DrawConnectionUI();
        }
        else
        {

            EditorGUILayout.HelpBox($"Connected as {accountName}",MessageType.Info);
            //GUILayout.Label($"Connected as {accountName}", EditorStyles.boldLabel);

            DrawSearchUI();

            if (pageModels?.results?.Length > 0 && !isSearching)
            {
                DisplayResults();
                return;
            }
        }
    }


    private void DrawSearchUI()
    {
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName(searchKewordInputControl);
        searchKeyword = EditorGUILayout.TextField("Search Keyword:", searchKeyword);

        bool canSearch = !isSearching && !string.IsNullOrEmpty(searchKeyword);
        bool canNavigatePages = !isSearching && pageModels != null;

        using (new EditorGUI.DisabledScope(!canSearch))
        {
            if (GUILayout.Button("Search") || GUI.GetNameOfFocusedControl() == searchKewordInputControl && Event.current.keyCode == KeyCode.Return)
            {
                Search24(searchKeyword).Forget();
            }
        }
        GUILayout.EndHorizontal();

        if (canNavigatePages)
        {
            GUILayout.BeginHorizontal();

            if(string.IsNullOrEmpty(pageModels?.previous)) GUI.enabled = false;
            if (GUILayout.Button("Previous"))
            {
                Search24(searchKeyword, pageModels?.previous).Forget();
            }
            GUI.enabled = true;
            
            if(string.IsNullOrEmpty(pageModels?.next)) GUI.enabled = false;
            if (GUILayout.Button("Next"))
            {
                Search24(searchKeyword, after: pageModels?.next).Forget();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        if (pageModels != null && !isSearching)
        {
            string resultMsg = pageModels.results.Length < 24 ? $"{pageModels.results.Length} models found." : "Showing 24 models.";
            GUILayout.Label(resultMsg, EditorStyles.boldLabel);
        }
        else if (isSearching)
        {
            GUILayout.Label("Searching...", EditorStyles.boldLabel);
        }
        else if (pageModels == null)
        {
            EditorGUILayout.HelpBox($"No models found with the keyword {searchKeyword}.",MessageType.Info);

        }

        if (Event.current.type == EventType.Repaint) GUI.FocusControl(searchKewordInputControl);
    }


    
 private void DisplayResults()
    {
        //scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        panelDrawer.Draw(position.width, pageModels.results, searchThumbs);
        //EditorGUILayout.EndScrollView();
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
            try
            {
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    connected = true;
                    AccountInfo accountInfo = JsonUtility.FromJson<AccountInfo>(request.downloadHandler.text);
                    accountName = accountInfo.username;
                    Repaint();

                    ModelDownloader.Instance.SetToken(token);
                }
                else
                {
                    Debug.LogError("Failed to connect to Sketchfab: " + request.error);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to connect to Sketchfab: " + request.error);
            }
        }
    }


    async UniTaskVoid FetchInfo() => await FetchModelInfo(modelId);
    //async UniTaskVoid Download() => await DownloadModel(modelId, CurrentModel.name);

    async UniTask Search24(string keyword, string after = null)
    {
        if (isSearching) return;
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
            //Debug.Log($"Search Finished with {pageModels.results.Length} results!");
            GUI.FocusControl(searchKeyword);
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

   
}
using System;
using Cysharp.Threading.Tasks;
using GLTFast;
using UnityEngine;

[RequireComponent(typeof(GltfAsset))]
public class GLTFGameObject : MonoBehaviour
{
    [SerializeField] private string assetUrl = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF/Duck.gltf";
    private GltfLoader loader; //inject
    private void Awake() => loader = new GltfLoader();

    private GltfAsset gltfAsset;

    private void Start()
    {
        gltfAsset = GetComponent<GltfAsset>();
        LoadModel();
    }

    [ContextMenu("Load Model")]
    public void LoadModel() => LoadModel(assetUrl, success: OnSuccess, fail: OnFail).Forget();

    public async UniTaskVoid LoadModel(string path, Action success = null, Action fail = null)
    {
        gltfAsset.Url = path;

        try
        {
            bool loadSuccess = await gltfAsset.Load(path);
            if (loadSuccess)
            {
                bool op = await gltfAsset.Instantiate();

                if (op)
                {
                    success?.Invoke();
                }
                else
                {
                    fail?.Invoke();
                }
            }
            else
            {
                fail?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading model: {e.Message}");
            fail?.Invoke();

        }
    }
    
    private void OnSuccess()
    {
        Debug.Log("Model loaded successfully.");

        GameObject newModel = gltfAsset.gameObject;
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;
    }

    private void OnFail()
    {
        Debug.Log("Failed to load model.");
    }
}

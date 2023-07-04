using UnityEngine;
using UnityEngine.Serialization;

namespace Package.Runtime
{
    [System.Serializable]
    public class PageModels
    {
        public Model[] results;
        public string next;
        public string previous;
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
        public bool IsDownloading;
        public float DownloadProgress;
        public bool IsDownloaded;
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
    public class AccountInfo
    {
        public string username;
    }
    
    [System.Serializable]
    public class ModelDownloadInfo
    {
        public ModelFormat glb;
        public ModelFormat gltf;
        public ModelFormat source;

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
        glb
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
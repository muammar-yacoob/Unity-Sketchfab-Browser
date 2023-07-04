using System;
using Cysharp.Threading.Tasks;

namespace SparkGames.SketchfabBrowser.Editor
{
    public interface IModelDownloader
    {
        UniTask DownloadModel(Model model, Action<float> onDownloadProgress = null);
        
        string ApiToken { get; }
        void SetToken(string apiToken);
    }
}
using System;
using Cysharp.Threading.Tasks;

namespace Package.Runtime
{
    public interface IModelDownloader
    {
        UniTaskVoid DownloadModel(Model model, Action<float> onDownloadProgress = null);
        
        string ApiToken { get; }
        void SetToken(string apiToken);
        void SetDownloadPath(string downloadPath);
    }
}
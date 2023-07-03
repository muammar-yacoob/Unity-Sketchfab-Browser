using System;
using Cysharp.Threading.Tasks;

namespace Package.Runtime
{
    public interface IModelDownloader
    {
        UniTaskVoid DownloadModel(string modelId, Action<float> onDownloadProgress = null);
    }
}
using System;
using Cysharp.Threading.Tasks;

namespace Package.Runtime
{
    public interface IModelDownloader
    {
        UniTask DownloadModel(Action<float> onDownloadProgress, Action<string> onSuccess, Action<string> onFail);
    }
}
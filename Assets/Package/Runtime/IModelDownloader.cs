using System;
using Cysharp.Threading.Tasks;

namespace Package.Runtime
{
    public interface IModelDownloader
    {
        UniTaskVoid DownloadModel(Model model, Action<float> onDownloadProgress = null);
    }
}
using System;

public class GltfLoader: IGltfLoader
{
    public void LoadModel(string path, Action onSuccess, Action onFail)
    {
        
    }
}

public interface IGltfLoader
{
    void LoadModel(string path, Action onSuccess, Action onFail);
}
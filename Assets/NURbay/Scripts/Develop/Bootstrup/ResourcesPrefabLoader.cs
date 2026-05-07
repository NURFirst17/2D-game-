using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ResourcesPrefabLoader : IPrefabLoader
{
    public GameObject Load(string path)
    {
        return Load<GameObject>(path);
    }

    public T Load<T>(string path) where T : Object
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Prefab path is null or empty.", nameof(path));
        }

        var asset = Resources.Load<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException($"Asset not found in Resources by path '{path}'.");
        }

        return asset;
    }

    public GameObject Instantiate(string path)
    {
        var prefab = Load(path);
        return UnityEngine.Object.Instantiate(prefab);
    }

    public GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var prefab = Load(path);
        return UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
    }
}

using UnityEngine;

public interface IPrefabLoader
{
    GameObject Load(string path);
    T Load<T>(string path) where T : Object;
    GameObject Instantiate(string path);
    GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null);
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckpointSnapshotSystem
{
    private const string SavedCheckpointKey = "continue_checkpoint_snapshot";
    private const string MainMenuSceneName = "MainMenu";

    private sealed class SnapshotRunner : MonoBehaviour
    {
    }

    private sealed class SceneSnapshot
    {
        public string SceneName;
        public Vector3 RespawnPosition;
        public readonly Dictionary<string, GameObjectSnapshot> Objects = new();
    }

    private sealed class GameObjectSnapshot
    {
        public bool ActiveSelf;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public bool HasRigidbody;
        public Vector2 LinearVelocity;
        public float AngularVelocity;
        public readonly List<ComponentSnapshot> Components = new();
    }

    [Serializable]
    private sealed class ComponentSnapshot
    {
        public string TypeName;
        public int TypeIndex;
        public string State;
    }

    [Serializable]
    private sealed class SavedSceneSnapshot
    {
        public string SceneName;
        public Vector3 RespawnPosition;
        public List<SavedGameObjectSnapshot> Objects = new();
    }

    [Serializable]
    private sealed class SavedGameObjectSnapshot
    {
        public string Path;
        public bool ActiveSelf;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public bool HasRigidbody;
        public Vector2 LinearVelocity;
        public float AngularVelocity;
        public List<ComponentSnapshot> Components = new();
    }

    private static SceneSnapshot _snapshot;
    private static SnapshotRunner _runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _snapshot = LoadSavedCheckpoint();
        _runner = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static void CaptureCurrentScene(Vector3 respawnPosition)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        var snapshot = new SceneSnapshot
        {
            SceneName = scene.name,
            RespawnPosition = respawnPosition
        };

        foreach (var gameObject in GetSceneGameObjects(scene))
        {
            snapshot.Objects[GetHierarchyPath(gameObject.transform)] = CaptureGameObject(gameObject);
        }

        _snapshot = snapshot;
        SaveCheckpoint(snapshot);
        Debug.Log($"Checkpoint saved for scene '{scene.name}'.");
    }

    public static void ClearSavedCheckpoint()
    {
        _snapshot = null;
        PlayerPrefs.DeleteKey(SavedCheckpointKey);
        PlayerPrefs.Save();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (_snapshot == null)
        {
            return;
        }

        if (_snapshot.SceneName != scene.name)
        {
            if (scene.name == MainMenuSceneName)
            {
                return;
            }

            Debug.Log($"Checkpoint from scene '{_snapshot.SceneName}' cleared after loading '{scene.name}'.");
            ClearSavedCheckpoint();
            return;
        }

        GetRunner().StartCoroutine(RestoreAfterSceneStart(scene));
    }

    private static IEnumerator RestoreAfterSceneStart(Scene scene)
    {
        yield return null;

        if (_snapshot == null || _snapshot.SceneName != scene.name)
        {
            yield break;
        }

        RestoreScene(scene, _snapshot);
    }

    private static void RestoreScene(Scene scene, SceneSnapshot snapshot)
    {
        var loadedObjects = GetSceneGameObjects(scene);

        foreach (var gameObject in loadedObjects)
        {
            var path = GetHierarchyPath(gameObject.transform);
            if (!snapshot.Objects.TryGetValue(path, out var objectSnapshot))
            {
                gameObject.SetActive(false);
                continue;
            }

            RestoreTransform(gameObject.transform, objectSnapshot);
        }

        foreach (var gameObject in loadedObjects)
        {
            var path = GetHierarchyPath(gameObject.transform);
            if (snapshot.Objects.TryGetValue(path, out var objectSnapshot))
            {
                RestoreGameObject(gameObject, objectSnapshot);
            }
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = snapshot.RespawnPosition;

            if (player.TryGetComponent<Rigidbody2D>(out var playerBody))
            {
                playerBody.linearVelocity = Vector2.zero;
                playerBody.angularVelocity = 0f;
            }
        }

        Debug.Log($"Checkpoint restored for scene '{scene.name}'.");
    }

    private static GameObjectSnapshot CaptureGameObject(GameObject gameObject)
    {
        var transform = gameObject.transform;
        var snapshot = new GameObjectSnapshot
        {
            ActiveSelf = gameObject.activeSelf,
            LocalPosition = transform.localPosition,
            LocalRotation = transform.localRotation,
            LocalScale = transform.localScale
        };

        if (gameObject.TryGetComponent<Rigidbody2D>(out var body))
        {
            snapshot.HasRigidbody = true;
            snapshot.LinearVelocity = body.linearVelocity;
            snapshot.AngularVelocity = body.angularVelocity;
        }

        var typeCounts = new Dictionary<string, int>();
        foreach (var behaviour in gameObject.GetComponents<MonoBehaviour>())
        {
            if (behaviour is not ICheckpointStateParticipant participant)
            {
                continue;
            }

            var typeName = behaviour.GetType().AssemblyQualifiedName;
            typeCounts.TryGetValue(typeName, out var typeIndex);
            typeCounts[typeName] = typeIndex + 1;

            snapshot.Components.Add(new ComponentSnapshot
            {
                TypeName = typeName,
                TypeIndex = typeIndex,
                State = participant.CaptureCheckpointState()
            });
        }

        return snapshot;
    }

    private static void RestoreTransform(Transform transform, GameObjectSnapshot snapshot)
    {
        transform.localPosition = snapshot.LocalPosition;
        transform.localRotation = snapshot.LocalRotation;
        transform.localScale = snapshot.LocalScale;
    }

    private static void RestoreGameObject(GameObject gameObject, GameObjectSnapshot snapshot)
    {
        gameObject.SetActive(snapshot.ActiveSelf);

        if (snapshot.HasRigidbody && gameObject.TryGetComponent<Rigidbody2D>(out var body))
        {
            body.linearVelocity = snapshot.LinearVelocity;
            body.angularVelocity = snapshot.AngularVelocity;
        }

        foreach (var componentSnapshot in snapshot.Components)
        {
            var type = Type.GetType(componentSnapshot.TypeName);
            if (type == null)
            {
                continue;
            }

            var components = gameObject.GetComponents(type);
            if (componentSnapshot.TypeIndex >= components.Length ||
                components[componentSnapshot.TypeIndex] is not ICheckpointStateParticipant participant)
            {
                continue;
            }

            participant.RestoreCheckpointState(componentSnapshot.State);
        }
    }

    private static List<GameObject> GetSceneGameObjects(Scene scene)
    {
        var gameObjects = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            AddHierarchy(root.transform, gameObjects);
        }

        return gameObjects;
    }

    private static void AddHierarchy(Transform transform, List<GameObject> gameObjects)
    {
        gameObjects.Add(transform.gameObject);

        for (var i = 0; i < transform.childCount; i++)
        {
            AddHierarchy(transform.GetChild(i), gameObjects);
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var path = $"{transform.name}[{transform.GetSiblingIndex()}]";
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}[{transform.GetSiblingIndex()}]/{path}";
        }

        return path;
    }

    private static SnapshotRunner GetRunner()
    {
        if (_runner != null)
        {
            return _runner;
        }

        var runnerObject = new GameObject(nameof(CheckpointSnapshotSystem));
        UnityEngine.Object.DontDestroyOnLoad(runnerObject);
        _runner = runnerObject.AddComponent<SnapshotRunner>();
        return _runner;
    }

    private static void SaveCheckpoint(SceneSnapshot snapshot)
    {
        var savedSnapshot = new SavedSceneSnapshot
        {
            SceneName = snapshot.SceneName,
            RespawnPosition = snapshot.RespawnPosition
        };

        foreach (var pair in snapshot.Objects)
        {
            var objectSnapshot = pair.Value;
            savedSnapshot.Objects.Add(new SavedGameObjectSnapshot
            {
                Path = pair.Key,
                ActiveSelf = objectSnapshot.ActiveSelf,
                LocalPosition = objectSnapshot.LocalPosition,
                LocalRotation = objectSnapshot.LocalRotation,
                LocalScale = objectSnapshot.LocalScale,
                HasRigidbody = objectSnapshot.HasRigidbody,
                LinearVelocity = objectSnapshot.LinearVelocity,
                AngularVelocity = objectSnapshot.AngularVelocity,
                Components = new List<ComponentSnapshot>(objectSnapshot.Components)
            });
        }

        PlayerPrefs.SetString(SavedCheckpointKey, JsonUtility.ToJson(savedSnapshot));
        PlayerPrefs.Save();
    }

    private static SceneSnapshot LoadSavedCheckpoint()
    {
        var json = PlayerPrefs.GetString(SavedCheckpointKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var savedSnapshot = JsonUtility.FromJson<SavedSceneSnapshot>(json);
        if (savedSnapshot == null || string.IsNullOrWhiteSpace(savedSnapshot.SceneName))
        {
            return null;
        }

        var snapshot = new SceneSnapshot
        {
            SceneName = savedSnapshot.SceneName,
            RespawnPosition = savedSnapshot.RespawnPosition
        };

        if (savedSnapshot.Objects == null)
        {
            return snapshot;
        }

        foreach (var savedObject in savedSnapshot.Objects)
        {
            if (savedObject == null || string.IsNullOrWhiteSpace(savedObject.Path))
            {
                continue;
            }

            var objectSnapshot = new GameObjectSnapshot
            {
                ActiveSelf = savedObject.ActiveSelf,
                LocalPosition = savedObject.LocalPosition,
                LocalRotation = savedObject.LocalRotation,
                LocalScale = savedObject.LocalScale,
                HasRigidbody = savedObject.HasRigidbody,
                LinearVelocity = savedObject.LinearVelocity,
                AngularVelocity = savedObject.AngularVelocity
            };
            if (savedObject.Components != null)
            {
                objectSnapshot.Components.AddRange(savedObject.Components);
            }

            snapshot.Objects[savedObject.Path] = objectSnapshot;
        }

        return snapshot;
    }
}

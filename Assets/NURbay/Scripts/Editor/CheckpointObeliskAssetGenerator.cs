using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CheckpointObeliskAssetGenerator
{
    private const string Folder = "Assets/NURbay/Art/Obelisk_demo";
    private const string InactiveSpritesPath = Folder + "/Obelisk.png";
    private const string ActivatedSpritesPath = Folder + "/Obelisk_effects.png";
    private const string InactiveClipPath = Folder + "/Obelisk_Inactive.anim";
    private const string ActivatedClipPath = Folder + "/Obelisk_Activated.anim";
    private const string ControllerPath = Folder + "/ObeliskCheckpoint.controller";
    private const string PrefabPath = "Assets/NURbay/Prefabs/Checkpoint.prefab";
    private const string ParameterName = "IsActivated";
    private const string VisualObjectName = "ObeliskVisual";

    [MenuItem("Tools/NURbay/Regenerate Checkpoint Obelisk %#g")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !AssetDatabase.LoadAllAssetsAtPath(InactiveSpritesPath).OfType<Sprite>().Any())
        {
            return;
        }

        var inactiveSprites = LoadSprites(InactiveSpritesPath, "Obelisk_", 0, 14);
        var activatedSprites = LoadSprites(ActivatedSpritesPath, "Obelisk_effects_", 30, 14);
        if (inactiveSprites.Length == 0 || activatedSprites.Length == 0)
        {
            return;
        }

        var inactiveClip = CreateOrUpdateClip(InactiveClipPath, inactiveSprites);
        var activatedClip = CreateOrUpdateClip(ActivatedClipPath, activatedSprites);
        var controller = CreateOrUpdateController(inactiveClip, activatedClip);
        UpdateCheckpointPrefab(controller, inactiveSprites[0]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Sprite[] LoadSprites(string path, string prefix, int startIndex, int count)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(sprite => TryGetIndex(sprite.name, prefix, out var index) &&
                             index >= startIndex &&
                             index < startIndex + count)
            .OrderBy(sprite => GetIndex(sprite.name, prefix))
            .ToArray();
    }

    private static bool TryGetIndex(string name, string prefix, out int index)
    {
        index = -1;
        return name.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(name.Substring(prefix.Length), out index);
    }

    private static int GetIndex(string name, string prefix)
    {
        TryGetIndex(name, prefix, out var index);
        return index;
    }

    private static AnimationClip CreateOrUpdateClip(string path, Sprite[] sprites)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
        clip.frameRate = 12f;

        var binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var frames = sprites
            .Select((sprite, index) => new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(AnimationClip inactiveClip, AnimationClip activatedClip)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(ParameterName, AnimatorControllerParameterType.Bool);

        var stateMachine = controller.layers[0].stateMachine;
        var inactiveState = stateMachine.AddState("Inactive");
        inactiveState.motion = inactiveClip;
        var activatedState = stateMachine.AddState("Activated");
        activatedState.motion = activatedClip;
        stateMachine.defaultState = inactiveState;

        AddTransition(inactiveState, activatedState, true);
        AddTransition(activatedState, inactiveState, false);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddTransition(AnimatorState source, AnimatorState destination, bool parameterValue)
    {
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(
            parameterValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            ParameterName);
    }

    private static void UpdateCheckpointPrefab(RuntimeAnimatorController controller, Sprite inactiveSprite)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var visual = prefabRoot.transform.Find(VisualObjectName);
            if (visual == null)
            {
                visual = new GameObject(VisualObjectName).transform;
                visual.SetParent(prefabRoot.transform, false);
            }

            var renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = inactiveSprite;

            var animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;

            var trigger = prefabRoot.GetComponent<CheckpointTrigger>();
            var serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("checkpointRenderer").objectReferenceValue = renderer;
            serializedTrigger.FindProperty("checkpointAnimator").objectReferenceValue = animator;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}

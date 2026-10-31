using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Script một lần (nằm ngoài Assets): nối mẫu A0 vào Player (Idle/Walk, màu trắng) và Slime.
public static class A0Wire
{
    public static string Run()
    {
        var log = new StringBuilder();
        const string anim = "Assets/_Game/Content/Animation";
        const string pl = "Assets/_Game/Art/Characters/Player";

        Sprite[] idle = Sprites(pl + "/Player_Composite_Idle_E.png");
        Sprite[] walk = Sprites(pl + "/Player_Composite_Walk_E.png");
        log.AppendLine("Khung Idle=" + idle.Length + " Walk=" + walk.Length);

        AnimationClip idleClip = MakeClip(anim + "/Player_Idle_E.anim", idle, 6f);
        AnimationClip walkClip = MakeClip(anim + "/Player_Walk_E.anim", walk, 10f);

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(anim + "/Player_AnimController.controller");
        foreach (var st in ctrl.layers[0].stateMachine.states)
        {
            if (st.state.name == "Idle") st.state.motion = idleClip;
            else if (st.state.name == "Walk") st.state.motion = walkClip;
        }
        EditorUtility.SetDirty(ctrl);

        // Player.prefab: màu trắng (H1) + hình khung đầu để thấy ngay trong Editor.
        string pp = "Assets/_Game/Prefabs/Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(pp);
        var sr = root.GetComponent<SpriteRenderer>();
        log.AppendLine("Player màu cũ=" + sr.color);
        sr.color = Color.white;
        sr.sprite = idle[0];
        PrefabUtility.SaveAsPrefabAsset(root, pp);
        PrefabUtility.UnloadPrefabContents(root);

        // Slime: đổi sprite trong EnemyDefinition; báo màu của Enemy.prefab.
        Sprite[] slime = Sprites("Assets/_Game/Art/Characters/Enemies/Enemy_Slime_Idle_E.png");
        var def = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Game/Data/Enemies/Enemy_Slime.asset");
        var so = new SerializedObject(def);
        so.FindProperty("sprite").objectReferenceValue = slime[0];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(def);

        GameObject eroot = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/Enemy.prefab");
        var esr = eroot.GetComponentInChildren<SpriteRenderer>();
        log.AppendLine("Enemy.prefab màu=" + (esr != null ? esr.color.ToString() : "không có SR") + " sprite=" + (esr != null && esr.sprite != null ? esr.sprite.name : "null"));
        PrefabUtility.UnloadPrefabContents(eroot);

        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    private static Sprite[] Sprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static AnimationClip MakeClip(string path, Sprite[] frames, float fps)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}

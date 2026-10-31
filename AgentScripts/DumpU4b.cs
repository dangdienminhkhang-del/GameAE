using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpU4b
{
    private static string Val(SerializedProperty it)
    {
        return it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "NULL")
            : it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString("0.###")
            : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString()
            : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString()
            : it.propertyType == SerializedPropertyType.String ? it.stringValue
            : it.propertyType == SerializedPropertyType.Enum ? it.enumDisplayNames[it.enumValueIndex]
            : it.propertyType == SerializedPropertyType.LayerMask ? ("mask=" + it.intValue) : it.propertyType.ToString();
    }

    private static void Dump(StringBuilder sb, UnityEngine.Object o)
    {
        var so = new SerializedObject(o);
        var it = so.GetIterator(); it.Next(true);
        while (it.NextVisible(false))
        {
            if (it.name == "m_Script" || it.propertyType == SerializedPropertyType.Generic) continue;
            sb.AppendLine("   " + it.name + " = " + Val(it));
        }
    }

    public static string Run()
    {
        var sb = new StringBuilder();
        foreach (string p in new[] { "Assets/_Game/Data/Enemies/Enemy_PipeSpitter.asset", "Assets/_Game/Data/Enemies/Enemy_WireStalker.asset", "Assets/_Game/Data/Enemies/Enemy_Slime.asset" })
        {
            sb.AppendLine("ASSET " + p);
            Dump(sb, AssetDatabase.LoadMainAssetAtPath(p));
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(p));
            var drops = so.FindProperty("drops");
            sb.AppendLine("   drops=" + (drops != null ? drops.arraySize.ToString() : "?"));
        }
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb.GetType().Name != "EnemyProjectilePool") continue;
            sb.AppendLine("POOL @" + mb.transform.parent?.name + "/" + mb.name + " activeInHierarchy=" + mb.gameObject.activeInHierarchy);
            Dump(sb, mb);
        }
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (!p.ToLower().Contains("projectile") && !p.ToLower().Contains("bullet")) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            sb.AppendLine("PREFAB " + p + " scale=" + go.transform.localScale + " sprite=" + (sr != null && sr.sprite != null ? sr.sprite.name : "null") + " color=" + (sr != null ? sr.color.ToString() : "") + " sortLayer=" + (sr != null ? sr.sortingLayerName + "/" + sr.sortingOrder : "") + " comps=" + string.Join(",", go.GetComponents<Component>().Select(c => c.GetType().Name)));
        }
        return sb.ToString();
    }
}

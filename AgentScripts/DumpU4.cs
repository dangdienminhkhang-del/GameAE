using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpU4
{
    private static void Props(StringBuilder sb, SerializedProperty prop, string indent)
    {
        var it = prop.Copy(); var end = prop.GetEndProperty();
        bool enter = true;
        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;
            if (it.name == "m_Script") continue;
            string v = it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "NULL")
                : it.propertyType == SerializedPropertyType.Float ? it.doubleValue.ToString("0.###")
                : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString()
                : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString()
                : it.propertyType == SerializedPropertyType.String ? it.stringValue
                : it.propertyType == SerializedPropertyType.Enum ? it.enumDisplayNames[it.enumValueIndex]
                : it.propertyType.ToString();
            sb.AppendLine(indent + it.name + " = " + v);
        }
    }

    public static string Run()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Game/Data/Enemies" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var a = AssetDatabase.LoadMainAssetAtPath(p);
            var so = new SerializedObject(a);
            var spr = so.FindProperty("sprite").objectReferenceValue;
            sb.AppendLine("ENEMY " + p + " id=" + so.FindProperty("id").stringValue + " sprite=" + (spr != null ? spr.name + " @" + AssetDatabase.GetAssetPath(spr) : "null") + " role=" + so.FindProperty("role").enumDisplayNames[so.FindProperty("role").enumValueIndex]);
        }
        foreach (string fp in new[] { "Assets/_Game/Content/Floors/Floor01/FloorDefinition_Floor01.asset", "Assets/_Game/Content/Floors/Floor02/FloorDefinition_Floor02.asset" })
        {
            var a = AssetDatabase.LoadMainAssetAtPath(fp);
            if (a == null) { sb.AppendLine("FLOOR không thấy " + fp); continue; }
            var so = new SerializedObject(a);
            var ids = so.FindProperty("spawnEnemyIds");
            sb.Append("FLOOR " + fp + " spawnEnemyIds=");
            for (int i = 0; i < ids.arraySize; i++) sb.Append(ids.GetArrayElementAtIndex(i).stringValue + ";");
            sb.AppendLine(" Lv " + so.FindProperty("recommendedLevelMin").intValue + "-" + so.FindProperty("recommendedLevelMax").intValue);
        }
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb.GetType().Name != "EnemySpawner") continue;
            sb.AppendLine("SPAWNER " + mb.name + " @" + mb.transform.position);
            var so = new SerializedObject(mb);
            var it = so.GetIterator(); it.Next(true);
            while (it.NextVisible(false))
            {
                if (it.name == "m_Script") continue;
                if (it.name == "entries")
                {
                    for (int i = 0; i < it.arraySize; i++) { sb.AppendLine("   entry[" + i + "]"); Props(sb, it.GetArrayElementAtIndex(i), "      "); }
                }
                else if (it.propertyType != SerializedPropertyType.Generic)
                    sb.AppendLine("   " + it.name + " = " + (it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "NULL") : it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString() : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString() : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString() : it.propertyType.ToString()));
            }
        }
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemy.prefab");
        var sr = enemyPrefab.GetComponentInChildren<SpriteRenderer>();
        sb.AppendLine("Enemy.prefab sortingLayer=" + sr.sortingLayerName + " order=" + sr.sortingOrder + " scale=" + enemyPrefab.transform.localScale + " layer=" + LayerMask.LayerToName(enemyPrefab.layer));
        sb.AppendLine("Layers: " + string.Join(",", Enumerable.Range(0, 32).Select(i => LayerMask.LayerToName(i)).Where(n => !string.IsNullOrEmpty(n))));
        sb.AppendLine("Pool trong scene: " + UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(m => m.GetType().Name == "EnemyProjectilePool"));
        foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/_Game/Art/Placeholder" }))
            sb.AppendLine("PLACEHOLDER " + AssetDatabase.GUIDToAssetPath(g));
        return sb.ToString();
    }
}

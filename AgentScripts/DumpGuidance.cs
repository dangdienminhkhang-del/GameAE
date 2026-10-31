using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpGuidance
{
    private static string Val(SerializedProperty it)
    {
        return it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "NULL")
            : it.propertyType == SerializedPropertyType.Float ? it.floatValue.ToString("0.###")
            : it.propertyType == SerializedPropertyType.Integer ? it.intValue.ToString()
            : it.propertyType == SerializedPropertyType.Boolean ? it.boolValue.ToString()
            : it.propertyType == SerializedPropertyType.String ? it.stringValue
            : it.propertyType == SerializedPropertyType.Enum ? it.enumDisplayNames[it.enumValueIndex]
            : it.propertyType == SerializedPropertyType.Color ? it.colorValue.ToString() : it.propertyType.ToString();
    }

    public static string Run()
    {
        var sb = new StringBuilder();
        var cfg = AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Guidance/GuidanceConfig.asset");
        var so = new SerializedObject(cfg);
        var it = so.GetIterator(); it.Next(true);
        while (it.NextVisible(false))
        {
            if (it.name == "m_Script") continue;
            if (it.isArray && it.propertyType == SerializedPropertyType.Generic)
            {
                sb.Append("CONFIG " + it.name + " [" + it.arraySize + "]:");
                for (int i = 0; i < it.arraySize; i++) { var e = it.GetArrayElementAtIndex(i); sb.Append(" " + (e.propertyType == SerializedPropertyType.ObjectReference && e.objectReferenceValue != null ? e.objectReferenceValue.name : "?")); }
                sb.AppendLine();
            }
            else if (it.propertyType == SerializedPropertyType.Generic)
            {
                sb.Append("CONFIG " + it.name + " {");
                var c = it.Copy(); var end = c.GetEndProperty(); bool enter = true;
                while (c.NextVisible(enter) && !SerializedProperty.EqualContents(c, end)) { enter = false; sb.Append(" " + c.name + "=" + Val(c)); }
                sb.AppendLine(" }");
            }
            else sb.AppendLine("CONFIG " + it.name + " = " + Val(it));
        }
        foreach (var g in AssetDatabase.FindAssets("t:TutorialHintDefinition", new[] { "Assets/_Game/Data/Guidance/Hints" }))
        {
            var a = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(g));
            var hs = new SerializedObject(a);
            var hi = hs.GetIterator(); hi.Next(true);
            sb.Append("HINT " + a.name + ":");
            while (hi.NextVisible(false)) { if (hi.name == "m_Script") continue; sb.Append(" " + hi.name + "=" + Val(hi)); }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

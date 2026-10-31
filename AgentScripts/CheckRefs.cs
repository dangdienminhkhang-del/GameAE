using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckRefs
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Scene: " + SceneManager.GetActiveScene().name);
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string tn = mb.GetType().Name;
            if (tn != "BootController" && tn != "LoadingOverlay" && tn != "MainMenuController" && tn != "SettingsPanel") continue;
            sb.AppendLine("== " + tn + " @ " + Path(mb.transform));
            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            it.Next(true);
            while (it.NextVisible(false))
            {
                if (it.name == "m_Script") continue;
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                    sb.AppendLine("   " + it.name + " = " + (it.objectReferenceValue != null ? it.objectReferenceValue.name + " (" + it.objectReferenceValue.GetType().Name + ")" : "NULL"));
            }
        }
        return sb.ToString();
    }

    private static string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}

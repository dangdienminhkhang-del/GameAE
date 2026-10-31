using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneProbe
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("playing=" + Application.isPlaying + " time=" + Time.realtimeSinceStartup.ToString("0.0"));
        for (int i = 0; i < SceneManager.sceneCount; i++) sb.AppendLine("scene[" + i + "]=" + SceneManager.GetSceneAt(i).name + " loaded=" + SceneManager.GetSceneAt(i).isLoaded);
        sb.AppendLine("active=" + SceneManager.GetActiveScene().name);
        return sb.ToString();
    }
}

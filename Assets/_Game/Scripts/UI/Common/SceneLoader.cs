using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Common
{
    public static class SceneLoader
    {
        public const string BootSceneName = "Boot";
        public const string MainMenuSceneName = "MainMenu";
        public const string PvESceneName = "PvE";
        public const string PvPSceneName = "PvP";

        public static IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
            if (asyncOp == null) yield break;
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }

        public static void Load(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
        }
    }
}

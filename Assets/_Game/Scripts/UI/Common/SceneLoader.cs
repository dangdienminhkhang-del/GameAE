using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.Common
{
    public static class SceneLoader
    {
        public const string BootSceneName = "Boot";
        public const string MainMenuSceneName = "MainMenu";
        // Phải trùng tên file scene trong Build Settings (Assets/_Game/Scenes/PvE_Gameplay.unity).
        public const string PvESceneName = "PvE_Gameplay";
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

        /// <summary>
        /// Bản có báo tiến độ (P9.6): <paramref name="progress"/> nhận giá trị 0..1 đơn điệu tăng (qua <see cref="LoadingProgressMapper"/>),
        /// scene chỉ được kích hoạt sau khi đã hiện đủ thời gian tối thiểu (overlay không chớp). API cũ giữ nguyên.
        /// </summary>
        public static IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode, System.IProgress<float> progress, float minShowSeconds = Loading.LoadingProgressMapper.DefaultMinShowSeconds)
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
            if (asyncOp == null)
            {
                progress?.Report(1f);
                yield break;
            }

            var mapper = new Loading.LoadingProgressMapper(minShowSeconds);
            asyncOp.allowSceneActivation = false;
            float startedAt = UnityEngine.Time.realtimeSinceStartup;

            while (true)
            {
                float elapsed = UnityEngine.Time.realtimeSinceStartup - startedAt;
                progress?.Report(mapper.Map(asyncOp.progress, elapsed));
                if (mapper.IsComplete(asyncOp.progress, elapsed)) break;
                yield return null;
            }

            progress?.Report(1f);
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }
    }
}

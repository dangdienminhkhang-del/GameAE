using Game.PvE.SafeZone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.PvP
{
    /// <summary>
    /// Tự động sinh Võ Đài Tỉ Thí (Shadow Duel Arena) tại runtime trong scene PvE.
    /// </summary>
    public static class DuelArenaRuntimeSpawner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (scene, mode) => TrySpawnArena();
            TrySpawnArena();
        }

        public static void TrySpawnArena()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;

            if (Object.FindAnyObjectByType<ShadowDuelArena>() != null) return;

            Vector3 centerPos = Vector3.zero;
            var safeZone = Object.FindAnyObjectByType<SafeZoneTrigger>();
            if (safeZone != null)
            {
                centerPos = safeZone.transform.position;
            }
            else
            {
                var player = Object.FindAnyObjectByType<Player.PlayerController>();
                if (player != null) centerPos = player.transform.position;
            }

            var arenaGo = new GameObject("ShadowDuelArena");
            arenaGo.transform.position = centerPos + new Vector3(6f, 3f, 0f);

            var col = arenaGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.5f;

            arenaGo.AddComponent<ShadowDuelArena>();

            var sr = arenaGo.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.4f, 0.8f, 0.8f); // Màu tím võ đài
            sr.sortingOrder = 1;
        }
    }
}

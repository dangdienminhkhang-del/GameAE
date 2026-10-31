using Game.PvE.SafeZone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.LifeSkills
{
    /// <summary>
    /// Tự động tạo Điểm Câu Cá (Fishing Spot) và Bếp Nấu Ăn (Cooking Pot) tại runtime,
    /// giúp người chơi trải nghiệm ngay mà không cần sửa file scene thủ công.
    /// </summary>
    public static class LifeSkillsRuntimeSpawner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (scene, mode) => TrySpawnLifeSkillSpots();
            TrySpawnLifeSkillSpots();
        }

        public static void TrySpawnLifeSkillSpots()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;

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

            // 1. Tạo Điểm Câu Cá (Fishing Spot)
            if (Object.FindAnyObjectByType<FishingSpotInteractable>() == null)
            {
                var fishGo = new GameObject("FishingSpot_LakeLake");
                fishGo.transform.position = centerPos + new Vector3(-4f, -2f, 0f);

                var col = fishGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 2.5f;

                fishGo.AddComponent<FishingSpotInteractable>();

                var sr = fishGo.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.3f, 0.7f, 1f, 0.9f); // Màu xanh lam nước
                sr.sortingOrder = 1;
            }

            // 2. Tạo Bếp Nấu Ăn (Cooking Pot)
            if (Object.FindAnyObjectByType<CookingPotInteractable>() == null)
            {
                var potGo = new GameObject("CookingPot_Campfire");
                potGo.transform.position = centerPos + new Vector3(4f, -1.5f, 0f);

                var col = potGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 2.0f;

                potGo.AddComponent<CookingPotInteractable>();

                var sr = potGo.AddComponent<SpriteRenderer>();
                sr.color = new Color(1f, 0.5f, 0.2f, 1f); // Màu cam lửa
                sr.sortingOrder = 2;
            }
        }
    }
}

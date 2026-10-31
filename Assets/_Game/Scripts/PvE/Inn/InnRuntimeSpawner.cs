using Game.PvE.SafeZone;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.PvE.Inn
{
    /// <summary>
    /// Tự động khởi tạo NPC Quán trọ tại runtime trong các scene PvE, không cần sửa scene thủ công.
    /// Đặt NPC ở gần Vùng an toàn (SafeZone) của thị trấn.
    /// </summary>
    public static class InnRuntimeSpawner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (scene, mode) => TrySpawnInn();
            TrySpawnInn();
        }

        public static void TrySpawnInn()
        {
            // Chỉ spawn trong scene gameplay, không spawn trong Boot hay MainMenu
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;

            // Kiểm tra xem đã có quán trọ nào chưa
            if (Object.FindAnyObjectByType<InnInteractable>() != null) return;

            // Tìm vị trí đặt quán trọ: ưu tiên gần SafeZoneTrigger, nếu không có thì gần điểm spawn của Player
            Vector3 spawnPos = Vector3.zero;
            var safeZone = Object.FindAnyObjectByType<SafeZoneTrigger>();
            if (safeZone != null)
            {
                spawnPos = safeZone.transform.position + new Vector3(3f, 1f, 0f);
            }
            else
            {
                var player = Object.FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                {
                    spawnPos = player.transform.position + new Vector3(3f, 0f, 0f);
                }
            }

            // Tạo GameObject NPC Quán trọ
            var innGo = new GameObject("InnKeeper_WhitePigeon");
            innGo.transform.position = spawnPos;

            var col = innGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.0f;

            innGo.AddComponent<InnInteractable>();

            // Tạo Sprite visual đơn giản (màu vàng ấm)
            var sr = innGo.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.95f, 0.85f, 0.4f, 1f);
            sr.sortingOrder = 2;
        }
    }
}

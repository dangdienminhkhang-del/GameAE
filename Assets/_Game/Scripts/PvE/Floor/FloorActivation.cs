using System;
using System.Collections.Generic;
using Game.Core.Floor;
using Game.PvE.Player;
using UnityEngine;
using CameraFollow = Game.PvE.Camera.CameraFollow;

namespace Game.PvE.Floor
{
    /// <summary>
    /// Chuyển tầng ở mức scene (generic, dùng chung cho mọi tầng):
    /// tắt nội dung mọi tầng khác, bật nội dung tầng đích, đặt người chơi vào điểm vào, snap camera.
    ///
    /// Phần chọn binder và quyết định bật/tắt là hàm thuần (<see cref="BuildPlan"/>, <see cref="FindBinder"/>)
    /// nên test được trong EditMode. Không có nhánh theo số tầng — chỉ so khớp floorId của dữ liệu.
    /// </summary>
    public static class FloorActivation
    {
        /// <summary>Kết quả quyết định (thuần) cho một lần chuyển tầng.</summary>
        public sealed class Plan
        {
            /// <summary>Binder của tầng đích; null nếu không có binder khớp.</summary>
            public FloorSceneBinder Target;

            /// <summary>Các binder khác đang bật cần tắt.</summary>
            public readonly List<FloorSceneBinder> ToDeactivate = new List<FloorSceneBinder>();

            /// <summary>Có gọi Activate trên tầng đích không.</summary>
            public bool ShouldActivate;

            /// <summary>Có đặt người chơi vào điểm vào không.</summary>
            public bool ShouldPlacePlayer;
        }

        // ── Hàm thuần ───────────────────────────────────────────────────────

        /// <summary>Tìm binder theo floorId (so sánh ordinal). Bỏ qua binder null/đã hủy.</summary>
        public static FloorSceneBinder FindBinder(IReadOnlyList<FloorSceneBinder> binders, string floorId)
        {
            if (binders == null || string.IsNullOrEmpty(floorId)) return null;
            for (int i = 0; i < binders.Count; i++)
            {
                var binder = binders[i];
                if (binder == null) continue;
                if (string.Equals(binder.FloorId, floorId, StringComparison.Ordinal)) return binder;
            }
            return null;
        }

        /// <summary>
        /// Quyết định bật/tắt, không phụ thuộc "tầng hiện tại" nào:
        /// mọi binder khác đang bật đều bị tắt.
        /// Khi <paramref name="placePlayer"/> = false (đang đứng đúng tầng đó) thì chỉ bật lại nếu nội dung đang tắt,
        /// không reset nội dung đang chạy và không dịch chuyển người chơi.
        ///
        /// "Đang đứng đúng tầng" còn được nhận ra từ chính trạng thái binder, không cần cờ placePlayer:
        /// binder đích ĐANG bật và là binder duy nhất đang bật → không bật lại, không đặt người chơi,
        /// kể cả khi placePlayer = true (vd. tầng khởi đầu nằm sẵn trong scene host mà loader chưa biết tầng hiện tại).
        /// Đích đang bật nhưng còn binder khác đang bật (vừa nạp scene mới, nội dung lưu mặc định bật) → vẫn là chuyển tầng thật.
        /// Đích đang tắt → chuyển tầng như thường.
        /// </summary>
        public static Plan BuildPlan(IReadOnlyList<FloorSceneBinder> binders, string floorId, bool placePlayer)
        {
            var plan = new Plan { Target = FindBinder(binders, floorId) };
            if (plan.Target == null) return plan;

            for (int i = 0; i < binders.Count; i++)
            {
                var binder = binders[i];
                if (binder == null || binder == plan.Target) continue;
                if (binder.IsActive) plan.ToDeactivate.Add(binder);
            }

            // Đích đang bật và không còn binder nào khác bật → người chơi đã đứng ở tầng này: không làm gì.
            bool alreadyStandingHere = plan.Target.IsActive && plan.ToDeactivate.Count == 0;
            if (alreadyStandingHere)
            {
                plan.ShouldActivate = false;
                plan.ShouldPlacePlayer = false;
                return plan;
            }

            plan.ShouldActivate = placePlayer || !plan.Target.IsActive;
            plan.ShouldPlacePlayer = placePlayer && plan.Target.EntryPoint != null;
            return plan;
        }

        /// <summary>
        /// Đặt người chơi tại điểm vào và xóa vận tốc Rigidbody2D (để không trượt khỏi điểm vào).
        /// Giữ nguyên z của người chơi.
        /// </summary>
        public static bool PlacePlayerAt(Transform player, Transform entryPoint)
        {
            if (player == null || entryPoint == null) return false;

            Vector3 position = entryPoint.position;
            position.z = player.position.z;
            player.position = position;

            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = new Vector2(position.x, position.y);
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            return true;
        }

        // ── Thực thi ────────────────────────────────────────────────────────

        /// <summary>
        /// Vào tầng theo định nghĩa. Không có binder khớp → cảnh báo, trả false, không đổi gì.
        /// <paramref name="placePlayer"/> = false dùng khi người chơi đã đứng đúng tầng này.
        /// </summary>
        public static bool ActivateFloor(FloorDefinition def, bool placePlayer = true)
        {
            return ActivateFloor(def, FloorSceneBinder.All, placePlayer, FindPlayerTransform, SnapCameraToPlayer);
        }

        /// <summary>Bản có inject (binder, cách tìm người chơi, bước sau khi đặt) để test không đụng scene thật.</summary>
        public static bool ActivateFloor(
            FloorDefinition def,
            IReadOnlyList<FloorSceneBinder> binders,
            bool placePlayer,
            Func<Transform> findPlayer,
            Action afterPlace)
        {
            if (def == null)
            {
                Debug.LogWarning("[FloorActivation] FloorDefinition null — không đổi gì.");
                return false;
            }

            // Sao chép để việc Activate/Deactivate không ảnh hưởng danh sách đang duyệt.
            var snapshot = binders != null ? new List<FloorSceneBinder>(binders) : new List<FloorSceneBinder>();
            var plan = BuildPlan(snapshot, def.id, placePlayer);

            if (plan.Target == null)
            {
                Debug.LogWarning($"[FloorActivation] Không có FloorSceneBinder cho tầng '{def.id}' — không đổi gì.");
                return false;
            }

            foreach (var other in plan.ToDeactivate) other.Deactivate();

            if (plan.ShouldActivate) plan.Target.Activate(def);

            if (plan.ShouldPlacePlayer)
            {
                var player = findPlayer != null ? findPlayer() : null;
                if (PlacePlayerAt(player, plan.Target.EntryPoint))
                {
                    afterPlace?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[FloorActivation] Không đặt được người chơi vào tầng '{def.id}' (thiếu người chơi hoặc điểm vào).");
                }
            }

            return true;
        }

        private static Transform FindPlayerTransform()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            return player != null ? player.transform : null;
        }

        private static void SnapCameraToPlayer()
        {
            var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            if (follow != null) follow.SnapToTarget();
        }
    }
}

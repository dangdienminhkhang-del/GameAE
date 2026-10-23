using System.Collections.Generic;
using Game.Services;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Audio
{
    /// <summary>
    /// Phát SFX bấm nút cho mọi Button con (docs/05 Phase 9.5). Lúc bật, thêm listener bằng code vào các Button đang có —
    /// KHÔNG sửa prefab/scene từng nút. Nút sinh ra sau đó được phủ tự động khi nó là CON TRỰC TIẾP của object mang
    /// script này (Unity chỉ báo <see cref="OnTransformChildrenChanged"/> cho con trực tiếp). Nút nằm sâu hơn
    /// (ví dụ trong Content của danh sách cuộn): đặt thêm một UiSfxRouter lên chính object chứa chúng, hoặc gọi
    /// <see cref="Rescan"/> sau khi tạo.
    /// Đặt ở gốc MỖI Canvas có Button. Tên nút chứa back/close/cancel → sfx.ui_back, còn lại → sfx.ui_click.
    /// </summary>
    public sealed class UiSfxRouter : MonoBehaviour
    {
        [Tooltip("Bỏ qua nút có tên chứa các mảnh này (không phân biệt hoa thường). Mặc định bỏ nút hành động chiến đấu vì đã có SFX riêng.")]
        [SerializeField] private string[] skipNameContains = { "Attack", "Dodge", "Technique", "Skill" };

        private sealed class Binding
        {
            public Button Button;
            public UnityAction Action;
        }

        private readonly List<Binding> _bindings = new List<Binding>();
        // Danh sách tạm dùng lại cho mọi lần quét: không cấp phát mảng mới mỗi lần.
        private readonly List<Button> _scratch = new List<Button>();

        private void OnEnable()
        {
            Rescan();
        }

        private void OnDisable()
        {
            Unbind();
        }

        // Rescan không đổi cây transform nên không tự gọi lại chính nó. Chặn khi đang tắt: OnDisable đã gỡ hết listener,
        // gắn thêm lúc này sẽ không còn ai gỡ.
        private void OnTransformChildrenChanged()
        {
            if (!isActiveAndEnabled) return;
            Rescan();
        }

        /// <summary>Quét lại các Button con (kể cả đang tắt) và gắn listener cho nút chưa gắn.</summary>
        public void Rescan()
        {
            PruneDestroyed();

            GetComponentsInChildren(true, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Button button = _scratch[i];
                if (button == null || IsBound(button)) continue;
                if (UiSfxRules.ShouldSkip(button.name, skipNameContains)) continue;

                string id = UiSfxRules.PickSfxId(button.name);
                UnityAction action = () => ServiceProvider.GetAudioService().PlaySfx(id);
                button.onClick.AddListener(action);
                _bindings.Add(new Binding { Button = button, Action = action });
            }
            _scratch.Clear();
        }

        private bool IsBound(Button button)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Button == button) return true;
            }
            return false;
        }

        // Nút đã bị hủy thì bỏ binding của nó, để danh sách không phình dần khi nút được sinh/hủy liên tục.
        private void PruneDestroyed()
        {
            for (int i = _bindings.Count - 1; i >= 0; i--)
            {
                if (_bindings[i].Button == null) _bindings.RemoveAt(i);
            }
        }

        private void Unbind()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Button != null) _bindings[i].Button.onClick.RemoveListener(_bindings[i].Action);
            }
            _bindings.Clear();
        }
    }
}

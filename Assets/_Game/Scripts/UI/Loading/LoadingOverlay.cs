using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Loading
{
    /// <summary>
    /// Màn tải nhẹ (P9.6): thanh tiến độ + một dòng chữ. Dùng khi Boot → MainMenu và MainMenu → gameplay.
    /// Là <see cref="IProgress{T}"/> nên đưa thẳng vào SceneLoader.LoadSceneAsync(..., progress).
    /// Không có splash riêng (dùng splash của Player Settings). Chỉ hiển thị, không chứa logic game.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour, IProgress<float>
    {
        public const string DefaultMessage = "Đang tải...";

        [Tooltip("Đối tượng gốc của overlay (bật khi tải, tắt khi xong). Thiếu thì dùng chính GameObject này.")]
        [SerializeField] private GameObject root;
        [Tooltip("Thanh tiến độ: Image kiểu Filled (Fill Method Horizontal). Không bắt buộc.")]
        [SerializeField] private Image fillImage;
        [Tooltip("Dòng chữ (ví dụ 'Đang tải...'). Không bắt buộc.")]
        [SerializeField] private Text messageText;

        private float _value;

        private void Awake()
        {
            if (root == null) root = gameObject;
        }

        public bool IsShowing => root != null && root.activeSelf;

        public void Show(string message = DefaultMessage)
        {
            if (root == null) root = gameObject;
            _value = 0f;
            ApplyValue();
            if (messageText != null) messageText.text = string.IsNullOrEmpty(message) ? DefaultMessage : message;
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>Nhận tiến độ 0..1 từ SceneLoader (đã đơn điệu tăng). Giá trị lùi hoặc ngoài khoảng được bỏ qua/kẹp.</summary>
        public void Report(float value)
        {
            if (float.IsNaN(value)) return;
            value = Mathf.Clamp01(value);
            if (value < _value) return;
            _value = value;
            ApplyValue();
        }

        private void ApplyValue()
        {
            if (fillImage != null) fillImage.fillAmount = _value;
        }
    }
}

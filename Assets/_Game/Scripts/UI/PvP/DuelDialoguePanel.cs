using Game.Core.Events;
using Game.Core.PvP;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.PvP
{
    /// <summary>
    /// Bảng giao diện chọn chế độ Đấu Tay Đôi (Duel SAO).
    /// Dựng bằng code thuần dưới Canvas lúc chạy, không cần prefab.
    /// </summary>
    public class DuelDialoguePanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 38;

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color SelectedColor = new Color(0.2f, 0.6f, 0.85f, 1f);
        private static readonly Color UnselectedColor = new Color(0.2f, 0.22f, 0.3f, 1f);
        private static readonly Color StartBtnColor = new Color(0.85f, 0.35f, 0.15f, 1f);

        private Font _font;
        private GameObject _root;
        private Text _titleText;
        private Text _descText;
        private DuelMode _selectedMode = DuelMode.HalfLoss;

        private Button _btnFirstStrike;
        private Button _btnHalfLoss;
        private Button _btnTotalLoss;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private static DuelDialoguePanel s_instance;
        public static DuelDialoguePanel Instance => s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            SceneManager.sceneLoaded += (scene, mode) => EnsureOnCanvas();
            EnsureOnCanvas();
        }

        public static void EnsureOnCanvas()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;
            if (s_instance != null) return;

            var canvas = GameObject.Find("/Canvas");
            if (canvas == null)
            {
                var canvasComp = FindAnyObjectByType<Canvas>();
                if (canvasComp != null) canvas = canvasComp.gameObject;
            }

            if (canvas != null && canvas.transform.Find("DuelDialoguePanel") == null)
            {
                var panelGo = new GameObject("DuelDialoguePanel", typeof(RectTransform), typeof(DuelDialoguePanel));
                panelGo.transform.SetParent(canvas.transform, false);
            }
        }

        private void Awake()
        {
            s_instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
            GameEvents.OnDuelRequested += Open;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            GameEvents.OnDuelRequested -= Open;
        }

        private void Update()
        {
            if (!IsOpen) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            SelectMode(DuelMode.HalfLoss);
            if (_root != null) _root.SetActive(true);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void SelectMode(DuelMode mode)
        {
            _selectedMode = mode;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            SetButtonColor(_btnFirstStrike, _selectedMode == DuelMode.FirstStrike);
            SetButtonColor(_btnHalfLoss, _selectedMode == DuelMode.HalfLoss);
            SetButtonColor(_btnTotalLoss, _selectedMode == DuelMode.TotalLoss);

            if (_descText != null)
            {
                switch (_selectedMode)
                {
                    case DuelMode.FirstStrike:
                        _descText.text = "Quy tắc [Đòn Đầu Tiên]: Bên nào ra đòn trúng đích trước sẽ thắng ngay lập tức. Phù hợp tỉ thí phản xạ tốc độ.";
                        break;
                    case DuelMode.HalfLoss:
                        _descText.text = "Quy tắc [Nửa Máu]: Bên nào tụt xuống dưới 50% HP sẽ xử thua cuộc. Đây là chế độ thi đấu an toàn chuẩn mực nhất của SAO.";
                        break;
                    case DuelMode.TotalLoss:
                        _descText.text = "Quy tắc [Toàn Lực]: Đánh tới khi 1 bên cạn 100% HP. Hệ thống bảo hộ sẽ can thiệp để người thua KHÔNG chết vĩnh viễn.";
                        break;
                }
            }
        }

        private void SetButtonColor(Button btn, bool selected)
        {
            if (btn != null && btn.targetGraphic is Image img)
            {
                img.color = selected ? SelectedColor : UnselectedColor;
            }
        }

        private void OnStartDuelClicked()
        {
            GameEvents.RaiseDuelStartRequested(_selectedMode);
            Close();
        }

        private void Build()
        {
            _root = NewUi("DuelDialoguePanelRoot", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject border = NewUi("Border", _root.transform);
            SetAnchors(border.GetComponent<RectTransform>(), 0.2f, 0.2f, 0.8f, 0.8f);
            border.AddComponent<PanelSizeClamp>();
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = Gold;

            Button topClose = NewButton("TopClose", border.transform, "X", 32, new Color(0.85f, 0.25f, 0.25f, 0.95f), Close);
            var tcRt = topClose.GetComponent<RectTransform>();
            tcRt.anchorMin = tcRt.anchorMax = tcRt.pivot = new Vector2(1f, 1f);
            tcRt.anchoredPosition = new Vector2(-8f, -8f);
            tcRt.sizeDelta = new Vector2(60f, 60f);

            GameObject win = NewUi("Window", border.transform);
            RectTransform wr = win.GetComponent<RectTransform>();
            Stretch(wr);
            wr.offsetMin = new Vector2(4, 4);
            wr.offsetMax = new Vector2(-4, -4);
            Image winImg = win.AddComponent<Image>();
            winImg.color = Ink;

            _titleText = NewText("Title", win.transform, "VÕ ĐÀI TỈ THÍ (DUEL)", 32, Gold, TextAnchor.MiddleLeft);
            SetAnchors(_titleText.rectTransform, 0.05f, 0.85f, 0.95f, 0.96f);

            // 3 nút chọn chế độ
            _btnFirstStrike = NewButton("BtnFirstStrike", win.transform, "1. Đòn Đầu Tiên (First-Strike)", 20, UnselectedColor, () => SelectMode(DuelMode.FirstStrike));
            SetAnchors(_btnFirstStrike.GetComponent<RectTransform>(), 0.05f, 0.68f, 0.95f, 0.82f);

            _btnHalfLoss = NewButton("BtnHalfLoss", win.transform, "2. Nửa Máu (Half-Loss - Chuẩn SAO)", 20, UnselectedColor, () => SelectMode(DuelMode.HalfLoss));
            SetAnchors(_btnHalfLoss.GetComponent<RectTransform>(), 0.05f, 0.52f, 0.95f, 0.66f);

            _btnTotalLoss = NewButton("BtnTotalLoss", win.transform, "3. Toàn Lực (Total-Loss - Không Chết Thật)", 20, UnselectedColor, () => SelectMode(DuelMode.TotalLoss));
            SetAnchors(_btnTotalLoss.GetComponent<RectTransform>(), 0.05f, 0.36f, 0.95f, 0.50f);

            // Khung mô tả luật
            GameObject descBox = NewUi("DescBox", win.transform);
            SetAnchors(descBox.GetComponent<RectTransform>(), 0.05f, 0.18f, 0.95f, 0.32f);
            descBox.AddComponent<Image>().color = RowBg;

            _descText = NewText("DescText", descBox.transform, "", 18, Color.white, TextAnchor.MiddleCenter);
            Stretch(_descText.rectTransform);
            _descText.rectTransform.offsetMin = new Vector2(10, 5);
            _descText.rectTransform.offsetMax = new Vector2(-10, -5);

            // Nút Bắt đầu
            var startBtn = NewButton("StartBtn", win.transform, "Bắt Đầu Tỉ Thí", 24, StartBtnColor, OnStartDuelClicked);
            SetAnchors(startBtn.GetComponent<RectTransform>(), 0.1f, 0.04f, 0.48f, 0.15f);

            // Nút Rời đi
            var leaveBtn = NewButton("LeaveBtn", win.transform, "Rời Đi", 24, new Color(0.35f, 0.35f, 0.4f, 1f), Close);
            SetAnchors(leaveBtn.GetComponent<RectTransform>(), 0.52f, 0.04f, 0.9f, 0.15f);
        }

        private GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = NewUi(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var text = NewText("Label", go.transform, label, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

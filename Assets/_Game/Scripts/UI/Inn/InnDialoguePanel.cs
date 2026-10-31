using Game.Core.Events;
using Game.Core.Inn;
using Game.PvE.Items;
using Game.PvE.Player;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Inn
{
    /// <summary>
    /// Bảng giao diện Quán trọ (Inn) theo phong cách Sword Art Online.
    /// Dựng bằng code thuần dưới Canvas lúc chạy, không cần prefab.
    /// Lắng nghe GameEvents.OnInnRequested để mở khi player tương tác.
    /// </summary>
    public class InnDialoguePanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 36;

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color RestBtnColor = new Color(0.18f, 0.65f, 0.35f, 1f);
        private static readonly Color CloseBtnColor = new Color(0.35f, 0.35f, 0.42f, 1f);

        private Font _font;
        private GameObject _root;
        private Text _titleText;
        private Text _colText;
        private Text _dialogueText;
        private Text _statusText;
        private Button _restButton;
        private Text _restButtonText;
        private Button _closeButton;

        private InnRequestArgs _activeRequest;
        private PlayerWallet _playerWallet;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private static InnDialoguePanel s_instance;
        public static InnDialoguePanel Instance => s_instance;

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

            if (canvas != null && canvas.transform.Find("InnDialoguePanel") == null)
            {
                var panelGo = new GameObject("InnDialoguePanel", typeof(RectTransform), typeof(InnDialoguePanel));
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
            GameEvents.OnInnRequested += HandleInnRequested;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            GameEvents.OnInnRequested -= HandleInnRequested;
            if (_playerWallet != null)
            {
                _playerWallet.Changed -= RefreshWallet;
            }
        }

        private void HandleInnRequested(InnRequestArgs args)
        {
            if (args == null) return;
            Open(args);
        }

        private void Update()
        {
            if (!IsOpen) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Tự đóng nếu player di chuyển quá xa quán trọ
            if (_activeRequest != null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    float dist = Vector2.Distance(player.transform.position, _activeRequest.Position);
                    if (dist > 6f)
                    {
                        Close();
                    }
                }
            }
        }

        public void Open(InnRequestArgs request)
        {
            _activeRequest = request;

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                if (_playerWallet != null) _playerWallet.Changed -= RefreshWallet;
                _playerWallet = player.GetComponent<PlayerWallet>();
                if (_playerWallet != null) _playerWallet.Changed += RefreshWallet;
            }

            RefreshWallet();

            int cost = _activeRequest != null ? _activeRequest.RestCost : InnRules.DefaultRestCost;
            string innName = _activeRequest != null ? _activeRequest.InnName : "Quán trọ Thị trấn";

            if (_titleText != null) _titleText.text = innName;
            if (_dialogueText != null)
            {
                _dialogueText.text = $"\"Chào mừng quý khách đến với {innName}!\n\n" +
                                     $"Chỉ với {cost} Col, quý khách có thể trọ lại một đêm yên bình.\n" +
                                     $"Thể lực sẽ hồi phục 100% và trời sẽ sáng tỏ khi quý khách thức giấc.\"";
            }
            if (_statusText != null) _statusText.text = "";
            if (_restButtonText != null) _restButtonText.text = $"Nghỉ ngơi ({cost} Col)";

            if (_root != null) _root.SetActive(true);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            if (_playerWallet != null) _playerWallet.Changed -= RefreshWallet;
            _activeRequest = null;
        }

        private void RefreshWallet()
        {
            if (_colText != null)
            {
                int currentCol = _playerWallet != null ? _playerWallet.Col : 0;
                _colText.text = $"{currentCol} Col";
            }
        }

        private void OnRestClicked()
        {
            if (_activeRequest == null || _activeRequest.OnExecuteRest == null) return;

            InnRestResult result = _activeRequest.OnExecuteRest.Invoke();
            if (_statusText != null)
            {
                _statusText.text = InnRules.GetResultMessage(result, _activeRequest.RestCost);
                _statusText.color = (result == InnRestResult.Success) ? Color.green : new Color(1f, 0.4f, 0.4f);
            }

            if (result == InnRestResult.Success)
            {
                RefreshWallet();
                Invoke(nameof(Close), 1.2f);
            }
        }

        private void Build()
        {
            _root = NewUi("InnDialoguePanelRoot", transform);
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

            _titleText = NewText("Title", win.transform, "Quán trọ", 34, Gold, TextAnchor.MiddleLeft);
            SetAnchors(_titleText.rectTransform, 0.05f, 0.85f, 0.65f, 0.96f);

            _colText = NewText("Col", win.transform, "0 Col", 30, Gold, TextAnchor.MiddleRight);
            SetAnchors(_colText.rectTransform, 0.65f, 0.85f, 0.95f, 0.96f);

            // Khung hội thoại
            GameObject dialogueBox = NewUi("DialogueBox", win.transform);
            SetAnchors(dialogueBox.GetComponent<RectTransform>(), 0.05f, 0.35f, 0.95f, 0.82f);
            Image dlgImg = dialogueBox.AddComponent<Image>();
            dlgImg.color = RowBg;

            _dialogueText = NewText("DialogueText", dialogueBox.transform, "", 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(_dialogueText.rectTransform);
            _dialogueText.rectTransform.offsetMin = new Vector2(16, 16);
            _dialogueText.rectTransform.offsetMax = new Vector2(-16, -16);

            _statusText = NewText("StatusText", win.transform, "", 22, Color.yellow, TextAnchor.MiddleCenter);
            SetAnchors(_statusText.rectTransform, 0.05f, 0.22f, 0.95f, 0.32f);

            // Nút Nghỉ ngơi
            _restButton = NewButton("RestButton", win.transform, "Nghỉ ngơi (50 Col)", 26, RestBtnColor, OnRestClicked);
            SetAnchors(_restButton.GetComponent<RectTransform>(), 0.1f, 0.06f, 0.48f, 0.20f);
            _restButtonText = _restButton.GetComponentInChildren<Text>();

            // Nút Rời đi
            _closeButton = NewButton("LeaveButton", win.transform, "Rời đi", 26, CloseBtnColor, Close);
            SetAnchors(_closeButton.GetComponent<RectTransform>(), 0.52f, 0.06f, 0.9f, 0.20f);
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

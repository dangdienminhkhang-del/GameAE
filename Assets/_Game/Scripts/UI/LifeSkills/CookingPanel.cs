using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.LifeSkills;
using Game.PvE.Items;
using Game.PvE.LifeSkills;
using Game.PvE.Player;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.LifeSkills
{
    /// <summary>
    /// Bảng giao diện Nấu Ăn (Cooking) theo phong cách Asuna trong SAO.
    /// Tự dựng toàn bộ UI bằng code thuần dưới Canvas lúc chạy, không cần prefab.
    /// Kế thừa IExclusivePanel để hỗ trợ phím Back trên mobile và đóng độc quyền.
    /// </summary>
    public class CookingPanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 37;

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color CookBtnColor = new Color(0.85f, 0.45f, 0.15f, 1f);
        private static readonly Color DisabledBtnColor = new Color(0.3f, 0.3f, 0.35f, 1f);

        private Font _font;
        private GameObject _root;
        private Text _titleText;
        private Text _skillLevelText;
        private Transform _recipeListContainer;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private static CookingPanel s_instance;
        public static CookingPanel Instance => s_instance;

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

            if (canvas != null && canvas.transform.Find("CookingPanel") == null)
            {
                var panelGo = new GameObject("CookingPanel", typeof(RectTransform), typeof(CookingPanel));
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
            GameEvents.OnCookingRequested += Open;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            GameEvents.OnCookingRequested -= Open;
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
            RefreshUI();
            if (_root != null) _root.SetActive(true);
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void RefreshUI()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;

            var lifeSkills = player.GetComponent<PlayerLifeSkills>();
            var inventory = player.GetComponent<PlayerInventory>();

            int cookingProf = lifeSkills != null ? lifeSkills.CookingProficiency : 0;
            int cookingLv = lifeSkills != null ? lifeSkills.CookingLevel : 1;

            if (_skillLevelText != null)
            {
                _skillLevelText.text = $"Cấp Nấu Ăn: {cookingLv} ({cookingProf} EXP)";
            }

            // Xóa các row công thức cũ
            foreach (Transform child in _recipeListContainer)
            {
                Destroy(child.gameObject);
            }

            // Dựng lại danh sách công thức
            var recipes = LifeSkillRules.GetDefaultRecipes();
            for (int i = 0; i < recipes.Count; i++)
            {
                BuildRecipeRow(recipes[i], cookingProf, inventory, lifeSkills, player);
            }
        }

        private void BuildRecipeRow(CookingRecipe recipe, int cookingProf, PlayerInventory inventory, PlayerLifeSkills lifeSkills, PlayerController player)
        {
            int owned = 0;
            if (inventory != null && inventory.Inventory != null)
            {
                owned = inventory.Inventory.GetStack(recipe.requiredIngredientId);
            }

            bool canCook = LifeSkillRules.CanCook(recipe, cookingProf, owned);

            var rowGo = NewUi($"Recipe_{recipe.id}", _recipeListContainer);
            var layout = rowGo.AddComponent<LayoutElement>();
            layout.minHeight = 70;
            layout.preferredHeight = 70;

            var img = rowGo.AddComponent<Image>();
            img.color = RowBg;

            // Tên món ăn & hiệu ứng
            string desc = $"{recipe.dishName} (Yêu cầu Cấp {recipe.requiredCookingLevel})\n" +
                          $"Cần: {recipe.requiredIngredientCount} Cá Tươi (Hiện có: {owned})  |  Hồi +{recipe.instantHeal:0} HP, +{recipe.attackBuff:0} ATK ({recipe.buffDurationSeconds:0}s)";
            var text = NewText("Desc", rowGo.transform, desc, 20, Color.white, TextAnchor.MiddleLeft);
            SetAnchors(text.rectTransform, 0.03f, 0.05f, 0.72f, 0.95f);

            // Nút Nấu
            Color btnColor = canCook ? CookBtnColor : DisabledBtnColor;
            var cookBtn = NewButton("CookBtn", rowGo.transform, "Nấu", 22, btnColor, () =>
            {
                if (!canCook) return;

                // Trừ nguyên liệu
                if (inventory != null && inventory.Inventory != null)
                {
                    inventory.Inventory.RemoveStack(recipe.requiredIngredientId, recipe.requiredIngredientCount);
                }

                // Hồi máu
                var health = player.GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.Heal(recipe.instantHeal);
                }

                // Thêm buff
                if (lifeSkills != null)
                {
                    lifeSkills.ApplyBuff(new CookingBuffInstance(recipe.dishName, recipe.attackBuff, recipe.buffDurationSeconds));
                    lifeSkills.AddCookingExp(LifeSkillRules.ExpPerCookingSuccess);
                }

                GameEvents.RaiseCombatNotice($"🍲 ĐÃ NẤU THÀNH CÔNG: {recipe.dishName}! (+{LifeSkillRules.ExpPerCookingSuccess} EXP Nấu)", Color.green);
                RefreshUI();
            });

            SetAnchors(cookBtn.GetComponent<RectTransform>(), 0.75f, 0.15f, 0.96f, 0.85f);
            cookBtn.interactable = canCook;
        }

        private void Build()
        {
            _root = NewUi("CookingPanelRoot", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject border = NewUi("Border", _root.transform);
            SetAnchors(border.GetComponent<RectTransform>(), 0.15f, 0.15f, 0.85f, 0.85f);
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

            _titleText = NewText("Title", win.transform, "Bếp Nấu Ăn (Cooking)", 34, Gold, TextAnchor.MiddleLeft);
            SetAnchors(_titleText.rectTransform, 0.05f, 0.88f, 0.55f, 0.98f);

            _skillLevelText = NewText("SkillLevel", win.transform, "Cấp Nấu Ăn: 1", 24, Color.yellow, TextAnchor.MiddleRight);
            SetAnchors(_skillLevelText.rectTransform, 0.55f, 0.88f, 0.95f, 0.98f);

            // Container danh sách công thức
            GameObject scrollGo = NewUi("ScrollView", win.transform);
            SetAnchors(scrollGo.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.85f);

            var vlg = scrollGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            _recipeListContainer = scrollGo.transform;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using Cysharp.Threading.Tasks;
using AnoGame.Data;
using AnoGame.Application.Inventory;

namespace AnoGame.SLFBDebug
{
    /// <summary>
    /// UI Toolkit ベースのデバッグパネルコントローラ。
    /// ツールバー、アイテムグリッド（フィルタ付き）、チャプター操作を管理する。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("AnoGame/Debug/DebugPanelController")]
    public class DebugPanelController : MonoBehaviour
    {
        // ─── Inspector ───
        [Header("Data")]
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Speed")]
        [SerializeField] private Unity.TinyCharacterController.Control.MoveControl moveControl;

        [Header("Save/Chapter")]
        [SerializeField] private string filePattern = "savedata_*.json";

        [Header("Consume")]
        [SerializeField, Min(1)] private int consumeQuantity = 1;

        // ─── DI ───
        private InventoryManager _inventoryManager;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
            Debug.Log("[DebugPanelController] Constructed via DI");
        }

        // ─── State ───
        private UIDocument _uiDocument;
        private VisualElement _root;
        private bool _isVisible;
        private float _defaultSpeed;

        // フィルターstate
        private Episode _selectedEpisode = Episode.None;
        private readonly HashSet<string> _activeTags = new();
        private string _searchText = "";

        // UIElement参照
        private VisualElement _itemGrid;
        private DropdownField _episodeDropdown;
        private VisualElement _tagButtonsContainer;
        private TextField _searchField;
        private DropdownField _chapterDropdown;

        // ─── Lifecycle ───
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                Debug.LogError("[DebugPanelController] UIDocument or rootVisualElement is null.");
                return;
            }

            _root = _uiDocument.rootVisualElement.Q<VisualElement>("debug-root");
            if (_root == null)
            {
                Debug.LogError("[DebugPanelController] debug-root not found in UXML.");
                return;
            }

            // 初期非表示
            _root.style.display = DisplayStyle.None;
            _isVisible = false;

            // 速度初期値を保存
            if (moveControl != null)
            {
                _defaultSpeed = moveControl.MoveSpeed;
            }

            SetupToolbar();
            SetupFilterBar();
            SetupItemGrid();
            SetupChapterDropdown();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Period))
            {
                Toggle();
            }
        }

        // ─── Toggle ───
        public void Toggle()
        {
            _isVisible = !_isVisible;
            _root.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            UnityEngine.Cursor.lockState = _isVisible ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = _isVisible;
        }

        // ═══════════════════════════════════════
        //  ツールバー
        // ═══════════════════════════════════════
        private void SetupToolbar()
        {
            var btnSpeedUp = _root.Q<Button>("btn-speed-up");
            var btnSpeedReset = _root.Q<Button>("btn-speed-reset");
            var btnBgmOff = _root.Q<Button>("btn-bgm-off");
            var btnSave = _root.Q<Button>("btn-save");

            btnSpeedUp?.RegisterCallback<ClickEvent>(_ =>
            {
                if (moveControl != null)
                {
                    moveControl.MoveSpeed *= 2;
                    Debug.Log($"[Debug] Speed: {moveControl.MoveSpeed}");
                }
            });

            btnSpeedReset?.RegisterCallback<ClickEvent>(_ =>
            {
                if (moveControl != null)
                {
                    moveControl.MoveSpeed = _defaultSpeed;
                    Debug.Log($"[Debug] Speed reset: {_defaultSpeed}");
                }
            });

            btnBgmOff?.RegisterCallback<ClickEvent>(_ =>
            {
                var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                foreach (var src in audioSources)
                {
                    if (src.clip != null && src.isPlaying)
                    {
                        src.Stop();
                    }
                }
                Debug.Log("[Debug] BGM stopped");
            });

            btnSave?.RegisterCallback<ClickEvent>(_ =>
            {
                Application.GameManager.Instance.SaveData();
                Debug.Log("[Debug] Saved");
            });
        }

        // ═══════════════════════════════════════
        //  フィルターバー
        // ═══════════════════════════════════════
        private void SetupFilterBar()
        {
            // エピソード DropDown
            _episodeDropdown = _root.Q<DropdownField>("dropdown-episode");
            if (_episodeDropdown != null)
            {
                var episodeNames = new List<string> { "All" };
                var episodeValues = Enum.GetValues(typeof(Episode));
                foreach (Episode ep in episodeValues)
                {
                    if (ep != Episode.None)
                        episodeNames.Add(ep.ToString());
                }
                _episodeDropdown.choices = episodeNames;
                _episodeDropdown.index = 0;
                _episodeDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == "All")
                        _selectedEpisode = Episode.None;
                    else
                        Enum.TryParse(evt.newValue, out _selectedEpisode);

                    RefreshGrid();
                });
            }

            // タグボタン
            _tagButtonsContainer = _root.Q<VisualElement>("tag-buttons");
            if (_tagButtonsContainer != null)
            {
                foreach (var tag in ItemTag.All)
                {
                    var btn = new Button { text = tag };
                    btn.AddToClassList("tag-btn");
                    btn.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (_activeTags.Contains(tag))
                        {
                            _activeTags.Remove(tag);
                            btn.RemoveFromClassList("tag-btn--active");
                        }
                        else
                        {
                            _activeTags.Add(tag);
                            btn.AddToClassList("tag-btn--active");
                        }
                        RefreshGrid();
                    });
                    _tagButtonsContainer.Add(btn);
                }
            }

            // 検索
            _searchField = _root.Q<TextField>("search-field");
            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _searchText = evt.newValue ?? "";
                    RefreshGrid();
                });
            }
        }

        // ═══════════════════════════════════════
        //  アイテムグリッド
        // ═══════════════════════════════════════
        private void SetupItemGrid()
        {
            _itemGrid = _root.Q<VisualElement>("item-grid");
            if (_itemGrid == null)
            {
                Debug.LogError("[DebugPanelController] item-grid not found.");
                return;
            }

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (_itemGrid == null || itemDatabase == null) return;

            _itemGrid.Clear();

            foreach (var item in itemDatabase.Items)
            {
                if (!PassFilter(item)) continue;
                _itemGrid.Add(CreateItemCard(item));
            }
        }

        private bool PassFilter(ItemData item)
        {
            // エピソードフィルタ
            if (_selectedEpisode != Episode.None && item.Episode != _selectedEpisode)
                return false;

            // タグフィルタ（AND: 選択されたタグを全て持つアイテムのみ表示）
            if (_activeTags.Count > 0)
            {
                foreach (var tag in _activeTags)
                {
                    if (!item.Tags.Contains(tag))
                        return false;
                }
            }

            // テキスト検索
            if (!string.IsNullOrEmpty(_searchText))
            {
                if (!item.ItemName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                    && !item.ItemId.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private VisualElement CreateItemCard(ItemData item)
        {
            var card = new VisualElement();
            card.AddToClassList("item-card");

            // エピソードバッジ
            if (item.Episode != Episode.None)
            {
                var badge = new VisualElement();
                badge.AddToClassList("item-card__episode-badge");
                var epLabel = new Label(item.Episode.ToString());
                epLabel.AddToClassList("item-card__episode-label");
                badge.Add(epLabel);
                card.Add(badge);
            }

            // アイテム名
            var nameLabel = new Label(item.ItemName);
            nameLabel.AddToClassList("item-card__name");
            nameLabel.tooltip = $"{item.ItemName}\nID: {item.ItemId}\nEp: {item.Episode}";
            card.Add(nameLabel);

            // ボタン行
            var btnRow = new VisualElement();
            btnRow.AddToClassList("item-card__buttons");

            var addBtn = new Button { text = "+" };
            addBtn.AddToClassList("item-card__btn");
            addBtn.RegisterCallback<ClickEvent>(_ =>
            {
                if (_inventoryManager == null) return;
                var ok = _inventoryManager.AddItem(item, 1);
                Debug.Log($"[Debug] Add: {item.ItemName} (ok={ok})");
            });

            var removeBtn = new Button { text = "-" };
            removeBtn.AddToClassList("item-card__btn");
            removeBtn.RegisterCallback<ClickEvent>(_ =>
            {
                if (_inventoryManager == null) return;
                var ok = _inventoryManager.RemoveItem(item.ItemName, 1);
                Debug.Log($"[Debug] Remove: {item.ItemName} (ok={ok})");
            });

            var consumeBtn = new Button { text = "消" };
            consumeBtn.AddToClassList("item-card__btn");
            consumeBtn.AddToClassList("item-card__btn--consume");
            consumeBtn.RegisterCallback<ClickEvent>(_ =>
            {
                if (_inventoryManager == null) return;
                var ok = _inventoryManager.RemoveItem(item.ItemName, consumeQuantity);
                Debug.Log($"[Debug] Consume: {item.ItemName} x{consumeQuantity} (ok={ok})");
            });

            btnRow.Add(addBtn);
            btnRow.Add(removeBtn);
            btnRow.Add(consumeBtn);
            card.Add(btnRow);

            return card;
        }

        // ═══════════════════════════════════════
        //  チャプター選択
        // ═══════════════════════════════════════
        private void SetupChapterDropdown()
        {
            _chapterDropdown = _root.Q<DropdownField>("dropdown-chapter");
            if (_chapterDropdown == null) return;

            var folderPath = UnityEngine.Application.persistentDataPath;
            var choices = new List<string> { "(なし)" };
            var filePaths = new List<string> { "" };

            try
            {
                var files = Directory.GetFiles(folderPath, filePattern);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string[] split = fileName.Split('_');
                    string display = split.Length >= 2 ? split[1] : fileName;
                    choices.Add(display);
                    filePaths.Add(file);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DebugPanelController] File scan error: {ex.Message}");
            }

            _chapterDropdown.choices = choices;
            _chapterDropdown.index = 0;

            _chapterDropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = choices.IndexOf(evt.newValue);
                if (idx <= 0 || idx >= filePaths.Count) return;

                string path = filePaths[idx];
                LoadChapterAsync(path).Forget();
            });
        }

        private async Cysharp.Threading.Tasks.UniTask LoadChapterAsync(string filePath)
        {
            try
            {
                var jsonMgr = new AnoGame.Infrastructure.Persistence.AsyncJsonDataManager();
                var data = await jsonMgr.LoadDataAsync<AnoGame.Domain.Data.Models.GameData>(filePath);
                if (data != null)
                {
                    Application.GameManager.Instance.UpdateGameState(data);
                    Debug.Log($"[Debug] Loaded: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Debug] Load error: {ex.Message}");
            }
        }
    }
}

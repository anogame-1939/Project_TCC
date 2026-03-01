using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using AnoGame.Application.Attributes;
using AnoGame.Data;

namespace AnoGame.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ItemSelectorAttribute))]
    public class ItemSelectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // PropertyDrawer はインスタンスが再利用されるため、
            // 状態はすべて VisualElement のローカル変数・クロージャで保持する
            string propertyPath = property.propertyPath;
            var serializedObject = property.serializedObject;

            // Data cache (local)
            var cachedIds = new List<string>();
            var cachedDisplayNames = new List<string>();
            var cachedAssets = new List<ItemData>();

            // UI State
            int filterEpisodeValue = -1; // -1 = All
            bool useGridView = true;

            // Load data
            LoadData(cachedIds, cachedDisplayNames, cachedAssets);

            var root = new VisualElement();
            root.style.marginBottom = 4;

            // ── Header ──
            var headerLabel = new Label("Item Selector");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 12;
            headerLabel.style.marginTop = 4;
            headerLabel.style.marginBottom = 4;
            root.Add(headerLabel);

            // ── Selection Section ──
            var idRow = new VisualElement();
            idRow.style.flexDirection = FlexDirection.Row;
            idRow.style.alignItems = Align.Center;
            idRow.style.overflow = Overflow.Hidden;

            var selectedField = new TextField("Selected Item");
            selectedField.isReadOnly = true;
            selectedField.SetEnabled(false);
            selectedField.style.flexGrow = 1;
            selectedField.style.flexShrink = 1;
            selectedField.style.overflow = Overflow.Hidden;
            selectedField.style.minWidth = 0;

            string currentId = property.stringValue;
            selectedField.value = ResolveDisplayName(currentId, cachedIds, cachedDisplayNames);
            idRow.Add(selectedField);

            var clearBtn = new Button(() =>
            {
                var prop = serializedObject.FindProperty(propertyPath);
                if (prop != null)
                {
                    prop.stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    selectedField.value = "";
                }
            });
            clearBtn.text = "Clear";
            clearBtn.style.width = 50;
            idRow.Add(clearBtn);
            root.Add(idRow);

            // Selection Info
            var selectionInfo = new HelpBox("", HelpBoxMessageType.None);
            root.Add(selectionInfo);

            // ── Separator ──
            root.Add(CreateSeparator());

            // ── Filter Section ──
            var filterRow = new VisualElement();
            filterRow.style.flexDirection = FlexDirection.Row;
            filterRow.style.alignItems = Align.Center;
            filterRow.style.marginBottom = 2;

            var filterLabel = new Label("Filter:");
            filterLabel.style.minWidth = 40;
            filterRow.Add(filterLabel);

            var episodeChoices = new List<string> { "All" };
            episodeChoices.AddRange(System.Enum.GetNames(typeof(Episode)));

            var episodeDropdown = new DropdownField("", episodeChoices, 0);
            episodeDropdown.style.flexGrow = 1;
            filterRow.Add(episodeDropdown);
            root.Add(filterRow);

            // ── Candidates Section ──
            var candidatesFoldout = new Foldout();
            candidatesFoldout.text = "Candidates (0)";
            candidatesFoldout.value = false;
            candidatesFoldout.style.marginTop = 4;
            root.Add(candidatesFoldout);

            var infoRow = new VisualElement();
            infoRow.style.flexDirection = FlexDirection.Row;
            infoRow.style.alignItems = Align.Center;

            var totalDataLabel = new Label("Total: 0");
            totalDataLabel.style.fontSize = 10;
            totalDataLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            infoRow.Add(totalDataLabel);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            infoRow.Add(spacer);

            var gridToggle = new Toggle("Grid");
            gridToggle.value = useGridView;
            infoRow.Add(gridToggle);
            candidatesFoldout.Add(infoRow);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.maxHeight = 200;
            scrollView.style.minHeight = 40;

            var candidateContainer = new VisualElement();
            candidateContainer.name = "item-candidate-list";
            scrollView.Add(candidateContainer);
            candidatesFoldout.Add(scrollView);

            // ── Refresh methods (closures) ──
            System.Action refreshSelectionInfo = () =>
            {
                var prop = serializedObject.FindProperty(propertyPath);
                string id = prop != null ? prop.stringValue : "";
                if (string.IsNullOrEmpty(id))
                {
                    selectionInfo.text = "アイテムを選択してください。";
                    selectionInfo.messageType = HelpBoxMessageType.Info;
                }
                else
                {
                    int idx = cachedIds.IndexOf(id);
                    if (idx >= 0 && cachedAssets[idx] != null)
                    {
                        var item = cachedAssets[idx];
                        string desc = !string.IsNullOrEmpty(item.Description) ? item.Description : "";
                        selectionInfo.text = $"ID: {item.ItemId}  Type: {item.ItemType}" +
                            (string.IsNullOrEmpty(desc) ? "" : $"\n{desc}");
                        selectionInfo.messageType = HelpBoxMessageType.None;
                    }
                    else
                    {
                        selectionInfo.text = $"不明な ItemId: {id}";
                        selectionInfo.messageType = HelpBoxMessageType.Warning;
                    }
                }
            };

            System.Action refreshCandidates = null;

            System.Action<string> selectItem = (id) =>
            {
                var prop = serializedObject.FindProperty(propertyPath);
                if (prop != null)
                {
                    prop.stringValue = id;
                    serializedObject.ApplyModifiedProperties();
                    selectedField.value = ResolveDisplayName(id, cachedIds, cachedDisplayNames);
                    refreshSelectionInfo();
                    refreshCandidates?.Invoke();
                }
            };

            refreshCandidates = () =>
            {
                candidateContainer.Clear();
                LoadData(cachedIds, cachedDisplayNames, cachedAssets);

                var prop = serializedObject.FindProperty(propertyPath);
                string selectedID = prop != null ? prop.stringValue : "";

                var filtered = GetFilteredItems(cachedAssets, filterEpisodeValue);

                candidatesFoldout.text = $"Candidates ({filtered.Count})";
                totalDataLabel.text = $"Total: {cachedIds.Count}";

                if (filtered.Count == 0)
                {
                    candidateContainer.Add(new HelpBox("候補が見つかりません。", HelpBoxMessageType.Info));
                    return;
                }

                if (useGridView)
                {
                    DrawGridCandidates(candidateContainer, filtered, selectedID,
                        cachedIds, cachedDisplayNames, cachedAssets, selectItem);
                }
                else
                {
                    DrawListCandidates(candidateContainer, filtered, selectedID,
                        cachedIds, cachedDisplayNames, cachedAssets, selectItem);
                }
            };

            // ── Register callbacks ──
            episodeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "All")
                {
                    filterEpisodeValue = -1;
                }
                else
                {
                    filterEpisodeValue = (int)(Episode)System.Enum.Parse(typeof(Episode), evt.newValue);
                }
                refreshCandidates();
            });

            gridToggle.RegisterValueChangedCallback(evt =>
            {
                useGridView = evt.newValue;
                refreshCandidates();
            });

            clearBtn.clicked += () =>
            {
                refreshSelectionInfo();
                refreshCandidates();
            };

            // Initial refresh
            root.schedule.Execute(() =>
            {
                refreshSelectionInfo();
                refreshCandidates();
            });

            return root;
        }

        // ────────────────────────────────────────────────
        // Data
        // ────────────────────────────────────────────────
        private static void LoadData(List<string> ids, List<string> displayNames, List<ItemData> assets)
        {
            ids.Clear();
            displayNames.Clear();
            assets.Clear();

            var guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    // ItemId が空の場合はアセット名をフォールバックIDとして使用
                    string id = !string.IsNullOrEmpty(item.ItemId) ? item.ItemId : item.name;
                    ids.Add(id);
                    string displayName = string.IsNullOrEmpty(item.ItemName)
                        ? item.name
                        : item.ItemName;
                    displayNames.Add(displayName);
                    assets.Add(item);
                }
            }
        }

        private static List<int> GetFilteredItems(List<ItemData> assets, int filterEpisodeValue)
        {
            var result = new List<int>();
            for (int i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                if (asset == null) continue;

                if (filterEpisodeValue != -1 && (int)asset.Episode != filterEpisodeValue)
                    continue;

                result.Add(i);
            }

            result.Sort((a, b) =>
            {
                var epA = assets[a] != null ? (int)assets[a].Episode : 0;
                var epB = assets[b] != null ? (int)assets[b].Episode : 0;
                if (epA != epB) return epA.CompareTo(epB);
                return string.Compare(
                    assets[a]?.ItemName ?? assets[a]?.name ?? "",
                    assets[b]?.ItemName ?? assets[b]?.name ?? "",
                    System.StringComparison.Ordinal);
            });

            return result;
        }

        private static string ResolveDisplayName(string id, List<string> ids, List<string> displayNames)
        {
            if (string.IsNullOrEmpty(id)) return "(None)";
            int idx = ids.IndexOf(id);
            if (idx >= 0) return displayNames[idx];
            return id;
        }

        private static string BuildTooltip(ItemData asset, string id)
        {
            if (asset == null) return id;
            string desc = !string.IsNullOrEmpty(asset.Description) ? asset.Description : "";
            return $"{asset.ItemId}\nType: {asset.ItemType}\nEpisode: {asset.Episode}" +
                (string.IsNullOrEmpty(desc) ? "" : $"\n{desc}");
        }

        private static string Truncate(string text, int maxLen)
        {
            return text.Length > maxLen ? text.Substring(0, maxLen) + ".." : text;
        }

        // ────────────────────────────────────────────────
        // Draw
        // ────────────────────────────────────────────────
        private static void DrawListCandidates(VisualElement container, List<int> indices, string selectedID,
            List<string> ids, List<string> displayNames, List<ItemData> assets, System.Action<string> onSelect)
        {
            if (!string.IsNullOrEmpty(selectedID))
            {
                int selIdx = ids.IndexOf(selectedID);
                if (selIdx >= 0 && indices.Contains(selIdx))
                {
                    container.Add(CreateSelectedButton(
                        ResolveDisplayName(selectedID, ids, displayNames), selectedID));
                }
            }

            Episode prevEp = (Episode)(-999);
            foreach (var idx in indices)
            {
                string id = ids[idx];
                var asset = assets[idx];
                string displayName = displayNames[idx];
                bool isSelected = id == selectedID;

                Episode curEp = asset != null ? asset.Episode : Episode.None;
                if (curEp != prevEp)
                {
                    container.Add(CreateEpisodeSeparator(curEp));
                }
                prevEp = curEp;

                if (isSelected)
                {
                    container.Add(CreateGhostButton(displayName, id));
                }
                else
                {
                    string capturedId = id;
                    var btn = new Button(() => onSelect(capturedId));
                    btn.text = displayName;
                    btn.tooltip = BuildTooltip(asset, id);
                    btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                    btn.style.marginBottom = 1;
                    btn.style.marginTop = 1;
                    container.Add(btn);
                }
            }
        }

        private static void DrawGridCandidates(VisualElement container, List<int> indices, string selectedID,
            List<string> ids, List<string> displayNames, List<ItemData> assets, System.Action<string> onSelect)
        {
            int maxCols = 3;

            if (!string.IsNullOrEmpty(selectedID))
            {
                int selIdx = ids.IndexOf(selectedID);
                if (selIdx >= 0 && indices.Contains(selIdx))
                {
                    string selDisplay = ResolveDisplayName(selectedID, ids, displayNames);
                    string selTruncated = Truncate(selDisplay, 12);
                    var selRow = new VisualElement();
                    selRow.style.flexDirection = FlexDirection.Row;
                    selRow.style.marginBottom = 2;

                    var selBtn = CreateSelectedButton(selTruncated, selectedID);
                    selBtn.style.flexGrow = 1;
                    selBtn.style.flexBasis = 0;
                    selBtn.style.marginRight = 2;
                    selRow.Add(selBtn);

                    container.Add(selRow);
                }
            }

            VisualElement row = null;
            int colIndex = 0;
            Episode prevEp = (Episode)(-999);

            foreach (var idx in indices)
            {
                string id = ids[idx];
                var asset = assets[idx];
                string displayName = displayNames[idx];
                bool isSelected = id == selectedID;

                Episode curEp = asset != null ? asset.Episode : Episode.None;
                if (curEp != prevEp)
                {
                    row = null;
                    colIndex = 0;
                    container.Add(CreateEpisodeSeparator(curEp));
                }
                prevEp = curEp;

                if (colIndex % maxCols == 0)
                {
                    row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 1;
                    container.Add(row);
                }

                string truncated = Truncate(displayName, 12);
                string tooltipText = BuildTooltip(asset, id);

                if (isSelected)
                {
                    var ghost = CreateGhostButton(truncated, tooltipText);
                    ghost.style.flexGrow = 1;
                    ghost.style.flexBasis = 0;
                    ghost.style.marginRight = 2;
                    row.Add(ghost);
                }
                else
                {
                    string capturedId = id;
                    var btn = new Button(() => onSelect(capturedId));
                    btn.text = truncated;
                    btn.tooltip = tooltipText;
                    btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                    btn.style.flexGrow = 1;
                    btn.style.flexBasis = 0;
                    btn.style.marginRight = 2;
                    row.Add(btn);
                }

                colIndex++;
            }
        }

        // ────────────────────────────────────────────────
        // Visual Helpers
        // ────────────────────────────────────────────────
        private static VisualElement CreateSeparator()
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            sep.style.marginTop = 4;
            sep.style.marginBottom = 4;
            return sep;
        }

        private static VisualElement CreateEpisodeSeparator(Episode episode)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginTop = 4;
            container.style.marginBottom = 2;

            string epText = episode == Episode.None ? "Default" : episode.ToString();
            var label = new Label(epText);
            label.style.fontSize = 9;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            label.style.marginRight = 4;
            label.style.minWidth = 30;
            container.Add(label);

            var line = new VisualElement();
            line.style.height = 1;
            line.style.flexGrow = 1;
            line.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            container.Add(line);

            return container;
        }

        private static Button CreateSelectedButton(string text, string id)
        {
            var btn = new Button();
            btn.text = text;
            btn.tooltip = id;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            btn.style.marginBottom = 1;
            btn.style.marginTop = 1;
            btn.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
            btn.style.color = Color.white;
            return btn;
        }

        private static VisualElement CreateGhostButton(string text, string tooltip)
        {
            var ghost = new Button();
            ghost.text = text;
            ghost.tooltip = tooltip;
            ghost.style.unityTextAlign = TextAnchor.MiddleLeft;
            ghost.style.marginBottom = 1;
            ghost.style.marginTop = 1;
            ghost.style.opacity = 0.35f;
            ghost.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
            ghost.style.color = new Color(1f, 1f, 1f, 0.5f);
            ghost.style.borderTopWidth = 1;
            ghost.style.borderBottomWidth = 1;
            ghost.style.borderLeftWidth = 1;
            ghost.style.borderRightWidth = 1;
            ghost.style.borderTopColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
            ghost.style.borderBottomColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
            ghost.style.borderLeftColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
            ghost.style.borderRightColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
            ghost.SetEnabled(false);
            return ghost;
        }
    }
}

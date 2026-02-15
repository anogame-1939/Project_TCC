using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using AnoGame.AnoDialogue.Timeline;

namespace AnoGame.AnoDialogue.Editor
{
    [CustomEditor(typeof(AnoNarrativeClip))]
    public class AnoNarrativeClipEditorUITK : UnityEditor.Editor
    {
        private MasterDialogueData _masterData;

        // Filter State
        private int _filterEp = -1;
        private int _filterCh = -1;
        private int _filterSec = -1;
        private bool _useGridView = true;

        // Cached filtered IDs
        private List<string> _filteredIDs = new List<string>();

        // UI references
        private VisualElement _candidateContainer;
        private Label _candidatesHeaderLabel;
        private Label _totalDataLabel;
        private HelpBox _selectionInfo;
        private TextField _convIDField;
        private Foldout _candidatesFoldout;
        private VisualElement _sceneImageSection;

        private const string PrefsKeyEp = "AnoNarrative_Filter_Ep";
        private const string PrefsKeyCh = "AnoNarrative_Filter_Ch";
        private const string PrefsKeySec = "AnoNarrative_Filter_Sec";

        public override VisualElement CreateInspectorGUI()
        {
            FindMasterData();

            var root = new VisualElement();
            root.style.paddingLeft = 0;
            root.style.paddingRight = 0;

            // ── Ano Narrative Clip Header ──
            var headerLabel = new Label("Ano Narrative Clip");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 13;
            headerLabel.style.marginTop = 4;
            headerLabel.style.marginBottom = 6;
            root.Add(headerLabel);

            // ── Pause Timeline ──
            var pauseProp = serializedObject.FindProperty("pauseTimeline");
            if (pauseProp != null)
            {
                var pauseField = new PropertyField(pauseProp, "Pause Timeline");
                root.Add(pauseField);
            }

            // ── Style Database ──
            BuildStyleSection(root);

            // ── Scene Image ──
            BuildSceneImageSection(root);

            // ── Separator ──
            root.Add(CreateSeparator());

            // ── Selection Section ──
            BuildSelectionSection(root);

            // ── Filter Section ──
            BuildFilterSection(root);

            // ── Candidates Section ──
            BuildCandidatesSection(root);

            // Initial data refresh
            root.schedule.Execute(() =>
            {
                LoadFilters();
                UpdateFilteredList();
                RefreshCandidateButtons();
                RefreshSelectionInfo();
            });

            return root;
        }

        // ────────────────────────────────────────────────
        // Style Database & Dialogue Style
        // ────────────────────────────────────────────────
        private void BuildStyleSection(VisualElement root)
        {
            var styleDbProp = serializedObject.FindProperty("styleDatabase");
            if (styleDbProp == null) return;

            var dbField = new PropertyField(styleDbProp, "Style Database");
            root.Add(dbField);

            // Dialogue Style dropdown (rebuilt when DB changes)
            var styleContainer = new VisualElement();
            styleContainer.name = "style-dropdown-container";
            root.Add(styleContainer);

            dbField.RegisterValueChangeCallback(evt =>
            {
                RebuildStyleDropdown(styleContainer);
            });

            // Auto-assign if empty
            if (styleDbProp.objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DialogueStyle");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var db = AssetDatabase.LoadAssetAtPath<AnoGame.AnoDialogue.Data.DialogueStyle>(path);
                    if (db != null)
                    {
                        styleDbProp.objectReferenceValue = db;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            // Deferred build (after binding)
            styleContainer.schedule.Execute(() => RebuildStyleDropdown(styleContainer));
        }

        private void RebuildStyleDropdown(VisualElement container)
        {
            container.Clear();

            var styleDbProp = serializedObject.FindProperty("styleDatabase");
            var styleNameProp = serializedObject.FindProperty("dialogueStyleName");
            if (styleDbProp == null || styleNameProp == null) return;

            var db = styleDbProp.objectReferenceValue as AnoGame.AnoDialogue.Data.DialogueStyle;
            if (db == null || db.styleNames == null || db.styleNames.Count == 0)
            {
                if (db != null)
                {
                    container.Add(new HelpBox("Database has no styles defined.", HelpBoxMessageType.Info));
                }
                return;
            }

            var list = db.styleNames.ToList();
            int index = list.IndexOf(styleNameProp.stringValue);
            if (index < 0) index = 0;

            var dropdown = new DropdownField("Dialogue Style", list, index);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                styleNameProp.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateSceneImageSectionVisibility(evt.newValue);
            });
            container.Add(dropdown);

            // 初期表示時にも可視性を更新
            container.schedule.Execute(() => UpdateSceneImageSectionVisibility(styleNameProp.stringValue));
        }

        // ────────────────────────────────────────────────
        // Scene Image Section
        // ────────────────────────────────────────────────
        private void BuildSceneImageSection(VisualElement root)
        {
            _sceneImageSection = new VisualElement();

            var sceneImageProp = serializedObject.FindProperty("sceneImage");
            if (sceneImageProp != null)
            {
                var imgField = new PropertyField(sceneImageProp, "Scene Image");
                _sceneImageSection.Add(imgField);
            }

            root.Add(_sceneImageSection);
        }

        private void UpdateSceneImageSectionVisibility(string styleName)
        {
            if (_sceneImageSection == null) return;

            // StyleDatabase から supportsSceneImage を取得
            var styleDbProp = serializedObject.FindProperty("styleDatabase");
            var db = styleDbProp?.objectReferenceValue as AnoGame.AnoDialogue.Data.DialogueStyle;

            bool supports = true;
            if (db != null)
            {
                supports = db.GetSupportsSceneImage(styleName);
            }

            _sceneImageSection.style.display = supports ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ────────────────────────────────────────────────
        // Selection
        // ────────────────────────────────────────────────
        private void BuildSelectionSection(VisualElement root)
        {
            var sectionLabel = new Label("Selection");
            sectionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionLabel.style.marginTop = 8;
            root.Add(sectionLabel);

            var convIDProp = serializedObject.FindProperty("conversationID");
            if (convIDProp == null) return;

            // ID Row
            var idRow = new VisualElement();
            idRow.style.flexDirection = FlexDirection.Row;
            idRow.style.alignItems = Align.Center;
            idRow.style.overflow = Overflow.Hidden;

            _convIDField = new TextField("Conversation ID");
            _convIDField.isReadOnly = true;
            _convIDField.SetEnabled(false);
            _convIDField.style.flexGrow = 1;
            _convIDField.style.flexShrink = 1;
            _convIDField.style.overflow = Overflow.Hidden;
            _convIDField.style.minWidth = 0;
            _convIDField.BindProperty(convIDProp);
            idRow.Add(_convIDField);

            var clearBtn = new Button(() =>
            {
                convIDProp.stringValue = "";
                serializedObject.ApplyModifiedProperties();
                RefreshSelectionInfo();
                RefreshCandidateButtons();
            });
            clearBtn.text = "Clear";
            clearBtn.style.width = 50;
            idRow.Add(clearBtn);

            root.Add(idRow);

            // Selection Info
            _selectionInfo = new HelpBox("", HelpBoxMessageType.None);
            root.Add(_selectionInfo);

            // Track changes to conversationID
            _convIDField.RegisterValueChangedCallback(evt =>
            {
                RefreshSelectionInfo();
            });
        }

        private void RefreshSelectionInfo()
        {
            if (_selectionInfo == null) return;

            var convIDProp = serializedObject.FindProperty("conversationID");
            if (convIDProp == null) return;

            string id = convIDProp.stringValue;
            if (string.IsNullOrEmpty(id))
            {
                _selectionInfo.text = "Please select a Conversation ID.";
                _selectionInfo.messageType = HelpBoxMessageType.Info;
            }
            else if (_masterData != null)
            {
                var unit = _masterData.GetConversationByID(id);
                if (unit != null)
                {
                    _selectionInfo.text = $"Selected: {unit.SectionName} (Speaker: {unit.SpeakerName})";
                    _selectionInfo.messageType = HelpBoxMessageType.None;
                }
                else
                {
                    _selectionInfo.text = "ID not found in MasterData";
                    _selectionInfo.messageType = HelpBoxMessageType.Warning;
                }
            }
            else
            {
                _selectionInfo.text = "MasterDialogueData not found.";
                _selectionInfo.messageType = HelpBoxMessageType.Warning;
            }
        }

        // ────────────────────────────────────────────────
        // Filter
        // ────────────────────────────────────────────────
        private void BuildFilterSection(VisualElement root)
        {
            var filterLabel = new Label("Filter Candidates");
            filterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            filterLabel.style.marginTop = 10;
            root.Add(filterLabel);

            var filterRow = new VisualElement();
            filterRow.style.flexDirection = FlexDirection.Row;
            filterRow.style.alignItems = Align.Center;

            var clearFilterBtn = new Button(() =>
            {
                _filterEp = -1;
                _filterCh = -1;
                _filterSec = -1;
                SaveFilters();
                // Refresh text fields
                RefreshFilterFields(filterRow);
                UpdateFilteredList();
                RefreshCandidateButtons();
            });
            clearFilterBtn.text = "Clear";
            clearFilterBtn.style.width = 45;
            filterRow.Add(clearFilterBtn);

            filterRow.Add(CreateFilterField("Ep", _filterEp, val =>
            {
                _filterEp = val;
                SaveFilters();
                UpdateFilteredList();
                RefreshCandidateButtons();
            }));

            filterRow.Add(CreateFilterField("Ch", _filterCh, val =>
            {
                _filterCh = val;
                SaveFilters();
                UpdateFilteredList();
                RefreshCandidateButtons();
            }));

            filterRow.Add(CreateFilterField("Sec", _filterSec, val =>
            {
                _filterSec = val;
                SaveFilters();
                UpdateFilteredList();
                RefreshCandidateButtons();
            }));

            root.Add(filterRow);
        }

        private TextField CreateFilterField(string label, int initialValue, System.Action<int> onChanged)
        {
            var field = new TextField(label);
            field.value = initialValue == -1 ? "" : initialValue.ToString();
            field.style.flexGrow = 1;
            field.style.minWidth = 70;
            field.name = $"filter-{label}";
            // ラベル幅を抑えて入力部分を確保
            field.labelElement.style.minWidth = 20;
            field.labelElement.style.maxWidth = 30;

            field.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrEmpty(evt.newValue))
                {
                    onChanged?.Invoke(-1);
                }
                else if (int.TryParse(evt.newValue, out int result))
                {
                    onChanged?.Invoke(result);
                }
            });

            return field;
        }

        private void RefreshFilterFields(VisualElement filterRow)
        {
            var epField = filterRow.Q<TextField>("filter-Ep");
            var chField = filterRow.Q<TextField>("filter-Ch");
            var secField = filterRow.Q<TextField>("filter-Sec");

            if (epField != null) epField.SetValueWithoutNotify(_filterEp == -1 ? "" : _filterEp.ToString());
            if (chField != null) chField.SetValueWithoutNotify(_filterCh == -1 ? "" : _filterCh.ToString());
            if (secField != null) secField.SetValueWithoutNotify(_filterSec == -1 ? "" : _filterSec.ToString());
        }

        // ────────────────────────────────────────────────
        // Candidates
        // ────────────────────────────────────────────────
        private void BuildCandidatesSection(VisualElement root)
        {
            _candidatesFoldout = new Foldout();
            _candidatesFoldout.text = "Candidates (0)";
            _candidatesFoldout.value = true;
            _candidatesFoldout.style.marginTop = 10;
            root.Add(_candidatesFoldout);

            // Total Data & Grid Toggle Row
            var infoRow = new VisualElement();
            infoRow.style.flexDirection = FlexDirection.Row;
            infoRow.style.alignItems = Align.Center;

            _totalDataLabel = new Label("Total Data: 0");
            _totalDataLabel.style.fontSize = 10;
            _totalDataLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            infoRow.Add(_totalDataLabel);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            infoRow.Add(spacer);

            var gridToggle = new Toggle("Grid View");
            gridToggle.value = _useGridView;
            gridToggle.RegisterValueChangedCallback(evt =>
            {
                _useGridView = evt.newValue;
                RefreshCandidateButtons();
            });
            infoRow.Add(gridToggle);

            _candidatesFoldout.Add(infoRow);

            // Scroll Area
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.maxHeight = 200;
            scrollView.style.minHeight = 40;

            _candidateContainer = new VisualElement();
            _candidateContainer.name = "candidate-list";
            scrollView.Add(_candidateContainer);

            _candidatesFoldout.Add(scrollView);
        }

        private void RefreshCandidateButtons()
        {
            if (_candidateContainer == null) return;
            _candidateContainer.Clear();

            var convIDProp = serializedObject.FindProperty("conversationID");
            string selectedID = convIDProp != null ? convIDProp.stringValue : "";

            // Header update
            if (_candidatesFoldout != null)
            {
                _candidatesFoldout.text = $"Candidates ({_filteredIDs.Count})";
            }
            if (_totalDataLabel != null && _masterData != null)
            {
                _totalDataLabel.text = $"Total Data: {_masterData.Conversations.Count}";
            }

            if (_filteredIDs.Count == 0)
            {
                _candidateContainer.Add(new HelpBox("No conversations match the current filter.", HelpBoxMessageType.Info));
                return;
            }

            // Build ordered list (selected first)
            var orderedIDs = GetOrderedIDs(selectedID);

            if (_useGridView)
            {
                DrawGridCandidates(orderedIDs, selectedID, convIDProp);
            }
            else
            {
                DrawListCandidates(orderedIDs, selectedID, convIDProp);
            }
        }

        private void DrawListCandidates(List<string> orderedIDs, string selectedID, SerializedProperty convIDProp)
        {
            foreach (var id in orderedIDs)
            {
                string displayName = ResolveDisplayName(id);
                bool isSelected = id == selectedID;

                var btn = new Button(() =>
                {
                    SelectConversation(convIDProp, id);
                });
                btn.text = displayName;
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                btn.style.marginBottom = 1;
                btn.style.marginTop = 1;

                if (isSelected)
                {
                    btn.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
                    btn.style.color = Color.white;
                }

                _candidateContainer.Add(btn);
            }
        }

        private void DrawGridCandidates(List<string> orderedIDs, string selectedID, SerializedProperty convIDProp)
        {
            VisualElement row = null;
            int colIndex = 0;
            int maxCols = 3;

            foreach (var id in orderedIDs)
            {
                if (colIndex % maxCols == 0)
                {
                    row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 1;
                    _candidateContainer.Add(row);
                }

                string displayName = ResolveDisplayName(id);
                string truncated = displayName.Length > 12 ? displayName.Substring(0, 12) + ".." : displayName;
                bool isSelected = id == selectedID;

                string tooltipText = id;
                if (_masterData != null)
                {
                    var unit = _masterData.GetConversationByID(id);
                    if (unit != null)
                    {
                        tooltipText = $"{unit.SectionName}\n{unit.SpeakerName}\n{unit.BodyText}";
                    }
                }

                var btn = new Button(() =>
                {
                    SelectConversation(convIDProp, id);
                });
                btn.text = truncated;
                btn.tooltip = tooltipText;
                btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                btn.style.flexGrow = 1;
                btn.style.flexBasis = 0;
                btn.style.marginRight = 2;

                if (isSelected)
                {
                    btn.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
                    btn.style.color = Color.white;
                }

                row.Add(btn);
                colIndex++;
            }
        }

        private void SelectConversation(SerializedProperty convIDProp, string id)
        {
            if (convIDProp == null) return;
            convIDProp.stringValue = id;
            serializedObject.ApplyModifiedProperties();

            // Also persist filter values to clip
            var epProp = serializedObject.FindProperty("targetEpisode");
            var chProp = serializedObject.FindProperty("targetChapter");
            var secProp = serializedObject.FindProperty("targetSection");
            if (epProp != null) epProp.intValue = _filterEp;
            if (chProp != null) chProp.intValue = _filterCh;
            if (secProp != null) secProp.intValue = _filterSec;
            serializedObject.ApplyModifiedProperties();

            RefreshSelectionInfo();
            RefreshCandidateButtons();
        }

        // ────────────────────────────────────────────────
        // Data Helpers
        // ────────────────────────────────────────────────
        private string ResolveDisplayName(string id)
        {
            if (_masterData == null) return id;
            var unit = _masterData.GetConversationByID(id);
            if (unit != null && !string.IsNullOrEmpty(unit.SectionName))
            {
                return unit.SectionName;
            }
            return id;
        }

        private List<string> GetOrderedIDs(string selectedID)
        {
            if (string.IsNullOrEmpty(selectedID) || !_filteredIDs.Contains(selectedID))
                return _filteredIDs;

            var ordered = new List<string>(_filteredIDs.Count);
            ordered.Add(selectedID);
            foreach (var id in _filteredIDs)
            {
                if (id != selectedID) ordered.Add(id);
            }
            return ordered;
        }

        private void UpdateFilteredList()
        {
            _filteredIDs.Clear();
            if (_masterData == null) return;

            // Always show roots only
            HashSet<string> referencedIDs = new HashSet<string>();
            foreach (var unit in _masterData.Conversations)
            {
                if (!string.IsNullOrEmpty(unit.NextID)) referencedIDs.Add(unit.NextID);
                if (unit.Choices != null)
                {
                    foreach (var choice in unit.Choices)
                    {
                        if (!string.IsNullOrEmpty(choice.TargetID)) referencedIDs.Add(choice.TargetID);
                    }
                }
            }

            foreach (var conv in _masterData.Conversations)
            {
                if (referencedIDs.Contains(conv.ID)) continue;

                bool match = true;
                if (_filterEp != -1 && conv.EpisodeID != _filterEp) match = false;
                if (_filterCh != -1 && conv.ChapterID != _filterCh) match = false;
                if (_filterSec != -1 && conv.SectionID != _filterSec) match = false;

                if (match)
                {
                    _filteredIDs.Add(conv.ID);
                }
            }
        }

        private void FindMasterData()
        {
            string[] guids = AssetDatabase.FindAssets("t:MasterDialogueData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _masterData = AssetDatabase.LoadAssetAtPath<MasterDialogueData>(path);
            }
        }

        private void LoadFilters()
        {
            // Load from clip properties first
            var epProp = serializedObject.FindProperty("targetEpisode");
            var chProp = serializedObject.FindProperty("targetChapter");
            var secProp = serializedObject.FindProperty("targetSection");

            if (epProp != null && epProp.intValue != -1)
            {
                _filterEp = epProp.intValue;
                _filterCh = chProp != null ? chProp.intValue : -1;
                _filterSec = secProp != null ? secProp.intValue : -1;
            }
            else
            {
                // Fallback to EditorPrefs
                _filterEp = EditorPrefs.GetInt(PrefsKeyEp, -1);
                _filterCh = EditorPrefs.GetInt(PrefsKeyCh, -1);
                _filterSec = EditorPrefs.GetInt(PrefsKeySec, -1);
            }
        }

        private void SaveFilters()
        {
            EditorPrefs.SetInt(PrefsKeyEp, _filterEp);
            EditorPrefs.SetInt(PrefsKeyCh, _filterCh);
            EditorPrefs.SetInt(PrefsKeySec, _filterSec);
        }

        // ────────────────────────────────────────────────
        // Utilities
        // ────────────────────────────────────────────────
        private VisualElement CreateSeparator()
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            sep.style.marginTop = 8;
            sep.style.marginBottom = 8;
            return sep;
        }
    }
}

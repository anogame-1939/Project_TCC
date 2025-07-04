// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Manages subtitle panels for StandardDialogueUI.
    /// </summary>
    [System.Serializable]
    public class StandardUISubtitleControls : AbstractUISubtitleControls
    {

        #region Private Fields

        // The built-in subtitle panels assigned to the StandardDialogueUI:
        private List<StandardUISubtitlePanel> m_builtinPanels = new List<StandardUISubtitlePanel>();
        private List<StandardUISubtitlePanel> m_customPanels = new List<StandardUISubtitlePanel>();
        private StandardUISubtitlePanel m_defaultNPCPanel = null;
        private StandardUISubtitlePanel m_defaultPCPanel = null;
        private StandardUISubtitlePanel m_forcedOverridePanel = null;

        // The panel that's currently focused:
        private StandardUISubtitlePanel m_focusedPanel = null;

        // After we look up which panel an actor uses, we cache the value so we don't need to look it up again:
        private Dictionary<Transform, StandardUISubtitlePanel> m_actorPanelCache = new Dictionary<Transform, StandardUISubtitlePanel>();

        // If the speaker has no DialogueActor, we can also override by actor ID:
        private Dictionary<int, StandardUISubtitlePanel> m_actorIdOverridePanel = new Dictionary<int, StandardUISubtitlePanel>();

        // Actor ID of last actor to use each panel:
        private Dictionary<int, StandardUISubtitlePanel> m_lastPanelUsedByActor = new Dictionary<int, StandardUISubtitlePanel>();
        private Dictionary<StandardUISubtitlePanel, int> m_lastActorToUsePanel = new Dictionary<StandardUISubtitlePanel, int>();

        // Cache DialogueActor components so we don't need to look them up again:
        private Dictionary<Transform, DialogueActor> m_dialogueActorCache = new Dictionary<Transform, DialogueActor>();

        // Cache of actors that want to use bark UIs:
        private List<Transform> m_useBarkUIs = new List<Transform>();

        public StandardUISubtitlePanel defaultNPCPanel
        {
            get { return m_defaultNPCPanel; }
            set { m_defaultNPCPanel = value; }
        }
        public StandardUISubtitlePanel defaultPCPanel
        {
            get { return m_defaultPCPanel; }
            set { m_defaultPCPanel = value; }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Indicates whether the focused subtitle contains text.
        /// </summary>
        public override bool hasText { get { return m_focusedPanel != null && !string.IsNullOrEmpty(m_focusedPanel.subtitleText.text); } }

        public virtual bool allowDialogueActorCustomPanels { get; set; } = true;

        #endregion

        #region Initialization & Lookup

        public void Initialize(StandardUISubtitlePanel[] subtitlePanels, StandardUISubtitlePanel defaultNPCSubtitlePanel, StandardUISubtitlePanel defaultPCSubtitlePanel)
        {
            m_builtinPanels.Clear();
            m_builtinPanels.AddRange(subtitlePanels);
            m_defaultNPCPanel = (defaultNPCSubtitlePanel != null) ? defaultNPCSubtitlePanel : (m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null;
            m_defaultPCPanel = (defaultPCSubtitlePanel != null) ? defaultPCSubtitlePanel : (m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null;
            if (m_defaultNPCPanel != null) m_defaultNPCPanel.isDefaultNPCPanel = true;
            if (m_defaultPCPanel != null) m_defaultPCPanel.isDefaultPCPanel = true;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) m_builtinPanels[i].panelNumber = i;
            }
            ClearCache();
        }

        public void ClearCache()
        {
            m_actorPanelCache.Clear();
            m_customPanels.Clear();
            m_actorIdOverridePanel.Clear();
            m_lastPanelUsedByActor.Clear();
            m_lastActorToUsePanel.Clear();
            m_dialogueActorCache.Clear();
            m_useBarkUIs.Clear();
        }

        public void ClearOverrideCache()
        {
            m_actorPanelCache.Clear();
            m_customPanels.Clear();
        }

        public void ForceOverrideSubtitlePanel(StandardUISubtitlePanel customPanel)
        {
            m_forcedOverridePanel = customPanel;
        }

        /// <summary>
        /// For speakers who do not have DialogueActor components, this method overrides the
        /// actor's default panel.
        /// </summary>
        public void OverrideActorPanel(Actor actor, SubtitlePanelNumber subtitlePanelNumber,
            StandardUISubtitlePanel customPanel = null, bool immediate = false)
        {
            if (actor == null) return;
            if (customPanel == null) customPanel = actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel;
            var panel = GetPanelFromNumber(subtitlePanelNumber, customPanel);
            if (panel == null)
            {
                m_actorIdOverridePanel.Remove(actor.id);
            }
            else
            {
                // Set panel override:
                m_actorIdOverridePanel[actor.id] = panel;

                if (immediate)
                {
                    // Make change immediately:
                    var subtitle = DialogueManager.currentConversationState.subtitle;
                    var isSpeaker = actor != null && subtitle.speakerInfo.id == actor.id;
                    StandardUISubtitlePanel actorCurrentPanel =
                        m_builtinPanels.Find(x => x.isOpen && x.portraitActorName == actor.Name) ??
                        m_customPanels.Find(x => x.isOpen && x.portraitActorName == actor.Name);
                    if (actorCurrentPanel != panel && actorCurrentPanel != null && actorCurrentPanel.portraitImage.sprite == actor.GetPortraitSprite())
                    {
                        // Actor is currently present in another open panel, so close it 
                        // and open the new panel with the same settings:
                        var isContinueButtonActive = actorCurrentPanel.continueButton != null && actorCurrentPanel.continueButton.gameObject.activeInHierarchy;
                        var currentName = actorCurrentPanel.portraitActorName;
                        var currentSprite = (actorCurrentPanel.portraitImage != null) ? actorCurrentPanel.portraitImage.sprite : null;
                        var currentHasFocus = actorCurrentPanel.hasFocus;
                        if (actorCurrentPanel.subtitleText.gameObject != panel.subtitleText.gameObject)
                        {
                            actorCurrentPanel.Close();
                        }
                        else
                        {
                            actorCurrentPanel.Unfocus();
                            actorCurrentPanel.SetPortraitName(string.Empty);
                            actorCurrentPanel.SetPortraitImage(null);
                        }
                        if (panel.panelState != UIPanel.PanelState.Open)
                        {
                            panel.Open();
                            if (currentHasFocus || (actor != null && actor.id == DialogueManager.currentConversationState.subtitle.speakerInfo.id))
                            {
                                panel.Focus();
                            }
                            if (isContinueButtonActive) panel.ShowContinueButton();
                        }
                        panel.SetPortraitName(currentName);
                        if (actorCurrentPanel.portraitImage != null)
                        {
                            panel.SetPortraitImage(currentSprite);
                        }
                    }
                    else
                    {
                        // Actor is not present in another panel, so open the
                        // specified panel and set its content from basic actor info:
                        if (panel.panelState != UIPanel.PanelState.Open)
                        {
                            panel.Open();
                            if ((actor != null && actor.id == DialogueManager.currentConversationState.subtitle.speakerInfo.id))
                            {
                                panel.Focus();
                            }
                            if (DialogueManager.conversationView.isWaitingForContinue) panel.ShowContinueButton();
                        }
                        panel.SetPortraitImage(actor.GetPortraitSprite());
                        // If actor is not current speaker and portrait name GameObject is set
                        // to speaker's name, do not set the portrait name to the actor's name
                        // in case multiple subtitle panels share the portrait name GameObject.
                        var actorDisplayName = CharacterInfo.GetLocalizedDisplayNameInDatabase(DialogueLua.GetActorField(actor.Name, "Name").asString);
                        if (isSpeaker || panel.portraitName.text != subtitle.speakerInfo.Name)
                        {
                            panel.SetPortraitName(actorDisplayName);
                        }
                    }
                    // Check animator if actor is current subtitle's speaker:
                    if (actor != null && isSpeaker)
                    {
                        panel.CheckSubtitleAnimator(subtitle);
                    }
                }
            }
        }

        /// <summary>
        /// Overrides a DialogueActor's subtitle panel.
        /// </summary>
        /// <param name="dialogueActor">DialogueActor whose panel to override for this conversation.</param>
        /// <param name="subtitlePanelNumber">New subtitle panel number.</param>
        /// <param name="customPanel">Only used if subtitlePanelNumber is Custom.</param>
        public void OverrideActorPanel(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel = null)
        {
            if (dialogueActor == null) return;
            var actor = DialogueManager.masterDatabase.GetActor(dialogueActor.actor);
            StandardUISubtitlePanel panel = null;
            switch (subtitlePanelNumber)
            {
                case SubtitlePanelNumber.Default:
                    panel = (actor != null && actor.IsPlayer) ? m_defaultPCPanel : m_defaultNPCPanel;
                    break;
                case SubtitlePanelNumber.UseBarkUI:
                    break;
                default:
                case SubtitlePanelNumber.Custom:
                    panel = allowDialogueActorCustomPanels
                        ? GetPanelFromNumber(subtitlePanelNumber, customPanel)
                        : (actor != null && actor.IsPlayer) ? m_defaultPCPanel : m_defaultNPCPanel;
                    break;
            }
            if (panel == null)
            {
                m_actorPanelCache.Remove(dialogueActor.transform);
            }
            else
            {
                m_actorPanelCache[dialogueActor.transform] = panel;
            }
            if (actor != null && m_actorIdOverridePanel.ContainsKey(actor.id))
            {
                m_actorIdOverridePanel.Remove(actor.id);
            }
        }

        public virtual StandardUISubtitlePanel GetPanel(Subtitle subtitle, out DialogueActor dialogueActor)
        {
            dialogueActor = null;
            if (subtitle == null) return m_defaultNPCPanel;

            // Get dialogue actor from cache in case we exit early:
            if (subtitle.speakerInfo.transform != null)
            {
                m_dialogueActorCache.TryGetValue(subtitle.speakerInfo.transform, out dialogueActor);
            }

            // Check if we have a forced override:
            if (m_forcedOverridePanel != null) return m_forcedOverridePanel;

            // Check [panel=#] tag:
            var overrideIndex = subtitle.formattedText.subtitlePanelNumber;
            if (0 <= overrideIndex && overrideIndex < m_builtinPanels.Count)
            {
                var overridePanel = m_builtinPanels[overrideIndex];
                overridePanel.actorOverridingPanel = subtitle.speakerInfo.transform;
                return overridePanel;
            }

            // Check actor ID override:
            if (m_actorIdOverridePanel.ContainsKey(subtitle.speakerInfo.id))
            {
                var overridePanel = m_actorIdOverridePanel[subtitle.speakerInfo.id];
                overridePanel.actorOverridingPanel = subtitle.speakerInfo.transform;
                return overridePanel;
            }

            // Get actor's panel:
            var speakerTransform = subtitle.speakerInfo.transform;
            var panel = GetActorTransformPanel(speakerTransform, subtitle.speakerInfo.isNPC ? m_defaultNPCPanel : m_defaultPCPanel, out dialogueActor);
            if (subtitle.speakerInfo.transform != null && dialogueActor != null)
            {
                m_dialogueActorCache[subtitle.speakerInfo.transform] = dialogueActor;
            }
            return panel;
        }

        public StandardUISubtitlePanel GetActorTransformPanel(Transform speakerTransform, StandardUISubtitlePanel defaultPanel, out DialogueActor dialogueActor)
        {
            dialogueActor = null;
            if (speakerTransform == null) return defaultPanel;
            if (m_dialogueActorCache.ContainsKey(speakerTransform))
            {
                dialogueActor = m_dialogueActorCache[speakerTransform];
            }
            else
            {
                dialogueActor = DialogueActor.GetDialogueActorComponent(speakerTransform);
                m_dialogueActorCache.Add(speakerTransform, dialogueActor);
            }
            if (m_actorPanelCache.ContainsKey(speakerTransform) && m_actorPanelCache[speakerTransform] != null) return m_actorPanelCache[speakerTransform];
            if (m_useBarkUIs.Contains(speakerTransform)) return null;
            if (DialogueActorUsesBarkUI(dialogueActor))
            {
                m_useBarkUIs.Add(speakerTransform);
                return null;
            }
            else
            {
                var panel = GetDialogueActorPanel(dialogueActor);
                if (panel == null) panel = defaultPanel;
                m_actorPanelCache[speakerTransform] = panel;
                m_useBarkUIs.Remove(speakerTransform);
                return panel;
            }
        }

        private bool DialogueActorUsesBarkUI(DialogueActor dialogueActor)
        {
            return dialogueActor != null && dialogueActor.GetSubtitlePanelNumber() == SubtitlePanelNumber.UseBarkUI;
        }

        public StandardUISubtitlePanel GetDialogueActorPanel(DialogueActor dialogueActor)
        {
            if (dialogueActor == null) return null;
            return GetPanelFromNumber(dialogueActor.standardDialogueUISettings.subtitlePanelNumber, dialogueActor.standardDialogueUISettings.customSubtitlePanel);
        }

        public StandardUISubtitlePanel GetPanelFromNumber(SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel)
        {
            switch (subtitlePanelNumber)
            {
                case SubtitlePanelNumber.Default:
                    return null;
                case SubtitlePanelNumber.Custom:
                    if (!allowDialogueActorCustomPanels) return null;
                    if (!m_customPanels.Contains(customPanel)) m_customPanels.Add(customPanel);
                    return customPanel;
                case SubtitlePanelNumber.UseBarkUI:
                    return null;
                default:
                    var index = PanelNumberUtility.GetSubtitlePanelIndex(subtitlePanelNumber);
                    return (0 <= index && index < m_builtinPanels.Count) ? m_builtinPanels[index] : null;
            }
        }

        private bool SubtitleUsesBarkUI(Subtitle subtitle)
        {
            if (subtitle == null) return false;
            return m_useBarkUIs.Contains(subtitle.speakerInfo.transform);
        }

        private string GetSubtitleTextSummary(Subtitle subtitle)
        {
            return (subtitle == null) ? "(empty subtitle)" : "[" + subtitle.speakerInfo.Name + "] '" + subtitle.formattedText.text + "'";
        }

        /// <summary>
        /// Changes a dialogue actor's panel for the current conversation. Can still be overridden by [panel=#] tags.
        /// </summary>
        public virtual void SetActorSubtitlePanelNumber(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber)
        {
            if (dialogueActor == null) return;
            if (m_actorPanelCache.ContainsKey(dialogueActor.transform))
            {
                m_actorPanelCache.Remove(dialogueActor.transform);
            }
            if (!m_dialogueActorCache.ContainsKey(dialogueActor.transform))
            {
                m_dialogueActorCache.Add(dialogueActor.transform, dialogueActor);
            }
            if (m_useBarkUIs.Contains(dialogueActor.transform) && subtitlePanelNumber != SubtitlePanelNumber.UseBarkUI)
            {
                m_useBarkUIs.Remove(dialogueActor.transform);
            }
            m_actorPanelCache[dialogueActor.transform] = GetPanelFromNumber(subtitlePanelNumber, dialogueActor.standardDialogueUISettings.customSubtitlePanel);
        }

        public void ShowActorInPanel(Actor actor, SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel = null)
        {
            if (actor == null) return;
            var panel = GetPanelFromNumber(subtitlePanelNumber, customPanel);
            if (m_lastPanelUsedByActor.ContainsKey(actor.id))
            {
                if (m_lastPanelUsedByActor[actor.id] == panel) return; // Already using panel.
                m_lastPanelUsedByActor[actor.id].Close();
            }
            var displayName = DialogueLua.GetLocalizedActorField(actor.Name, "Display Name").asString;
            if (string.IsNullOrEmpty(displayName)) displayName = actor.localizedName;
            panel.OpenOnStartConversation(actor.GetPortraitSprite(), displayName, null);

        }

        #endregion

        #region Save & Load Actor Panel Cache

        // Queued panel numbers to apply when starting a conversation.
        private List<string> m_queuedActorGOs = null;
        private List<SubtitlePanelNumber> m_queuedActorGOPanels = null;
        private List<int> m_queuedActorIDs = null;
        private List<SubtitlePanelNumber> m_queuedActorIDPanels = null;

        /// <summary>
        /// Record the current actor panel cache values for saved games (ConversationStateSaver)
        /// so the cache can be restored when loading a game. Only saves built-in panel numbers,
        /// not custom panels.
        /// </summary>
        public virtual void RecordActorPanelCache(out List<string> actorGOs, out List<SubtitlePanelNumber> actorGOPanels,
            out List<int> actorIDs, out List<SubtitlePanelNumber> actorIDPanels, out List<string> actorNames)
        {
            actorGOs = new List<string>();
            actorGOPanels = new List<SubtitlePanelNumber>();
            actorIDs = new List<int>();
            actorIDPanels = new List<SubtitlePanelNumber>();
            actorNames = new List<string>();
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                actorNames.Add(string.Empty);
            }
            foreach (var kvp in m_actorPanelCache)
            {
                if (kvp.Key == null) continue;
                var panelNumber = GetSubtitlePanelNumberFromPanel(kvp.Value, false);
                if (panelNumber == SubtitlePanelNumber.Custom) continue;
                actorGOs.Add(kvp.Key.name);
                actorGOPanels.Add(panelNumber);
                if (panelNumber >= SubtitlePanelNumber.Panel0)
                {
                    actorNames[(int)panelNumber - (int)SubtitlePanelNumber.Panel0] = kvp.Key.name;
                }
            }
            foreach (var kvp in m_actorIdOverridePanel)
            {
                actorIDs.Add(kvp.Key);
                var panelNumber = GetSubtitlePanelNumberFromPanel(kvp.Value, false);
                actorIDPanels.Add(panelNumber);
                if (panelNumber >= SubtitlePanelNumber.Panel0)
                {
                    var actor = DialogueManager.masterDatabase.GetActor(kvp.Key);
                    if (actor != null)
                    {
                        actorNames[(int)panelNumber - (int)SubtitlePanelNumber.Panel0] = actor.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Queues actor panel caches to be applies when the next conversation starts.
        /// </summary>
        public virtual void QueueSavedActorPanelCache(List<string> actorGOs, List<SubtitlePanelNumber> actorGOPanels,
            List<int> actorIDs, List<SubtitlePanelNumber> actorIDPanels)
        {
            m_queuedActorGOs = actorGOs;
            m_queuedActorGOPanels = actorGOPanels;
            m_queuedActorIDs = actorIDs;
            m_queuedActorIDPanels = actorIDPanels;
        }

        /// <summary>
        /// Apply any actor panel cache values that ConversationStateSaver may have
        /// queued when loading a saved game. Only applies built-in panel numbers,
        /// not custom panels.
        /// </summary>
        public virtual void ApplyQueuedActorPanelCache()
        {
            try
            {
                if (m_queuedActorGOs == null) return; // Nothing queued.
                for (int i = 0; i < m_queuedActorGOs.Count; i++)
                {
                    var actorGO = GameObject.Find(m_queuedActorGOs[i]);
                    if (actorGO == null) continue;
                    var panel = GetPanelFromNumber(m_queuedActorGOPanels[i], null);
                    if (panel == null) continue;
                    m_actorPanelCache[actorGO.transform] = panel;
                }
                for (int i = 0; i < m_queuedActorIDs.Count; i++)
                {
                    var panel = GetPanelFromNumber(m_queuedActorIDPanels[i], null);
                    if (panel == null) continue;
                    m_actorIdOverridePanel[m_queuedActorIDs[i]] = panel;
                }
            }
            finally
            {
                m_queuedActorGOs = null;
                m_queuedActorGOPanels = null;
                m_queuedActorIDs = null;
                m_queuedActorIDPanels = null;
            }
        }

        protected virtual SubtitlePanelNumber GetSubtitlePanelNumberFromPanel(StandardUISubtitlePanel panel,
            bool allowReturnDefault = true)
        {
            if (allowReturnDefault &&
                (panel == m_defaultNPCPanel || panel == m_defaultPCPanel))
            {
                return SubtitlePanelNumber.Default;
            }
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (panel == m_builtinPanels[i]) return PanelNumberUtility.IntToSubtitlePanelNumber(i);
            }
            return SubtitlePanelNumber.Custom;
        }

        #endregion

        #region Show & Hide

        /// <summary>
        /// Sets the panel that will receive focus for the specified subtitle.
        /// When delaying the display of the subtitle while the main dialogue
        /// panel is opening, we still need a reference to the subtitle panel
        /// to handle continue button stuff once the main panel is open.
        /// 
        /// </summary>
        public StandardUISubtitlePanel StageFocusedPanel(Subtitle subtitle)
        {
            DialogueActor dialogueActor;
            m_focusedPanel = GetPanel(subtitle, out dialogueActor);
            return m_focusedPanel;
        }

        /// <summary>
        /// Shows a subtitle. Opens a subtitle panel and sets the content. If the speaker
        /// has a DialogueActor component, this may dictate which panel opens.
        /// </summary>
        /// <param name="subtitle">Subtitle to show.</param>
        public override void ShowSubtitle(Subtitle subtitle)
        {
            if (subtitle == null) return;
            DialogueActor dialogueActor;
            var panel = GetPanel(subtitle, out dialogueActor);
            if (SubtitleUsesBarkUI(subtitle))
            {
                DialogueManager.instance.StartCoroutine(BarkController.Bark(subtitle));
            }
            else if (panel == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
            }
            else if (string.IsNullOrEmpty(subtitle.formattedText.text))
            {
                HideSubtitle(subtitle);
            }
            else
            {
                // If actor is currently displaying on another panel, close that panel:
                var actorID = subtitle.speakerInfo.id;
                if (m_lastPanelUsedByActor.ContainsKey(actorID) && m_lastPanelUsedByActor[actorID] != panel)
                {
                    var previousPanel = m_lastPanelUsedByActor[actorID];
                    if (m_lastActorToUsePanel.ContainsKey(previousPanel) && m_lastActorToUsePanel[previousPanel] == actorID)
                    {
                        if (previousPanel.hasFocus || previousPanel.isFocusing) previousPanel.Unfocus();
                        if (previousPanel.isOpen) previousPanel.Close();
                    }
                }
                SetLastActorToUsePanel(panel, actorID);

                if (dialogueActor != null)
                {
                    m_actorPanelCache[dialogueActor.transform] = panel;
                }

                // Focus the panel and show the subtitle:
                m_focusedPanel = panel;
                if (dialogueActor != null && dialogueActor.standardDialogueUISettings.setSubtitleColor)
                {
                    subtitle.formattedText.text = dialogueActor.AdjustSubtitleColor(subtitle);
                }
                SupercedeOtherPanels(panel);
                panel.ShowSubtitle(subtitle);
            }

            // Check if a [picc=#] tag changed the conversant's portrait:
            if (subtitle.formattedText.picConversant != FormattedText.NoPicOverride)
            {
                var conversantPortrait = subtitle.GetListenerOverridePortrait();
                var defaultPanel = subtitle.listenerInfo.IsNPC ? defaultNPCPanel : defaultPCPanel;
                DialogueActor conversantDialogueActor;
                var conversantPanel = GetActorTransformPanel(subtitle.listenerInfo.transform, defaultPanel, out conversantDialogueActor);
                if (conversantPanel != null && conversantPanel.portraitImage != null) conversantPanel.portraitImage.sprite = conversantPortrait;
            }
        }

        /// <summary>
        /// Hides a subtitle.
        /// </summary>
        /// <param name="subtitle">Subtitle associated with panel to hide.</param>
        public void HideSubtitle(Subtitle subtitle)
        {
            if (subtitle == null) return;
            DialogueActor dialogueActor;
            var panel = GetPanel(subtitle, out dialogueActor);
            if (SubtitleUsesBarkUI(subtitle)) return;
            if (panel == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
            }
            else if (panel.visibility == UIVisibility.OnlyDuringContent)
            {
                panel.HideSubtitle(subtitle);
            }
            else
            {
                panel.FinishSubtitle();
            }
        }

        /// <summary>
        /// Close all panels.
        /// </summary>
        public void Close()
        {
            if (m_defaultNPCPanel != null) m_defaultNPCPanel.Close();
            if (m_defaultPCPanel != null) m_defaultPCPanel.Close();
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) m_builtinPanels[i].Close();
            }
            for (int i = 0; i < m_customPanels.Count; i++)
            {
                if (m_customPanels[i] != null) m_customPanels[i].Close();
            }
            foreach (var kvp in m_actorPanelCache)
            {
                if (kvp.Value != null) kvp.Value.Close();
            }
            //--- No longer clear all caches when closing subtitles because SetDialoguePanel may close them: ClearCache();
            ClearOverrideCache();
        }

        public bool AreAnyPanelsClosing()
        {
            if (m_defaultNPCPanel != null && m_defaultNPCPanel.panelState == UIPanel.PanelState.Closing) return true;
            if (m_defaultPCPanel != null && m_defaultPCPanel.panelState == UIPanel.PanelState.Closing) return true;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null && m_builtinPanels[i].panelState == UIPanel.PanelState.Closing) return true;
            }
            for (int i = 0; i < m_customPanels.Count; i++)
            {
                if (m_customPanels[i] != null && m_customPanels[i].panelState == UIPanel.PanelState.Closing) return true;
            }
            foreach (var kvp in m_actorPanelCache)
            {
                if (kvp.Value != null && kvp.Value.panelState == UIPanel.PanelState.Closing) return true;
            }
            return false;
        }

        protected virtual void SupercedeOtherPanels(StandardUISubtitlePanel newPanel)
        {
            SupercedeOtherPanelsInList(m_builtinPanels, newPanel);
            SupercedeOtherPanelsInList(m_customPanels, newPanel);
        }

        protected virtual void SupercedeOtherPanelsInList(List<StandardUISubtitlePanel> list, StandardUISubtitlePanel newPanel)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var panel = list[i];
                if (panel == null || panel == newPanel) continue;
                if (panel.isOpen)
                {
                    if (UITools.CanBeSuperceded(panel.visibility))
                    {
                        panel.Close();
                    }
                    else
                    {
                        panel.Unfocus();
                    }
                }
                else
                {
                    panel.StopShowSubtitleCoroutines();
                }
            }
        }

        public virtual void UnfocusAll()
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel != null && panel.isOpen && (panel.hasFocus || panel.isFocusing))
                {
                    panel.Unfocus();
                }
            }
        }

        public virtual void HideOnResponseMenu()
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel != null && panel.isOpen &&
                    panel.visibility == UIVisibility.UntilSupercededOrActorChangeOrMenu)
                {
                    panel.Close();
                }
            }
        }

        public override void ShowContinueButton()
        {
            if (m_focusedPanel != null) m_focusedPanel.ShowContinueButton();
        }

        public override void HideContinueButton()
        {
            if (m_focusedPanel != null) m_focusedPanel.HideContinueButton();
        }

        public override void SetActive(bool value) { } // Unused. Work is done by StandardUISubtitlePanel.
        public override void SetSubtitle(Subtitle subtitle) { } // Unused. Work is done by StandardUISubtitlePanel.
        public override void ClearSubtitle() { } // Unused. Work is done by StandardUISubtitlePanel.

        public virtual void ClearSubtitlesOnCustomPanels()
        {
            foreach (var panel in m_customPanels)
            {
                panel.ClearText();
            }
        }

        /// <summary>
        /// Sets the portrait sprite to use in the subtitle if the named actor is the speaker.
        /// This is used to immediately update the GUI control if the SetPortrait() sequencer 
        /// command changes the portrait sprite.
        /// </summary>
        /// <param name="actorName">Actor name in database.</param>
        /// <param name="portraitSprite">Portrait sprite.</param>
        public override void SetActorPortraitSprite(string actorName, Sprite portraitSprite)
        {
            if (string.IsNullOrEmpty(actorName)) return;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel != null &&
                    ((panel.currentSubtitle != null && string.Equals(panel.currentSubtitle.speakerInfo.nameInDatabase, actorName)) ||
                    (panel.portraitActorName == actorName)))
                {
                    panel.SetActorPortraitSprite(actorName, portraitSprite);
                    //--- Check other panels in case listener's portrait changed. return;
                }
            }
            foreach (var panel in m_actorPanelCache.Values)
            {
                if (panel != null &&
                    ((panel.currentSubtitle != null && string.Equals(panel.currentSubtitle.speakerInfo.nameInDatabase, actorName)) ||
                    (panel.portraitActorName == actorName)))
                {
                    panel.SetActorPortraitSprite(actorName, portraitSprite);
                    //--- Check other panels in case listener's portrait changed. return;
                }
            }
        }

        /// <summary>
        /// Searches the current conversation for any DialogueActors who use subtitle
        /// panels that are configured to appear when the conversation starts.
        /// </summary>
        public void OpenSubtitlePanelsOnStartConversation(StandardDialogueUI ui)
        {
            ApplyQueuedActorPanelCache();

            var conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
            if (conversation == null) return;
            HashSet<StandardUISubtitlePanel> checkedPanels = new HashSet<StandardUISubtitlePanel>();
            HashSet<int> checkedActorIDs = new HashSet<int>();

            // Check main Actor & Conversant:
            var mainActorID = conversation.ActorID;
            var mainActor = DialogueManager.masterDatabase.GetActor(DialogueActor.GetActorName(DialogueManager.currentActor));
            if (mainActor != null) mainActorID = mainActor.id;
            CheckActorIDOnStartConversation(mainActorID, checkedActorIDs, checkedPanels, ui);
            CheckActorIDOnStartConversation(conversation.ConversantID, checkedActorIDs, checkedPanels, ui);

            // Check other actors:
            for (int i = 0; i < conversation.dialogueEntries.Count; i++)
            {
                var actorID = conversation.dialogueEntries[i].ActorID;
                CheckActorIDOnStartConversation(actorID, checkedActorIDs, checkedPanels, ui);
            }
        }

        private void CheckActorIDOnStartConversation(int actorID, HashSet<int> checkedActorIDs, HashSet<StandardUISubtitlePanel> checkedPanels, StandardDialogueUI ui)
        {
            if (checkedActorIDs.Contains(actorID)) return;
            checkedActorIDs.Add(actorID);
            var actor = DialogueManager.MasterDatabase.GetActor(actorID);
            if (actor == null) return;
            var actorTransform = GetActorTransform(actor.Name);
            DialogueActor dialogueActor;
            var panel = GetActorTransformPanel(actorTransform, actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel, out dialogueActor);
            if (m_actorIdOverridePanel.ContainsKey(actor.id))
            {
                panel = m_actorIdOverridePanel[actor.id];
            }
            if (panel == null && actorTransform == null && Debug.isDebugBuild) Debug.LogWarning("Dialogue System: Can't determine what subtitle panel to use for " + actor.Name, actorTransform);
            if (panel == null || checkedPanels.Contains(panel)) return;
            panel.dialogueUI = ui;
            checkedPanels.Add(panel);
            if (panel.visibility == UIVisibility.AlwaysFromStart)
            {
                var actorPortrait = (dialogueActor != null && dialogueActor.GetPortraitSprite() != null) ? dialogueActor.GetPortraitSprite() : actor.GetPortraitSprite();
                var actorName = CharacterInfo.GetLocalizedDisplayNameInDatabase(actor.Name);
                panel.OpenOnStartConversation(actorPortrait, actorName, dialogueActor);
                SetLastActorToUsePanel(panel, actorID);
            }
        }

        public void SetLastActorToUsePanel(StandardUISubtitlePanel panel, int actorID)
        {
            m_lastActorToUsePanel[panel] = actorID;
            m_lastPanelUsedByActor[actorID] = panel;
        }

        protected Transform GetActorTransform(string actorName)
        {
            var actorTransform = CharacterInfo.GetRegisteredActorTransform(actorName);
            if (actorTransform == null)
            {
                var go = GameObject.Find(actorName);
                if (go != null) actorTransform = go.transform;
            }
            return actorTransform;
        }

        public void OpenSubtitlePanelLikeStart(SubtitlePanelNumber subtitlePanelNumber)
        {
            var panel = GetPanelFromNumber(subtitlePanelNumber, null);
            if (panel == null || panel.isOpen) return;
            var conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
            if (conversation == null) return;

            for (int i = 0; i < conversation.dialogueEntries.Count; i++)
            {
                var actorID = conversation.dialogueEntries[i].ActorID;
                var actor = DialogueManager.MasterDatabase.GetActor(actorID);
                var actorTransform = GetActorTransform(actor.Name);
                DialogueActor dialogueActor;
                var actorPanel = GetActorTransformPanel(actorTransform, actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel, out dialogueActor);
                if (m_actorIdOverridePanel.ContainsKey(actor.id))
                {
                    actorPanel = m_actorIdOverridePanel[actor.id];
                }
                if (actorPanel == panel)
                {
                    var actorPortrait = (dialogueActor != null && dialogueActor.GetPortraitSprite() != null) ? dialogueActor.GetPortraitSprite() : actor.GetPortraitSprite();
                    var actorName = CharacterInfo.GetLocalizedDisplayNameInDatabase(actor.Name);
                    panel.OpenOnStartConversation(actorPortrait, actorName, dialogueActor);
                    return;
                }
            }
        }

        #endregion

        #region Typewriter Speed

        public virtual float GetTypewriterSpeed()
        {
            AbstractTypewriterEffect typewriter;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                typewriter = GetTypewriter(m_builtinPanels[i]);
                if (typewriter != null) return TypewriterUtility.GetTypewriterSpeed(typewriter);
            }
            typewriter = GetTypewriter(m_defaultNPCPanel);
            if (typewriter != null) return TypewriterUtility.GetTypewriterSpeed(typewriter);
            typewriter = GetTypewriter(m_defaultNPCPanel);
            return TypewriterUtility.GetTypewriterSpeed(typewriter);
        }

        public virtual void SetTypewriterSpeed(float charactersPerSecond)
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) TypewriterUtility.SetTypewriterSpeed(m_builtinPanels[i].subtitleText, charactersPerSecond);
            }
            if (m_defaultNPCPanel != null && !m_builtinPanels.Contains(m_defaultNPCPanel)) TypewriterUtility.SetTypewriterSpeed(m_defaultNPCPanel.subtitleText, charactersPerSecond);
            if (m_defaultPCPanel != null && !m_builtinPanels.Contains(m_defaultPCPanel)) TypewriterUtility.SetTypewriterSpeed(m_defaultPCPanel.subtitleText, charactersPerSecond);
        }

        private AbstractTypewriterEffect GetTypewriter(StandardUISubtitlePanel panel)
        {
            return (panel != null) ? TypewriterUtility.GetTypewriter(panel.subtitleText) : null;
        }

        #endregion

    }

}

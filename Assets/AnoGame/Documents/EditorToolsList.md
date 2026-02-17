# エディタツール一覧

プロジェクト内のカスタムエディタツール・拡張の棚卸しリスト。

---

## メニューアイテム（EditorWindow / ユーティリティ）

| メニューパス | スクリプト | 種別 | 説明 |
|---|---|---|---|
| `AnoGame/AnoFlow/Event Graph` | EventGraphWindow.cs | EditorWindow | イベント依存関係のグラフ可視化ツール |
| `Tools/SLFB Event Generator` | EventGeneratorWindow.cs | EditorWindow | イベントデータの一括生成 |
| `Tools/SLFB Item Generator` | ItemGeneratorWindow.cs | EditorWindow | アイテムデータの一括生成 |
| `Tools/Procedural Placement Tool` | ProceduralPlacementTool.cs | EditorWindow | プロシージャル配置ツール |
| `Tools/Procedural Line Placement` | ProceduralLinePlacementTool.cs | EditorWindow | ライン上のプロシージャル配置 |
| `Tools/Object Toggler` | ObjectTogglerWindow.cs | EditorWindow | オブジェクトの表示切替 |
| `Tools/Object Pinger` | ObjectPingerWindow.cs | EditorWindow | オブジェクトのPing |
| `Tools/Object Mover` | ObjectMoverWindow.cs | EditorWindow | オブジェクトの移動 |
| `Tools/AnoGame/Reset Save Data` | GameManagerEditor.cs | MenuItem | セーブデータリセット |
| `Tools/Steam/Reset All Steam Stats...` | ResetSteamStatsEditor.cs | MenuItem | Steam実績・統計リセット |
| `AnoGame/Tools/不正なReceptorを検出` | FixInvalidReceptors.cs | MenuItem | 不正Receptorの検出（ドライラン） |
| `AnoGame/Tools/不正なReceptorを無効化して修正` | FixInvalidReceptors.cs | MenuItem | 不正Receptorの自動修正 |
| `AnoGame/Tools/Set Timeline Track Bindings` | TimelineBindingSetter.cs | MenuItem | タイムライントラックバインディング設定 |
| `Assets/AnoGame/EventData/選択にGUIDを再割当` | EventDataEditor.cs | MenuItem | EventDataへのGUID再割当 |
| `Assets/AnoGame/EventData/選択で空欄のみGUID自動生成` | EventDataEditor.cs | MenuItem | 空欄EventDataへのGUID自動生成 |
| `Tools/Quick Component Adder` | QuickComponentAdderWindow.cs | EditorWindow | コンポーネントのクイック追加（最近使用＋ピン留め） |

---

## カスタムインスペクター（CustomEditor）

| 対象クラス | スクリプト | 配置 |
|---|---|---|
| GameManager | GameManagerEditor.cs | Editor/ |
| PlayableDirector | PlayableDirectorEditor.cs | AnoUtility/Editor/ |
| MonoBehaviour (ButtonAttribute) | ButtonAttributeEditor.cs | Editor/ |
| EnemyBehaviorCoordinator | ComponentDescriptionEditor.cs | Editor/Attributes/ |
| ItemData | ItemDataEditor.cs | Editor/ |
| InstantEventTrigger | InstantEventTriggerEditor.cs | Editor/ |
| TreeFeller | TreeFellerEditor.cs | Editor/ |
| NoiseTextureGenerator | NoiseTextureGeneratorEditor.cs | Editor/ |
| EventLockControl | EventLockControlEditor.cs | Application/Player/Editor/ |
| InventoryItemClip | InventoryItemClipEditor.cs | Application/Direction/Timeline/Editor/ |
| EventData | EventDataEditor.cs | Application/Data/Editor/ |
| AnoNarrativeClip | AnoNarrativeClipEditorUITK.cs | AnoDialogue/Scripts/Editor/ (UI Toolkit) |
| ItemDatabase | ItemDatabaseEditor.cs | Application/Data/Editor/ |
| StoryData | StoryData.cs (内部クラス) | Application/Data/ |
| PlayerSpawnManager | PlayerSpawnManager.cs (内部クラス) | Application/Story/ |
| ObjectMover | ObjectMover.cs (内部クラス) | Application/Common/Movement/ |
| LoreSceneManager | LoreSceneManager.cs (内部クラス) | Application/Lore/ |
| CanvasGroup | CanvasGroupEditor.cs | AnoUtility/Editor/ |

---

## PropertyDrawer

| スクリプト | 配置 | 説明 |
|---|---|---|
| ConditionDrawers.cs | Editor/PropertyDrawers/ | Condition系PropertyDrawer |
| EventSelectorDrawer.cs | Editor/PropertyDrawers/ | EventIDのドロップダウン選択 |
| ItemSelectorDrawer.cs | Editor/PropertyDrawers/ | ItemIDのドロップダウン選択 |

---

## バッチ処理

| スクリプト | 配置 | 説明 |
|---|---|---|
| BatchEventCreator.cs | Application/Event/Editor/ | イベントの一括作成（V1） |
| BatchEventCreatorV2.cs | Application/Event/Editor/ | イベントの一括作成（V2） |
| BatchItemCreator.cs | Application/Data/Editor/ | アイテムの一括作成 |

---

## AnoFlow EventGraph

| スクリプト | 説明 |
|---|---|
| EventGraphWindow.cs | メインウィンドウ・ツールバー |
| EventGraphView.cs | GraphViewの実装・レイアウト |
| EventNodeView.cs | ノードの描画・インタラクション |
| EventGraphMeta.cs | ノード位置のメタデータ永続化 |
| EventGraphStyles.uss | USSスタイルシート |
| EventSceneBinder.cs | シーン内オブジェクトとの連携 |

---

## タイムライン関連エディタ

| スクリプト | 配置 | 説明 |
|---|---|---|
| StickyTimelinePreview.cs | Application/Direction/Timeline/Editor/ | タイムラインプレビュー補助 |
| TimelineReflectionUtility.cs | Application/Direction/Timeline/Editor/ | タイムラインリフレクションユーティリティ |

---

## その他

| スクリプト | 配置 | 説明 |
|---|---|---|
| AlignSceneViewToMainCamera.cs | Editor/ | SceneViewをメインカメラに合わせる |
| ProjectRegenerator.cs | Editor/ | プロジェクトファイル再生成 |
| ScriptableObjectToAsset.cs | Editor/ | ScriptableObjectのアセット変換 |

---

## 無効化済み（コメントアウト）

| メニューパス | スクリプト | 備考 |
|---|---|---|
| ~~Tools/ItemEditor~~ | ItemEditorWindow.cs | コメントアウト |
| ~~Tools/EventEditor~~ | EventEditorWindow.cs | コメントアウト |

# イベント配置モード — 要件整理・調査レポート

## 1. 現在のイベント構成

### 1.1 シーン上のオブジェクト階層

```
GeneratedEventsV2 (Root)
└── EV_xxx_{EventName} (Container — AnoEventRoot)
    ├── EV_xxx_Receptor_{Category} (Receptor)
    │   └── ContactReceptor / InspectReceptor / ItemReceptor
    └── EV_xxx_Trigger (Trigger)
        ├── InstantEventTrigger
        ├── TimelineController
        └── PlayableDirector (.playable アセット参照)
```

### 1.2 データフロー

```
Receptor → IEventService.TriggerEventStart(eventId)
    → EventTriggerBase が検知 → OnStartEvent()
        → InstantEventTrigger: 即時クリア + EventManager に記録
        → TimelineController.Enqueue() → TimelineQueueManager で再生
```

### 1.3 主要クラス一覧

| クラス | パス | 役割 |
|---|---|---|
| `AnoEventRoot` | Application/Event/ | イベントオブジェクトのハブ。EventData・Receptor・Triggerの参照を保持 |
| `ContactReceptor` | Application/Event/ | 距離ベースの自動トリガー |
| `InspectReceptor` | Application/Event/ | 「調べる」インタラクション（IInteractable実装） |
| `ItemReceptor` | Application/Event/ | アイテム使用検知 |
| `InstantEventTrigger` | Application/Event/ | イベント検知→即時クリア＋Timeline実行 |
| `TimelineController` | Application/Event/ | PlayableDirector管理＋キュー登録 |
| `EventTriggerBase` | Application/Event/ | Trigger共通基盤（条件チェック・イベント購読） |
| `EventData` | Application/Data/ | ScriptableObject。eventId, 条件, タグ等 |
| `EventSceneBinder` | Editor/UIToolkit/ | eventId→シーンオブジェクト検索＋Ping（名前規約ベース） |
| `EventGraphMeta` | Editor/UIToolkit/ | ノード座標・SceneBinding永続化（JSON） |
| `BatchEventCreatorV2` | Event/Editor/ | JSON→EventData＋シーンオブジェクト一括生成 |

### 1.4 テンプレートプレハブ

| プレハブ名 | サイズ | 用途 |
|---|---|---|
| `Tpl_ContactZone.prefab` | 2.3KB | ContactReceptor付きゾーン |
| `Tpl_InspectZone.prefab` | 1.4KB | InspectReceptor付きゾーン |
| `Tpl_ItemZone.prefab` | 1.4KB | ItemReceptor付きゾーン |
| `Tpl_Trigger.prefab` | 2.8KB | InstantEventTrigger + TimelineController + PlayableDirector |

### 1.5 .playable ファイル統計

- **総数**: 97ファイル
- **合計**: 約1.15MB
- **平均**: 約12KB/ファイル

---

## 2. 要件: イベント配置モード

### 2.1 概要

SceneView上でのワンクリック・イベント配置ツール。AnoEventRootVisualizerやPModeのように、シーンビュー上で完結する操作を目指す。

### 2.2 機能要件

| # | 要件 | 詳細 |
|---|---|---|
| R1 | **配置モードの切り替え** | ツールバーボタンまたはショートカットで「イベント追加モード」をON/OFF |
| R2 | **クリック配置** | モード中にSceneView上をクリック → Receptor種別選択ドロップダウン → 配置 |
| R3 | **自動インクリメント** | eventIdを自動で連番生成（現在のフォルダ内の最大IDから+1） |
| R4 | **EventDataの自動生成** | EventData(.asset)を現在のEventGraphフォルダに自動作成 |
| R5 | **オブジェクト構造の自動構築** | Container > Receptor + Trigger の階層を自動生成（テンプレートPrefab利用） |
| R6 | **EventGraphとの連携** | 生成したイベントをEventGraphMeta.sceneBindingsに自動登録 |
| R7 | **Ping連携** | EventGraphノード ↔ シーンオブジェクト間の双方向Ping |
| R8 | **.playable自動生成** | テンプレートの.playableを複製して自動割り当て（後述） |

### 2.3 配置フロー（案）

```
1. イベント追加モードON
2. SceneView上をクリック
3. Receptor種別選択のGenericMenuが表示
   - ContactReceptor (距離トリガー)
   - InspectReceptor (調べる)
   - ItemReceptor (アイテム使用)
4. 選択するとクリック位置に以下が生成:
   a. Container (AnoEventRoot) — 自動命名: EV_xxx_{category}
   b. Receptor子オブジェクト — テンプレートPrefabからインスタンス化
   c. Trigger子オブジェクト — Tpl_Trigger から生成
   d. EventData (.asset) — フォルダに作成
   e. .playable ファイル — テンプレートから複製
5. AnoEventRoot.Setup() で参照を自動セット
6. EventGraphMetaにSceneBinding登録
7. 必要ならEventGraphノードとして可視化
```

---

## 3. Q&A: 検討事項

### Q1: シーンオブジェクト削除時にイベントデータも消えるようにできるか？

**結論: 技術的には可能だが、安全装置が必要。**

方法案:
- **A. `OnDestroy` + EditorCallbacks**: AnoEventRoot の `OnDestroy` でエディタ時に EventData.asset を削除する。ただし誤操作リスクが高い。
- **B. 同期チェックツール**: 定期的に「シーンに存在しないEventData」を検出して一覧表示し、ユーザーが選択して削除する方式。よりウトなアプローチ。
- **C. ソフト削除フラグ**: EventDataに `IsDeleted` フラグ（既に存在する）を立てるだけで、実体は残す。EventGraphで非表示フィルタ。

> [!IMPORTANT]
> **推奨: 案B + 案Cの併用。** 自動削除は事故リスクが高いため、ソフト削除＋定期クリーンアップが安全です。

### Q2: テンプレート .playable は必要か？

**結論: はい。最小限のテンプレートを1つ用意すべき。**

根拠:
- `TimelineController` は `PlayableDirector` を `RequireComponent` しており、`.playable` (TimelineAsset) がないとキューに登録できない
- テンプレートには会話（AnoDialogue）トラックだけ入れた最小構成が望ましい
- 自動生成時にテンプレートを `AssetDatabase.CopyAsset` で複製すれば、各イベント固有の `.playable` ファイルが最小コストで作成できる

### Q3: 新しいReceptorでTimelineControllerを直接呼ぶことについて

**検討: 「会話専用Receptor」→ TimelineControllerを直呼び**

現在のフロー:
```
InspectReceptor → IEventService.TriggerEventStart()
  → InstantEventTrigger.OnStartEvent()
    → TimelineController.Enqueue()
```

提案フロー:
```
新Receptor → 直接 TimelineController.Enqueue() + EventService通知
```

| 観点 | 評価 |
|---|---|
| **シンプルさ** | Trigger/EventTriggerBaseの中間層がなくなり簡潔 |
| **EventDataとの整合性** | 条件チェック・ResultTagsの処理をReceptor側で完結させる必要あり |
| **既存システムとの共存** | 両方式を混在させると管理が複雑化するリスク |
| **スキップ対応** | TimelineController.OnSkip の呼び出しタイミングに注意 |

> [!WARNING]
> EventTriggerBaseには条件チェック・イベントライフサイクル管理（Start/Finish/Done/Failed）が含まれており、これをReceptor側で再実装するコストが発生します。既存のフローを維持しつつ、生成の自動化で煩雑さを解消するアプローチが安全です。

### Q4: .playable ファイル数が増えることへの懸念

**結論: 現状ではそこまで気にする必要はない。**

- 平均 12KB/ファイルと軽量
- 97ファイルでも合計約1.15MB
- 仮に300ファイルになっても約3.5MB — ビルドサイズへの影響は軽微
- Metaファイル分のGit管理コストはあるが、問題になるレベルではない

> [!NOTE]
> ただし「ドアが開かない」のような短い固定演出を共有テンプレートで運用するアプローチも有効です。カテゴリ別テンプレート（会話用、短演出用等）を用意して選択できると、ファイル増加を抑えつつ柔軟性も確保できます。

---

## 4. 実装方針の選択肢

### 案A: AnoEventRootVisualizer拡張型

既存の `AnoEventRootVisualizer` のオーバーレイに「追加モード」ボタンを設け、SceneGUI上で配置ロジックを追加する。

| 利点 | 欠点 |
|---|---|
| 既存UI/UX との統一感 | AnoEventRootVisualizer.cs がさらに肥大化 |
| 可視化と配置が同一ツール内 | 責務の分離が甘くなる |

### 案B: 独立エディタツール型

新規に `AnoEventPlacementTool.cs` を作成。SceneView Overlayとして独立実装。

| 利点 | 欠点 |
|---|---|
| 責務が明確 | ツール切り替えが必要 |
| テスト・メンテが容易 | AnoEventRootVisualizerとの連携が間接的 |

### 案C: ハイブリッド型（推奨）

配置モードのコアロジックは `EventPlacementService` として独立クラスに分離。AnoEventRootVisualizerのOverlayから呼び出す形。SceneGUIのイベントハンドリングも共有。

| 利点 | 欠点 |
|---|---|
| 既存UIからアクセス可能 | 初期実装コスト |
| ロジック再利用可能 | — |
| テスト容易 | — |

---

## 5. 次のステップ

- [ ] 方針の選定（A/B/C）
- [ ] テンプレート .playable ファイルの準備
- [ ] 新Receptor（会話直接実行型）の要否決定
- [ ] シーンオブジェクト削除時の連動挙動の仕様決定
- [ ] 実装計画の作成

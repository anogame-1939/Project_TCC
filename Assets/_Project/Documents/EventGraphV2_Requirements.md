# EventGraphViewV2 改良要件書

## 1. 背景と現状

### 現在のシステム構成

```
events_story2.json (EventJsonItem形式)
    ↓ Load/Save
EventGraphWindowV2 → EventGraphViewV2 → EventNodeView
    ↓ 別プロセス
BatchEventCreatorV2 → EventData (ScriptableObject) → シーンオブジェクト生成
```

### 現在のデータモデルの乖離

| 項目 | EventJsonItem (Graph Editor) | EventData (ScriptableObject) | BatchCreatorV2の内部EventJsonItem |
|------|------------------------------|-------------------------------|----------------------------------|
| 条件 | `conditions` (文字列リスト: State_*, Has_* 混在) | `requiredItemIds` + `requiredEventIds` (分離済) | `requiredItemIds` + `requiredEventIds` + `results` |
| 結果 | `results` (文字列リスト) | なし | `results` |
| 追加フィールド | なし | `isOneTime`, `category` | `paramText`, `paramItemId`, `timeline` |

**課題**: Graph Editorの`EventJsonItem`とBatchCreatorの`EventJsonItem`は同じJSONを読んでいるが、フィールド構造が異なる。EventData (ScriptableObject) が最終的なランタイムデータソースだが、Graph上では直接参照していない。

---

## 2. 改良目標

### Phase 1: EventDataベースのグラフ可視化
EventData (ScriptableObject) を直接参照してノードを構築し、イベント間の依存関係・実行可能性を可視化する。

### Phase 2: ノードとシーンオブジェクトの紐づけ
ノードをクリックしたら、対応するシーン上のGameObject (Receptor/Trigger) をPingで示す。

---

## 3. Phase 1: EventDataベースのグラフ可視化

### 3.1 データソースの変更

**現状**: `events_story2.json` → `EventJsonItem` → ノード生成
**目標**: `EventData` (ScriptableObject) アセット群 → ノード生成

#### 変更箇所

1. **EventGraphViewV2**: `PopulateGraph(EventList)` → `PopulateGraph(List<EventData>)` に変更
2. **EventNodeView**: `EventJsonItem` の参照を `EventData` (ScriptableObject) に変更
3. **EventGraphWindowV2**: JSONロードではなく、`Assets/AnoGame/Data/Events/Story2/` フォルダからEventDataアセットを読み込む

#### 利点
- JSONとScriptableObjectの二重管理を解消
- ランタイムで実際に使われるデータをそのまま可視化
- `requiredItemIds` / `requiredEventIds` の分離された条件をそのまま表示可能

### 3.2 ノード表示の改良

現在のEventNodeViewに加え、以下の情報を表示する:

```
┌─────────────────────────────┐
│ ◉ In                         │
├─────────────────────────────┤
│   EV_013_Get_KeyChain        │
│   キーホルダーのついた鍵入手  │
├─────────────────────────────┤
│ ID:       EV_013_Get_KeyChain│
│ Name:     キーホルダー…      │
│ Category: 井戸               │
│ Desc:     井戸イベントクリア… │
├─────────────────────────────┤
│ Required Events:             │
│  (なし)                      │
│ Required Items:              │
│  • Has_Magnet [✓ 存在]       │ ← アイテムマスタとの照合
├─────────────────────────────┤
│ Results:                     │
│  • Has_KeyHolderKey          │
├─────────────────────────────┤
│                    Out ◉     │
└─────────────────────────────┘
```

#### 表示要素

| セクション | 内容 | データソース |
|-----------|------|-------------|
| ヘッダー | eventId + eventName | `EventData.EventId`, `EventData.EventName` |
| 基本情報 | ID, Name, Category, Description | `EventData` の各プロパティ |
| Required Events | 必要なイベント条件一覧 | `EventData.RequiredEventIds` |
| Required Items | 必要なアイテム条件一覧 | `EventData.RequiredItemIds` |
| Results | イベント完了時の結果 | ※ EventDataにresultsフィールドを追加するか、JSON参照を併用 |

### 3.3 エッジ(接続線)の生成ロジック

EventData同士の依存関係をエッジで表現する:

1. **イベント条件エッジ**: `EventData.RequiredEventIds` に含まれるeventIdを持つEventDataノード → 当ノード
2. **アイテム条件エッジ**: あるEventDataの`results`に含まれるアイテム/ステートが、別EventDataの`requiredItemIds`に一致 → エッジ生成
3. **ステート依存エッジ**: results内のState_*が別イベントのconditionsに一致する場合

#### 課題: results フィールドの扱い

現在の `EventData` (ScriptableObject) には `results` フィールドが存在しない。

**選択肢:**
- **A案**: EventData に `results` フィールドを追加する (推奨)
- **B案**: results は JSON (events_story2.json) から補完的に読み込む
- **C案**: BatchEventCreatorV2の段階でresultsもEventDataに書き込む

**推奨: A案** — EventDataに`List<string> results`を追加し、BatchCreatorV2でresultsも書き込むようにする。これによりEventDataだけで完結する。

### 3.4 実行可能性の可視化

アイテムデータ (`items_batch.json`) とイベントの状態を参照し、各イベントの実行可能性をノード上で表示する。

#### 表示仕様
- **条件充足**: 各conditionの横に `✓` / `✗` アイコンを表示
- **ノード色**:
  - 全条件充足: 緑系のボーダー
  - 一部充足: 黄系のボーダー
  - 未充足: デフォルト(灰色)
- **アイテム存在確認**: `items_batch.json` のアイテムIDと照合し、定義されているアイテムかどうかを表示

#### 判定ロジック
```
foreach condition in EventData.RequiredItemIds:
    itemExists = items_batch.json に itemId が存在するか
    → ノード上に [✓存在] / [✗未定義] を表示

foreach condition in EventData.RequiredEventIds:
    eventExists = EventDataアセット群に eventId が存在するか
    → ノード上に [✓存在] / [✗未定義] を表示
```

> 注意: これはエディタ上での「定義の整合性チェック」であり、ランタイムの「クリア済み判定」とは異なる。

---

## 4. Phase 2: ノードとシーンオブジェクトの紐づけ

### 4.1 目的

EventGraphのノードをクリック/選択したとき、対応するシーン上のGameObjectをPingで強調表示する。

### 4.2 シーン上のオブジェクト構造

`Chapter2-0` シーンの `GeneratedEvents` 配下:

```
GeneratedEvents/
├── EV_002_StreetlightStop_街灯で足止め/
│   ├── EV_002_StreetlightStop_Receptor_Contact
│   └── EV_002_StreetlightStop_Trigger
├── EV_005_Get_Magnet_磁石入手/
│   ├── EV_005_Get_Magnet_Receptor_Inspect
│   └── EV_005_Get_Magnet_Trigger
└── ...
```

### 4.3 紐づけ方式

#### Option A: 名前規則ベース (シンプル)

EventData.EventId をキーにして、シーン上の `GeneratedEvents` 配下から名前が `EventId` で始まるGameObjectを検索する。

```csharp
// 疑似コード
var root = GameObject.Find("GeneratedEvents");
foreach (Transform child in root.transform)
{
    if (child.name.StartsWith(eventData.EventId))
    {
        EditorGUIUtility.PingObject(child.gameObject);
    }
}
```

**利点**: 追加のメタファイル不要。命名規則さえ守れば動作する。
**欠点**: 命名規則が変わると壊れる。複数シーンをまたぐ場合の対応が必要。

#### Option B: メタファイル (EventGraphMeta.json) による紐づけ

イベントノードとシーンオブジェクトのマッピングをメタファイルで管理する。

```json
{
  "sceneName": "Chapter2-0",
  "mappings": [
    {
      "eventId": "EV_002_StreetlightStop",
      "sceneObjects": [
        {
          "path": "GeneratedEvents/EV_002_StreetlightStop_街灯で足止め",
          "type": "container"
        },
        {
          "path": "GeneratedEvents/EV_002_StreetlightStop_街灯で足止め/EV_002_StreetlightStop_Receptor_Contact",
          "type": "receptor"
        },
        {
          "path": "GeneratedEvents/EV_002_StreetlightStop_街灯で足止め/EV_002_StreetlightStop_Trigger",
          "type": "trigger"
        }
      ]
    }
  ],
  "nodePositions": {
    "EV_002_StreetlightStop": { "x": 100, "y": 200 }
  }
}
```

**利点**:
- 命名規則に依存しない
- ノード位置情報もメタファイルに保存可能 (AutoLayoutに頼らず位置を保持)
- 複数シーンへの対応が容易
- HierarchyResults JSONからの自動生成が可能

**欠点**: メタファイルの管理が増える

#### Option C: ハイブリッド方式 (推奨)

1. **基本**: 名前規則ベースで自動検索 (Option A)
2. **補助**: メタファイルでノード位置・追加マッピング情報を管理 (Option Bの一部)
3. **自動生成**: `BatchEventCreatorV2` 実行時にメタファイルも自動更新

### 4.4 Ping動作の仕様

| アクション | 挙動 |
|-----------|------|
| ノードをシングルクリック | コンテナGameObjectをPing |
| ノードをダブルクリック | コンテナGameObjectを選択 + Hierarchy上でフォーカス |
| ノード右クリック → "Ping Receptor" | Receptor GameObjectをPing |
| ノード右クリック → "Ping Trigger" | Trigger GameObjectをPing |

### 4.5 シーンの特定

- 現在ロードされているシーンから `GeneratedEvents` (または `GeneratedEventsV2`) ルートを検索
- 見つからない場合はメタファイルのsceneNameを参照してシーンをロード提案

---

## 5. メタファイル仕様 (EventGraphMeta.json)

### 保存場所
`Assets/AnoGame/Data/Events/Story2/EventGraphMeta.json`

### スキーマ

```json
{
  "version": 1,
  "storyId": "Story2",
  "eventDataFolder": "Assets/AnoGame/Data/Events/Story2",
  "targetScenes": ["Chapter2-0"],
  "nodePositions": {
    "<eventId>": { "x": 0, "y": 0 }
  },
  "sceneBindings": {
    "<eventId>": {
      "sceneName": "Chapter2-0",
      "containerPath": "GeneratedEvents/<containerName>",
      "receptorName": "<receptorObjectName>",
      "triggerName": "<triggerObjectName>"
    }
  }
}
```

### 用途
1. **nodePositions**: AutoLayout後のノード位置を永続化 (リロード時に復元)
2. **sceneBindings**: EventIDとシーンオブジェクトのパスマッピング
3. **targetScenes**: このストーリーに関連するシーン名

---

## 6. 実装タスク一覧

### Phase 1: EventDataベース化

| # | タスク | 対象ファイル | 概要 |
|---|--------|-------------|------|
| 1-1 | EventDataにresultsフィールド追加 | `EventData.cs` (ScriptableObject) | `List<string> results` を追加 |
| 1-2 | BatchCreatorV2でresults書き込み | `BatchEventCreatorV2.cs` | CreateEventData内でresultsも書き込む |
| 1-3 | EventNodeViewをEventData参照に変更 | `EventNodeView.cs` | `EventJsonItem` → `EventData` (SO) を参照 |
| 1-4 | EventGraphViewV2のデータソース変更 | `EventGraphViewV2.cs` | `PopulateGraph(List<EventData>)` に変更 |
| 1-5 | EventGraphWindowV2のロード処理変更 | `EventGraphWindowV2.cs` | JSONロードからEventDataアセットロードに変更 |
| 1-6 | ノード上にRequiredEvents/Items/Results表示 | `EventNodeView.cs` | 分離された条件セクション表示 |
| 1-7 | エッジ生成ロジックの更新 | `EventGraphViewV2.cs` | EventData.RequiredEventIds + results マッチング |
| 1-8 | 条件の整合性表示 (✓/✗) | `EventNodeView.cs` | アイテム/イベントの存在確認表示 |

### Phase 2: シーンオブジェクト紐づけ

| # | タスク | 対象ファイル | 概要 |
|---|--------|-------------|------|
| 2-1 | メタファイルのスキーマ定義・クラス作成 | 新規: `EventGraphMeta.cs` | Serializable メタデータクラス |
| 2-2 | メタファイルの読み書きユーティリティ | 新規 or `EventGraphWindowV2.cs` | Load/Save処理 |
| 2-3 | ノードクリック時のPing処理 | `EventNodeView.cs` or `EventGraphViewV2.cs` | Selection/Ping呼び出し |
| 2-4 | シーンオブジェクト検索ロジック | 新規: `EventSceneBinder.cs` | 名前規則 + メタファイルでGameObject検索 |
| 2-5 | ノード右クリックメニュー追加 | `EventNodeView.cs` | ContextMenu: Ping Receptor/Trigger |
| 2-6 | ノード位置の保存・復元 | `EventGraphViewV2.cs` + `EventGraphWindowV2.cs` | メタファイルへの位置永続化 |
| 2-7 | BatchCreatorV2でメタファイル自動更新 | `BatchEventCreatorV2.cs` | CreateEventSet時にsceneBindings更新 |

---

## 7. 依存関係・実行順序

```
Phase 1:
  1-1 → 1-2 (resultsフィールド追加してからBatchCreatorで書き込み)
  1-3, 1-4, 1-5 は並行可能 (ただし1-3と1-4は密結合)
  1-6, 1-7, 1-8 は 1-3〜1-5 完了後

Phase 2:
  2-1 → 2-2 → 2-3, 2-4 (メタクラスと読み書きが先)
  2-5 は 2-3, 2-4 完了後
  2-6 は独立
  2-7 は 2-1, 2-2 完了後
```

---

## 8. 注意事項

### データ移行
- Phase 1実装後、`Tools/Run Batch Event Creation V2` を再実行してEventDataアセットにresultsを書き込む必要がある
- 既存のevents_story2.jsonは引き続きマスターデータとして保持可能 (BatchCreatorの入力)

### 互換性
- EventGraphWindowV2のSave機能はPhase 1後に変更が必要 (JSONではなくEventDataアセットへの書き込みになるか、あるいはグラフは読み取り専用にするか検討)
- 現在のJSON編集フローとの共存を検討

### Graph Editor の編集可否
- **方針A**: Graph EditorはEventDataの読み取り専用ビューとする (データ編集はInspectorまたはJSON→BatchCreator経由)
- **方針B**: Graph EditorからEventDataを直接編集可能にする (SerializedObjectを介して)
- **推奨: 方針A** を初期実装とし、必要に応じてBに移行

---

## 9. 将来の拡張案

- ランタイムでのイベントクリア状態とグラフの連動 (PlayMode時にクリア済みノードの色が変わる)
- 複数ストーリー / 複数シーンへの対応
- ノードのグルーピング (Category別の囲み)
- ミニマップ表示
- 検索・フィルタ機能

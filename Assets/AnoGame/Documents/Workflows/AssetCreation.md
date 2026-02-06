# アイテム・イベント作成ワークフロー

スプレッドシート等の外部定義データから、ゲーム内のアイテムデータやイベントオブジェクトを一括生成する手順書です。

## 1. データの準備
以下のフォルダにあるJSONファイルを編集・更新してください。
**フォルダ**: `Assets/AnoGame/Data/ItemsResources/`

- **アイテムデータ**: `items_batch.json`
- **イベントデータ**: `events_batch.json`

## 2. アイテムの一括作成
（変更なし：`ItemData` 作成とDB登録）
1. `items_batch.json` を保存。
2. **Tools > Run Batch Item Creation** を実行。

## 3. イベントの一括作成

### 3.1 処理の流れ
ツールは以下の順序で処理を行います。
1. **`EventData` アセットの作成**:
   JSON定義に基づき `EventData` (ScriptableObject) を作成・保存します。
2. **プレハブの配置**:
   カテゴリに応じた「Receptor（入り口）」と「Trigger（実行）」をシーンに配置します。
3. **Timelineの割り当て**:
   Triggerに対して `.playable` アセットを割り当てます。

### 3.2 カテゴリとプレハブの対応マッピング
CSV/JSONの `category` に応じて、使用するテンプレートプレハブが自動決定されます。

| カテゴリ | 使用プレハブ (Receptor) | 用途 |
| :--- | :--- | :--- |
| **Event** | `Tpl_ContactZone` | 接触・接近で発生するイベント |
| **Gimmick** | `Tpl_ContactZone` | 接触・接近で発生するギミック |
| **Background** | `Tpl_ContactZone` | 環境演出など |
| **Object** | `Tpl_InspectZone` | 調べて発生（扉、井戸など） |
| **Inspect** | `Tpl_InspectZone` | 調べてテキスト表示など |
| **ItemGet** | `Tpl_InspectZone` | 調べてアイテム入手 |
| **Action** | `Tpl_ItemZone` | 特定アイテム使用で発生 |
| **System** | `Tpl_Trigger` (Receptorなし) | 条件達成時の自動実行など（実体のみ） |

**必須プレハブ** (`Assets/AnoGame/Prefabs/EventZone/`):
- `Tpl_ContactZone.prefab`
- `Tpl_InspectZone.prefab`
- `Tpl_ItemZone.prefab`
- `Tpl_Trigger.prefab` (全てのイベントでペアとして生成)

### 3.3 手順
1. `events_batch.json` を保存します。
2. Unityメニューの **Tools > Run Batch Event Creation** をクリックします。
3. コンソールに `Batch Creation Complete!` と表示されたら完了です。

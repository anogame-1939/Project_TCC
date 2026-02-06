# アイテム・イベント作成ワークフロー

スプレッドシート等の外部定義データから、ゲーム内のアイテムデータやイベントオブジェクトを一括生成する手順書です。

## 1. データの準備
以下のフォルダにあるJSONファイルを編集・更新してください。
**フォルダ**: `Assets/AnoGame/Data/ItemsResources/`

- **アイテムデータ**: `items_batch.json`
- **イベントデータ**: `events_batch.json`

## 2. アイテムの一括作成
アイテムリスト (`items_batch.json`) を元に `ItemData` を作成し、データベースに登録します。

**手順**:
1. `items_batch.json` を保存します。
2. Unityメニューの **Tools > Run Batch Item Creation** をクリックします。
3. コンソールに `Batch Creation Complete!` と表示されたら完了です。

## 3. イベントの一括作成
イベントリスト (`events_batch.json`) を元に、シーン上にイベントオブジェクトを配置します。

**前提**:
以下のプレハブが `Assets/AnoGame/Prefabs/EventZone/` に存在することを確認してください。
- `Tpl_ContactZone.prefab`: 接触で起動するイベント（入り口）
- `Tpl_InspectZone.prefab`: 調べて起動するイベント（入り口）
- `Tpl_ItemZone.prefab`: アイテム使用で起動するイベント（入り口）
- `Tpl_Trigger.prefab`: イベント実体（判定・Timeline制御など）

**生成ロジック**:
ツールは1つのイベント定義に対し、常に**2つのオブジェクト（ペア）**を作成します。
1. **Receptor**: プレイヤーのインタラクション（接触・調べる等）を受け付けるオブジェクト。
2. **Trigger**: イベントIDやTimeline情報を持ち、実際にイベント処理を行うオブジェクト。
これにより、**「入り口（Receptor）」→「実行（Trigger）」**の1対1の関係が構築されます。

**手順**:
1. `events_batch.json` を保存します。
2. Unityメニューの **Tools > Run Batch Event Creation** をクリックします。
3. シーン上にオブジェクトが配置され、コンソールに `Complete` と表示されたら完了です。

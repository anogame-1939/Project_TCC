# プロジェクト固有ルール・用語対応規則

## データ構造の用語対応

このプロジェクトでは、ストーリー/シナリオの階層構造が複数の場所で異なる用語で管理されている。
混乱を避けるため、以下の対応関係を厳守すること。

### ストーリー構成と MasterDialogueData の対応

| ストーリー構成 | MasterDialogueData のフィールド | 説明 |
|---|---|---|
| **エピソード** | `EpisodeID` | シナリオの大分類（EP1, EP2, ...） |
| **チャプター** | `ChapterID` | エピソード内の章分け |
| **セクション** | `SectionID` | 個々の会話イベント単位 |

### チャプターとエピソードの対応

| ストーリーのチャプター | MasterDialogueData の EpisodeID | 補足 |
|---|---|---|
| Chapter 1 | `EpisodeID: 1` | OLの怪異 |
| Chapter 2-0 | `EpisodeID: 2` | 大男の庭園 |
| (汎用・共通) | `EpisodeID: -1` | アイテム取得・使用などの汎用テキスト |

> [!IMPORTANT]
> **「チャプター2-0」のイベントは `EpisodeID: 2`**。
> タイムラインの `AnoNarrativeClip.targetEpisode` には **2** を設定すること。
> 過去に `targetEpisode: 1` と誤設定されたケースがあるため注意。

### EventData と MasterDialogueData の ID対応

Chapter 2-0 では **`EV_XXX` の XXX 番号** と **MasterDialogueData の `SectionID`** が一致する設計になっている。

例:
- `EV_005_Get_Magnet` → `SectionID: 5`（磁石）
- `EV_013_Get_KeyChain` → `SectionID: 13`（キーホルダーのついた鍵）

### AnoNarrativeClip の設定値

Chapter 2-0 のイベントでは以下の値を使用：
- `targetEpisode`: **2**
- `targetChapter`: **-1**（全チャプター検索）
- `targetSection`: **-1**（全セクション検索）
- `conversationID`: MasterDialogueData の該当会話の **ID (GUID)** を指定
- `dialogueStyleName`: `"通常"`
- `pauseTimeline`: **1**（会話中はタイムライン一時停止）

---

## ファイル命名規則

| 種類 | 命名パターン | 例 |
|---|---|---|
| EventDataアセット | `EV_{番号3桁}_{英語名}.asset` | `EV_005_Get_Magnet.asset` |
| タイムラインアセット | `EV_{番号3桁}_{英語名}_Timeline.playable` | `EV_005_Get_Magnet_Timeline.playable` |

---

## 名前空間ルール

- `_Project` 内のスクリプトには `AnoGame` から始まる名前空間を必ずつけること

---

## コンパイルチェックルール（uLoopMCP）

C#スクリプトの作成・修正を行った際は、**必ず uLoopMCP を使用してコンパイルエラーの有無を確認すること**。

### 手順

1. スクリプトの作成・修正が完了したら、uLoopMCP の `refresh_and_check_errors` ツールを実行する
2. コンパイルエラーが報告された場合は、該当箇所を修正して再度チェックする
3. エラーが解消されるまで繰り返す

> [!IMPORTANT]
> uLoopMCP が接続されていない場合（Unityエディタ未起動等）は、その旨をユーザーに報告し、手動確認を促すこと。

---

## 会話・対話システム

- 会話・対話は **AnoNarative** を使用すること
- 旧 Pixel Crushers Dialogue System は使用しない
- 使い方の詳細は [`AnoNarrative/Docs/AnoNarativeGuide.md`](file:///c:/Users/krsmg/Documents/GitHub/Project_TCC/Assets/AnoNarrative/Docs/AnoNarativeGuide.md) を参照

---

## セリフ・テキストルール

### 女言葉の禁止

全キャラクター共通で、いわゆる「女言葉」は使用しない。

| ❌ 使用禁止 | ✅ 代替表現の例 |
|---|---|
| 「～だわ」 | 「～だよ」「～だね」 |
| 「～のよ」 | 「～んだよ」「～んだ」 |
| 「～かしら」 | 「～かな」「～だろうか」 |
| 「～ですわ」 | 「～ですよ」 |

> [!IMPORTANT]
> おばさんのセリフ「珍しいわ」等、一部既存セリフに残っている女言葉は今後修正対象とする。

### 句読点ルール

**方針：句読点をつける（統一）**

- セリフ内で読点「、」句点「。」を自然に使用する
- 短い独り言や感嘆（「うっ…」「え？」等）は句読点不要
- 改行で文を区切る場合も、文末には句点をつける

> [!NOTE]
> 既存セリフでは句読点の有無が混在している。新規セリフから統一し、既存セリフは順次修正する。

---

## イベントトリガーシステム

### 変遷

| 世代 | 使用箇所 | 方式 | 備考 |
|---|---|---|---|
| 第1世代 | ストーリー1 | `EventTriggerBase` 継承 + `InstantEventTrigger` | Collider依存、条件チェックが基底クラスに集約 |
| 第2世代 | ストーリー2（現行） | **Receptor系** + `InstantEventTrigger` | Collider不要、軽量・単機能 |

> [!IMPORTANT]
> 新規イベントの実装は **第2世代（Receptor系 + InstantEventTrigger）** を使用すること。
> `EventTriggerBase` を直接継承する方式はストーリー1のレガシーであり、新規では使用しない。

### 第1世代：EventTriggerBase 方式（ストーリー1）

- `EventTriggerBase`（基底クラス）に条件チェック・イベントライフサイクルを集約
- `InstantEventTrigger` が `EventTriggerBase` を継承し、開始と同時にクリア
- `SimpleEventTrigger`、`SimpleEnterEventTrigger` 等のサブクラスが存在
- Collider + UnityEvent ベースのトリガー方式

### 第2世代：Receptor 方式（ストーリー2・現行）

Collider不要の軽量コンポーネント群。各Receptorが「検知」を担当し、`IEventService.TriggerEventStart()` でイベントサービスに開始合図を送る。

| クラス | 役割 | トリガー条件 |
|---|---|---|
| `ContactReceptor` | 接近検知 | プレイヤーとの距離が `triggerDistance` 以下 |
| `InspectReceptor` | 調べるアクション | `IInteractable` 実装、`InteractionController` に自動登録 |
| `ItemReceptor` | アイテム使用検知 | アイテム消費イベントを購読、近接チェック付き |
| `InstantEventTrigger` | 即時クリア | イベント開始と同時にクリア済みにする（両世代で共用） |

### InstantEventTrigger のバージョン

| クラス | 備考 |
|---|---|
| `InstantEventTrigger` | 現行版。`targetEventId` から `EventData` を自動解決 |
| `InstantEventTrigger_V2` | 改良版（条件チェックを `Start` で実行）。チャプター3-1のみ使用 |
| `InstantEventTrigger_Old` | レガシー版。`Legacy/` フォルダに退避済み |

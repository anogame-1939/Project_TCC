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

---
description: イベントトリガー設定ワークフロー - タイムラインへの会話トラック・シグナルトラック・アイテムトラック設定手順
---

# イベントトリガー設定ワークフロー

Chapter 2-0 のイベントトリガー設定を行う際の標準手順を定義する。
既存のアイテム入手系イベント（EV_005, EV_006, EV_007等）の実装パターンを基に整理。

> **用語ルール**: [ProjectRules.md](file:///c:/Users/krsmg/Documents/GitHub/Project_TCC/Assets/AnoGame/Documents/ProjectRules.md) を参照。
> Chapter 2-0 = MasterDialogueData の **EpisodeID: 2**。SectionID = EV番号。

---

## 前提知識

### ファイル構成

| 種類 | パス |
|---|---|
| EventDataアセット | `Assets/AnoGame/Data/Events/Story2/EV_XXX_*.asset` |
| タイムラインアセット | `Assets/AnoGame/Data/ItemsResources/Timelines/EV_XXX_*_Timeline.playable` |
| MasterDialogueData | `Assets/AnoGame/DialogSettings/MasterDialogueData.asset` |
| シグナルアセット(イベント終了) | GUID: `04c4fcb71aec90d45838aef305c4cb2c` |

### タイムラインの基本構造

作成済みのアイテム入手イベント（例: EV_005_Get_Magnet）は以下の3トラック構成：

1. **Ano Narrative Track** (会話トラック)
   - Script GUID: `715762f68eae57048a1135c74ca1eb7d`
   - クリップ: `AnoNarrativeClip`（Script GUID: `2ed68ee5f3575f8439a90743bd341d1c`）
   - Start: 0, Duration: 0.5
   - `conversationID`: MasterDialogueDataの該当会話のID
   - `targetEpisode`: 2（Chapter 2-0の場合）
   - `targetChapter`: -1
   - `targetSection`: -1
   - `pauseTimeline`: 1（会話中タイムライン一時停止）
   - `dialogueStyleName`: "通常"
   - `styleDatabase`: GUID `f2be86183cd92044c884d519222012b3`

2. **Inventory Item Track** (アイテム取得トラック)
   - Script GUID: `d04636f37faf2a74da684ed27f2c5831`
   - クリップ: `InventoryItemClip`（Script GUID: `47a85bdf89c554540bed8c529840d8b1`）
   - Start: 0.5, Duration: 0.5
   - `itemData`: 対応するItemDataアセットへの参照

3. **Signal Track** (シグナル終了トラック)
   - Script GUID: `b46e36075dd1c124a8422c228e75e1fb`
   - Signal Emitter（Script GUID: `15c38f6fa1940124db1ab7f6fe7268d1`）
   - Time: 1.0（タイムライン終了時に発火）
   - Asset: "01.イベント終了" シグナル（GUID: `04c4fcb71aec90d45838aef305c4cb2c`）

---

## カテゴリ別の設定手順

### A. アイテム入手系（調べる/アイテム入手）

**対象イベント**: EV_005, EV_006, EV_007, EV_013, EV_016, EV_017, EV_021, EV_023, EV_026, EV_028

**必要トラック**: 3トラック（会話 + アイテム + シグナル）

#### 手順:
1. タイムラインアセットを開く
2. **Ano Narrative Track** を追加
   - AnoNarrativeClip を配置（Start: 0, Duration: 0.5）
   - `conversationID` にMasterDialogueDataの対応する会話IDを設定
   - `targetEpisode`: 2, `pauseTimeline`: 1
3. **Inventory Item Track** を追加
   - InventoryItemClip を配置（Start: 0.5, Duration: 0.5）
   - `itemData` に対応するItemDataアセットを参照設定
4. **Signal Track** を追加
   - Signal Emitter を配置（Time: 1.0）
   - Asset に "01.イベント終了" シグナルを設定

---

### B. 調べる系（テキスト表示のみ、アイテムなし）

**対象イベント**: EV_003, EV_008, EV_010, EV_018, EV_027

**必要トラック**: 2トラック（会話 + シグナル）

#### 手順:
1. タイムラインアセットを開く
2. **Ano Narrative Track** を追加（同上）
3. **Signal Track** を追加（同上）
4. Inventory Item Track は不要

---

### C. 接触系（ContactReceptor）

**対象イベント**: EV_001, EV_002, EV_009, EV_019

**必要トラック**: 1～2トラック（会話のみ仮設定）
- 強制移動イベントが良く挟まるため、**会話のみ仮設定**する
- Signal Track はシーン側のSignalReceiverで対応するため後で追加

#### 手順:
1. タイムラインアセットを開く
2. **Ano Narrative Track** を追加
   - AnoNarrativeClip を配置
   - `conversationID` を設定
3. 強制移動等の追加演出は後から手動で組み込む

---

### D. 専用インタラクト系

**対象イベント**: EV_004, EV_014, EV_015, EV_020, EV_022

→ **スキップ**（別途実装）

---

### E. アイテム使用系

**対象イベント**: EV_011, EV_012, EV_024, EV_025

**必要設定**: 会話 + シグナルレシーバー（Signal Track）のみ

#### 手順:
1. タイムラインアセットを開く
2. **Ano Narrative Track** を追加
   - AnoNarrativeClip を配置
   - `conversationID` を設定
3. **Signal Track** を追加
   - Signal Emitter を配置（Time: 1.0）
   - "01.イベント終了" シグナルを設定
4. アイテム消費等のロジックは別途C#で対応

---

### F. 連鎖系 / エンディング

**対象イベント**: EV_019（ひまわり男消失）, EV_029（エンディング）

→ **スキップ**（特殊処理のため別途実装）

---

## MasterDialogueData 会話IDマッピング

| EV_ID | SectionName | conversationID |
|---|---|---|
| EV_001 | 大男と初回遭遇 | `ffbb31e6-5c77-4c71-876b-25460b8c2ecb` |
| EV_002 | 街灯で足止め | `dcc3fd07-5a29-4d25-b930-2f0bbfad2703` |
| EV_003 | 電子ロック式扉１ | `e86c462d-8788-4a00-a12a-372e57d6a69a` |
| EV_004 | 電子ロック式扉１(開) | `f23ef0cb-54b7-4e49-bae4-2292dd3106be` |
| EV_005 | 磁石 | `19dfb92c-189c-4946-a021-3c4ea14282e0` |
| EV_006 | 制御盤の鍵 | `62eda147-ed83-4a23-a78a-aa639651ce46` |
| EV_007 | 管理人のメモ | `065a4921-fc4a-4625-9e44-d83549cfd6a1` |
| EV_008 | 金属プレート | `1e74b652-88eb-44b4-a156-1b803fba0701` |
| EV_009 | 霧壁の花 | `5846e8e7-508a-45e9-a0f8-d8d27f99dbe1` |
| EV_010 | 井戸 | `637dadcb-e95d-4ebf-98c5-901e5ccc38d9` |
| EV_011 | 井戸（タコ糸） | `601ce2a1-f40d-423b-9971-40c6ee6819db` |
| EV_012 | 井戸（磁石） | `dfe0017d-a0ab-4e59-b0fa-da34f1de709b` |
| EV_013 | キーホルダーのついた鍵 | `aa5a8fd0-ecbe-4a91-91c8-7e60b8e9838f` |
| EV_014 | 電子ロック式扉２ | `befcff06-d350-43e5-b14a-f2cfe5f874fb` |
| EV_015 | 電子ロック式扉３ | `ccf997b9-5f8c-408b-8c4b-7c4a915b2adb` |
| EV_016 | タコ糸 | `76660d2c-6b97-489c-aee8-4dcac6ae91dc` |
| EV_017 | 同僚のメモ | `37f070c3-5bf1-4f13-9162-d792b0fb6748` |
| EV_018 | 鍵のかかったロッカー | `728aad9c-130f-4652-b4fd-fb0d9084e980` |
| EV_019 | ひまわり男消失 | `9d20e298-1ca4-44fe-a601-749721a5b048` |
| EV_020 | 北の小屋の鍵開け | `2b2d88e0-42b4-40b6-a269-0179b1395bd2` |
| EV_021 | アルミボール | `58b10405-7617-4792-bf32-859fa7b02543` |
| EV_022 | 広場の鍵開け | `42d87c9f-df39-499d-a8b2-49a63eb397bb` |
| EV_023 | コーラ缶 | `ed6a9feb-381d-49f3-872b-551ea9e5c374` |
| EV_024 | 北の小屋：アイテム合成(コーラ) | `ea1f9ff5-844f-42de-a62e-c2ca9ace1f29` |
| EV_025 | 北の小屋：アイテム合成(アルミ) | `c6800c65-2e6f-4508-80bc-beb1bbcc0b9a` |
| EV_026 | ロッカーの鍵の番号 | `0adef27f-e19b-43e0-8839-0036d0c86ecc` |
| EV_027 | 南の小屋：ロッカー | `a5b92e3f-2ac5-4de1-9c76-c76c158c8a24` |
| EV_028 | ひまわりの折り紙 | `045aa1ca-5cb5-4226-9f95-3456f6bbc062` |
| EV_029 | エンディング | `8f1aef2b-b906-4823-9139-7bb16379e376` |

## PlayableDirector トラックバインディング設定

タイムラインアセット（.playable）にトラックを追加しただけでは、シーン上のPlayableDirectorの
トラック出力先（バインディング）が空のままになる。
各トラックに対して、`LevelModules` プレハブ配下の対応コンポーネントをバインドする必要がある。

### バインディングターゲット

`LevelModules.prefab` (`Assets/AnoGame/Prefabs/LevelModules.prefab`) 配下：

| トラック種別 | バインディング先 GameObject | 説明 |
|---|---|---|
| Ano Narrative Track | `DialogueTimelineReceiver` | 会話再生のレシーバー |
| Inventory Item Track | `InventoryHandler` | アイテム取得処理 |
| Signal Track | `共通のSignalReceiver` | シグナル受信（イベント終了等） |

### 一括設定ツール

メニュー **AnoGame → Tools → Set Timeline Track Bindings** で一括バインディング設定可能。

スクリプト: `Assets/AnoGame/Scripts/Editor/Tools/TimelineBindingSetter.cs`

#### 動作仕様:
1. シーン上の `GeneratedEvents` 配下の全 `PlayableDirector` を走査
2. 各トラックのバインディングが `null` の場合のみ、LevelModules配下の該当コンポーネントを自動設定
3. 既にバインディング済みのトラックはスキップ

#### 使い方:
// turbo
1. Chapter2-0 シーンを開く
2. メニュー `AnoGame > Tools > Set Timeline Track Bindings` を実行
3. Consoleログで設定結果を確認
4. シーンを保存（Ctrl+S）

---

## チェックポイント

タイムライン設定後に以下を確認:

- [ ] `conversationID` がMasterDialogueData内に存在するか
- [ ] `targetEpisode` が 2 に設定されているか
- [ ] Signal Emitter の Time が 1.0（または適切な値）か
- [ ] Signal Emitter の Asset が "01.イベント終了" を指しているか
- [ ] アイテム入手系: `itemData` が正しいItemDataアセットを参照しているか
- [ ] シーン側のPlayableDirectorが正しいタイムラインを参照しているか
- [ ] シーン側のSignalReceiverが正しく設定されているか
- [ ] **Ano Narrative Track** のバインディングが `DialogueTimelineReceiver` を指しているか
- [ ] **Inventory Item Track** のバインディングが `InventoryHandler` を指しているか
- [ ] **Signal Track** のバインディングが `共通のSignalReceiver` を指しているか


---
description: Dialogue System → AnoDialogue タイムライン移行ワークフロー
---

# Dialogue System → AnoDialogue タイムライン移行ワークフロー

## 前提条件
- ULoop MCPが接続済みであること
- 対象シーン（Chapter1-1 or Chapter1-2）がUnity Editorで開かれていること

## 移行手順

### Step 1: 対象タイムラインの特定

1. `.playable` ファイル内で `DialogueConversationTrack` のGUID (`976fe2996ecf9db4d8c14ff0c660b91d`) を検索
2. 対象ファイルの一覧と各ファイル内のClip数を記録

### Step 2: タイムラインへの AnoNarrativeTrack 追加

参考: Chapter1-0の「最初の会話」タイムライン (`Assets/AnoGame/Timeline/Old/1-0.最初の会話.playable`)

1. 対象タイムラインを Unity Timeline Editor で開く
2. `AnoGame.AnoDialogue.Timeline > AnoNarrativeTrack` を追加
3. 各シーンの `LevelModules > DialogueTimelineReceiver` をトラックにバインド
4. `AnoNarrativeClip` を追加
5. 既存の `DialogueSystemTrigger` に設定されている会話に近いものを選択

### Step 3: 旧トラックの削除

1. 設定確認後、既存の `DialogueConversationTrack` を削除
2. タイムラインに直接関連する `DialogueSystemTrigger` コンポーネントを削除

### Step 4: スキップ判断

以下のケースはスキップ:
- タイムラインなしで `DialogueSystemTrigger` を直接呼び出しているパターン
- プレハブ内の `DialogueSystemTrigger`（`HidePoint_最初のハイド`, `HidePoint_街角`, `イベントエリア` 等）

### Step 5: 結果記録

各タイムラインについて以下を記録:
- ステータス: 成功 / 失敗 / スキップ
- タイムライン GUID
- 旧Clip情報（conversationTitle）
- 新Clip情報（conversationID）
- 備考

## GUID一覧

### スクリプト GUID

| 名称 | GUID |
|---|---|
| DialogueConversationTrack（旧） | `976fe2996ecf9db4d8c14ff0c660b91d` |
| DialogueConversationClip（旧） | `4e7ff2d2dc30ed04eb9db90e6ae6a722` |
| DialogueSystemTrigger Wrapper | `c593457cd8105e148906690e1707c592` |
| AnoNarrativeTrack（新） | `715762f68eae57048a1135c74ca1eb7d` |
| AnoNarrativeClip（新） | `2ed68ee5f3575f8439a90743bd341d1c` |
| DialogueTimelineReceiver | GUID要確認 |

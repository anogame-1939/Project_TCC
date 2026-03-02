# AnoFlow ライブラリ切り出し計画

## 概要

イベント駆動のアーキテクチャを簡素化し、将来のライブラリ切り出しに備える。

**現行プロジェクトでのスコープ:** 新規コンポーネント追加のみ。既存シーンの移行は行わない。

## 方針

- `EventData.CanExecute()` を追加し、条件判定を EventData に一元化
- `EventReactor` を新規作成（軽量な「イベントクリア後リアクター」）
- `EventTriggerBase` は既存シーンでそのまま使用し、将来の別ワークスペースで段階的に廃止
- Receptor（2状態まで）+ EventReactor の組み合わせを新規イベントの標準パターンとする

## 設計ルール

- **Receptor は最大2状態まで**（未クリア/クリア済み）。3段階以上は別 Receptor に分割する
- **条件判定は `EventData.CanExecute()` に一元化**。Receptor/Trigger 側で独自の条件チェックは行わない
- **タグによる複数入口パターン**は引き続き有効（同一 conditionTag を複数イベントが付与可能）

## ロードマップ

```
現行プロジェクト                          将来ワークスペース
─────────────────                  ──────────────────
[済] EventData.CanExecute() 追加      EventTriggerBase 全廃止
[済] EventReactor 新規作成             Receptor + EventReactor に統一
     新規イベントで運用開始              UPM パッケージとして切り出し
```

## 新アーキテクチャの構成

### コンポーネント役割

| コンポーネント | 役割 | 条件判定 |
|---|---|---|
| **Receptor群** (Contact/Inspect/Item) | プレイヤーアクションの入口 + 直接実行 | `EventData.CanExecute()` |
| **EventReactor** (新) | クリア結果への反応 + 状態復元 | イベントIDの一致のみ |
| ~~EventTriggerBase~~ (既存シーンのみ) | 段階的に廃止予定 | — |

### フロー図

```
[Receptor] → EventData.CanExecute()? → 直接実行(UnityEvent/Timeline)
                                          ↓ クリア記録
                              IEventService.TriggerEventComplete()
                                          ↓ 通知
                              [EventReactor A] → ドアAを開く
                              [EventReactor B] → NPCの台詞変更
```

### 状態復元フロー

```
シーンロード → EventReactor.Start()
                  ↓
             IsEventCleared(watchEventId)?
                  ↓ Yes
             onAlreadyCleared.Invoke()  ← ドアを開いた状態にする等
```

## 変更済みファイル

- `Assets/AnoGame/Scripts/Application/Data/EventData.cs` — `CanExecute()` メソッド追加
- `Assets/AnoGame/Scripts/Application/Event/EventReactor.cs` — 新規作成

## 将来の切り出し時のパッケージ構成案

```
Packages/com.anogame.anoflow/
├── Runtime/
│   ├── Interfaces/
│   │   ├── IEventService.cs
│   │   ├── IInventoryService.cs
│   │   ├── IEventStore.cs
│   │   └── IInteractionRegistry.cs
│   ├── Receptors/
│   │   ├── ContactReceptor.cs
│   │   ├── InspectReceptor.cs
│   │   └── ItemReceptor.cs
│   ├── Reactor/
│   │   └── EventReactor.cs
│   └── Data/
│       └── EventData.cs
└── Editor/
    └── EventGraph系
```

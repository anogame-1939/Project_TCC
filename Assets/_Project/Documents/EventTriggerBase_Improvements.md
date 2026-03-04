# EventTriggerBase 改善提案

## 概要

本ドキュメントは、現プロジェクトで発生した `EventTriggerBase` の設計上の問題と、将来プロジェクトでの改善案をまとめたものである。

---

## 発生した問題

### 症状

`ItemReceptor`（アイテム消費を待つセンサー）と `InstantEventTrigger` が同じ GameObject に同居している場合、**プレイヤーがアイテムを手動消費していないにもかかわらず、前提条件が揃った瞬間にイベントが自動実行** されてしまう。

### 原因

`EventTriggerBase.InitializeConditions()` が**条件監視（Observable）と自動発火を不可分にセットで行う**設計になっている。

```csharp
// EventTriggerBase.InitializeConditions() 内
var condition = new EventClearedCondition(_eventService, evtId);
_conditions.Add(condition);
if (condition is IObservableCondition observableCondition)
    observableCondition.OnConditionChanged += StartEvent;  // 自動発火
```

この仕組みは `InspectReceptor`（調べる系）との組み合わせでは正しく機能するが、`ItemReceptor`（手動消費を要求する系）では意図しない自動発火を引き起こす。

### 条件タイプによる挙動の違い

| 条件タイプ          | クラス                    | IObservable | 自動発火 |
|---------------------|---------------------------|:-----------:|:--------:|
| Req Events          | `EventClearedCondition`   | ✓          | する     |
| Req Items           | `KeyItemCondition`        | ✓          | する     |
| Condition Tags      | `TagCondition`            | ✗          | しない   |
| Condition Component | 実装依存                   | 実装依存    | 実装依存 |

### 今回の対処

`ConsumeEventTrigger` を新設し、`InitializeConditions()` をオーバーライドして `OnConditionChanged` への購読を行わないようにした。`InstantEventTrigger` は互換性のため変更なし。

---

## 将来プロジェクトへの改善提案

### 1. EventTriggerBase に `autoFireOnConditionMet` フラグを追加

```csharp
public abstract class EventTriggerBase : MonoBehaviour
{
    [SerializeField] protected bool autoFireOnConditionMet = true;

    protected virtual void InitializeConditions()
    {
        // ...条件オブジェクト生成...
        if (autoFireOnConditionMet && condition is IObservableCondition obs)
        {
            obs.OnConditionChanged += StartEvent;
        }
    }
}
```

- `InstantEventTrigger` など即時クリア系: `autoFireOnConditionMet = true`（デフォルト）
- `ConsumeEventTrigger` など手動トリガー系: `autoFireOnConditionMet = false`

### 2. 条件監視と発火ロジックを分離する

現状の `EventTriggerBase` は以下の2つの責務を結合している：

1. **イベントライフサイクル管理**（RegisterStartEventHandler, TriggerEventComplete 等）
2. **条件監視＋自動発火**（OnConditionChanged → StartEvent）

これを分離し、条件監視は別のコンポーネント（`EventConditionMonitor` 等）に切り出すことで、トリガータイプごとに柔軟に組み合わせられるようにする。

```
EventTriggerBase            ← ライフサイクルのみ
  + EventConditionMonitor   ← 条件変化の自動発火（opt-in）
  + InstantCompleteBehavior ← 即時クリア（opt-in）
```

### 3. タグ条件（TagCondition）のObservable化を検討

現在 `TagCondition` は `IObservableCondition` を実装しておらず、タグ変化時に通知されない。これは今回の問題では「偶然正しく動作する」要因だったが、将来的に「タグが付与されたら自動で何かしたい」ケースが出たときに困る可能性がある。

Observable化する場合は、上記の `autoFireOnConditionMet` フラグとセットで導入すべきである。

---

## トリガータイプ別の設計指針（現プロジェクト）

| Receptor タイプ        | 推奨 Trigger              | 自動発火 | 備考                     |
|------------------------|---------------------------|:--------:|--------------------------|
| `InspectReceptor`      | `InstantEventTrigger`     | ✓       | 調べる→即クリア           |
| `ContactReceptor`      | `InstantEventTrigger`     | ✓       | 接触→即クリア             |
| `ItemReceptor`         | `ConsumeEventTrigger`     | ✗       | 手動消費を待つ             |

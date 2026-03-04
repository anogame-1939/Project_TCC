# Event Graph: Collapse/Expand & コネクタ位置ずれ問題

## 概要

EventGraphのノード展開/折りたたみ操作時に、コンディションタグのコネクタ（入力ポート）位置がずれる問題を追跡するプロジェクト文書。

---

## 対象ファイル

| ファイル | 役割 |
|---|---|
| `EventNodeView.cs` | ノード表示・ポート位置制御 |
| `EventGraphStyles.uss` | ノードスタイル定義 |
| `EventGraphView.cs` | グラフ全体の管理 |

---

## 現在のポート位置制御アーキテクチャ

### 3つの状態

| State | 条件 | ポート位置 |
|---|---|---|
| A | ノード折りたたみ (expanded=false) | ノード中央Y |
| B | ノード展開 + セクション折りたたみ (bodyH < 1) | セクションヘッダーY |
| C | ノード展開 + セクション展開 | 通常位置 (transform=0) |

### 処理フロー

```
[collapse-button クリック]
  → MouseUpEvent → 1フレーム遅延
  → OnExpandCollapseChanged()
  → SyncExtensionContainerState()  (extensionContainerのスタイル復元)
  → 1フレーム遅延
  → ApplyPortPositions()

[セクションヘッダー クリック]
  → SetBodyCollapsed()
  → SchedulePortPositionUpdate()
  → ResetPortTransforms() + 1フレーム遅延
  → ApplyPortPositions()
```

---

## 修正履歴

### 2026-02-25: GeometryChangedEvent → MouseUpEvent 変更

**問題**: USS変更でノードジオメトリが変化しなくなり、`GeometryChangedEvent`が再展開時に発火しなくなった。`SyncExtensionContainerState()`が呼ばれず、`extensionContainer`が`height:0`のまま残り、再展開後にコンテンツが表示されなかった。

**原因**: `.event-node`の`border-width:0`, `#divider{display:none}`等のUSS変更により、collapse→expandでノードサイズが変化せず、GeometryChangedEventが不発火。

**修正**: `#collapse-button`の`MouseUpEvent`を直接監視し、1フレーム遅延で`expanded`プロパティの変化を検出。

**影響範囲**: `CreateContent()`内のイベント登録部分のみ。

---

### 2026-02-25: コネクタ座標デバッグログ追加

以下のタイミングで `[PortDbg]` プレフィックスのログを出力:

| タイミング | ログプレフィックス | 内容 |
|---|---|---|
| collapse-button クリック直後 | `[PortDbg][Snap] CollapseBtn CLICK` | 全ポートの座標スナップショット |
| collapse/expand 処理完了後 | `[PortDbg][Snap] CollapseToggle AFTER` | 処理後の全ポート座標 |
| セクションヘッダー クリック直後 | `[PortDbg][Snap] SectionToggle BEFORE` | 全ポートの座標スナップショット |
| セクション処理完了後 | `[PortDbg][Snap] SectionToggle AFTER` | 処理後の全ポート座標 |
| ApplyPortPositions State B/C | `[PortDbg] B:` / `[PortDbg] C:` | 各ポートの座標変化詳細 |

---

### 2026-02-25: 5フレーム遅延によるエッジ再描画実験（失敗）

**仮説**: `ApplyPortPositions()` で `transform.position` を変更した直後は `worldBound` がまだ古い値のため、`ForceEdgeRepaint()` が古い座標でエッジを描画してしまう。フレーム遅延で解決するか？

**実験**: `ForceEdgeRepaint()` を `ScheduleDelayedEdgeRepaint(5)` に変更し、5フレーム遅延後にエッジを再描画。

**結果**: **改善されず。** フレーム遅延は根本解決にならない。

**考察**: 問題はタイミングではなく、UIToolkit (GraphView) のエッジ描画メカニズム自体にある可能性。`edge.UpdateEdgeControl()` が `port.transform.position` の変更を正しく反映できていない、またはGraphView内部のポート位置キャッシュが更新されない可能性がある。**UIToolkit自体の未熟さが原因である可能性が高い。**

---

## 既知の問題

- [ ] コンディションタグの開閉時にコネクタ位置がずれる
- [ ] タイトルcollapseの開閉時にコネクタ位置がずれる
- [x] ~~フレーム遅延で改善するか~~ → **改善しない（5フレームでも不可）**
- [ ] UIToolkit/GraphView自体の制約として、`port.transform.position` 変更後のエッジ再計算が正しく機能しない可能性

---

## 技術的な注意点

- `extensionContainer`はGraphView内部が`display:none`に設定するが、EventNodeViewは`position:absolute, height:0, overflow:hidden`で上書きして制御している
- `schedule.Execute()`の1フレーム遅延はレイアウト更新の完了を待つためだが、タイミングが安定しない場合がある
- `_portUpdateGen`による世代管理で、連続操作時のdebounceを行っている
- **フレーム遅延（1〜5フレーム）ではエッジの描画ずれを解決できない**。GraphViewのエッジルーティングがport.transform.positionを考慮しない可能性


---
trigger: always_on
---

1._Project内のスクリプトには、「AnoGame」から始まる名前空間を必ずつけること。

2.エディタ拡張のUI記号ルール
  - ボタンやラベルにポップな絵文字（✏📋📁🔍🎯等）を使用しないこと。
  - プロフェッショナルなUnicode記号のみ許可（例: ▼▶●✓✗▲×+ 等）。
  - プロフェッショナルさを維持し、業務ツールとしてふさわしい外観にすること。

3.エディタツールに関する作業を行う際は、`Assets/AnoGame/Documents/EditorToolsList.md` を参照し、既存ツールの一覧・構成を把握すること。

4.Unity標準コンポーネント（CanvasGroup, PlayableDirector等）のエディタ拡張は、`Assets/AnoUtility/` に配置すること。アセンブリは `AnoUtility.Editor`（名前空間: `AnoGame.Utility.Editor`）を使用する。

5.EventGraphの展開/折りたたみ・コネクタ位置に関する修正を行った際は、`Assets/AnoGame/Documents/EventGraph_CollapseExpand_Issues.md` に修正内容・原因・影響範囲のサマリを追記すること。

6.エディタツール作成・編集時に `MarkDirtyRepaint()` / `EditorUtility.SetDirty()` 等のDirty/Repaint系メソッドを安易に使用しないこと。UIToolkitのレイアウト問題はスタイル変更や構造変更で根本的に解決し、Dirty系はデータ永続化目的のみに限定すること。

7.エディタツール作成時、IMGUI（OnGUI, GenericMenu, EditorGUILayout等）は極力使用せず、UIToolkit（VisualElement, Button, Toggle等）を使用すること。特にOverlayやEditorWindow内ではUIToolkitに統一すること。

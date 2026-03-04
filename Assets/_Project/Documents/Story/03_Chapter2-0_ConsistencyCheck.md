# Chapter 2-0 イベントデータ整合性チェック & シーン反映ワークフロー

## 目的
設計表のイベント定義と、実際のEventDataアセット・シーン上のReceptor/Triggerの整合性を確認し、不整合を修正する。

---

## 現状の発見事項

### 1. タグ名の言語統一（対応済み）
EventDataアセットの `conditionTags`/`resultTags` は**日本語タグ**で管理されている。
設計表も日本語に統一済み。対応表は `01_Chapter2-0_Scenario.md` の末尾を参照。

> **注意**: conditionTagsの一致判定はstring比較のため、全角半角・スペースの差異に注意。

### 2. `requiredItemIds` / `requiredEventIds` が全て空
全29個のEventDataアセットで `requiredItemIds` と `requiredEventIds` は空配列 `[]`。
条件管理は `conditionTags` のみで行われている。

> **判定**: EventTriggerBase のコード上、`conditionTags` は `ConditionTags` プロパティとして公開されていないが、
> InspectReceptor の CheckConditions では `eventData.RequiredItemIds` / `eventData.RequiredEventIds` を参照している。
> **→ conditionTags とは別系統。InspectReceptor 側のチェックは常に true になる（空だから）。**
> **→ 条件チェックは EventTriggerBase 側の conditionTags で行われている。**

### 3. 不正なReceptorオブジェクト（修正済み）
- 21件の不正な `N_Receptor_*` オブジェクトが検出され、無効化済み
- これらは `targetEventId` に数字インデックス（`"1"`, `"5"`, `"16"` 等）が入っていた
- 原因: EventSelectorDrawer の旧バグにより、ドロップダウンのインデックスが保存されていた

---

## チェック作業手順

### Step 1: EventDataアセット全件の内容確認

各EventDataアセット（EV_001〜EV_029）について以下を確認する：

- [ ] `eventId` が正しいか（`EV_XXX_名前` 形式）
- [ ] `eventName` が設計表と一致するか
- [ ] `category` が設計表のパートと一致するか
- [ ] `conditionTags` が設計表の前提条件と対応するか
- [ ] `resultTags` が設計表の獲得ステートと対応するか
- [ ] `requiredItemIds` / `requiredEventIds` の設定状態を確認
- [ ] `isOneTime` の設定が適切か

### Step 2: シーン上のReceptor/Trigger確認

Chapter2-0シーンのHierarchyで以下を確認：

- [ ] 各EventData（EV_001〜029）に対応する**Receptorオブジェクト**が存在するか
- [ ] 各EventData（EV_001〜029）に対応する**Triggerオブジェクト**が存在するか
- [ ] ReceptorのtargetEventIdが正しいEventIdを指しているか（GUID/EventId文字列）
- [ ] Triggerの`eventData`フィールドに正しいEventDataアセットが設定されているか
- [ ] Triggerの`onEventStart`/`onEventFinish`/`onEventDone`にUnityEventが設定されているか
- [ ] InspectReceptor / ContactReceptor の `maxDistance` が適切か（プレイヤーがインタラクトできる距離）
- [ ] 不正な（無効化された）Receptorオブジェクトが残っていないか

### Step 3: conditionTags の連鎖チェック

あるイベントの `resultTags` が、別のイベントの `conditionTags` の前提条件として正しく参照されているか確認する。

#### 期待される連鎖（全件・日本語タグ）
```
[パート1: 導入]
EV_001(大男と初回遭遇) → result:[大男と遭遇済み]
EV_002(街灯で足止め)   → result:[街灯で足止め済み]
EV_003(扉1・閉)        → result:なし
EV_004(扉1・開錠)      → result:[扉１開放済み]
EV_005(磁石入手)        → result:[磁石所持]
EV_006(制御盤の鍵入手) → result:[制御盤の鍵所持]
EV_007(管理人のメモ)   → result:[管理人のメモ所持]
EV_008(金属プレート)   → result:なし
EV_009(霧壁の花)       → result:なし

[パート2: 井戸]
EV_010(井戸デフォルト)     → result:[井戸確認済み]
EV_011(井戸にタコ糸)      → result:[井戸にタコ糸使用済み]
EV_012(井戸に磁石)        → cond:[井戸にタコ糸使用済み] → result:[井戸に磁石使用済み]
EV_013(キーホルダー鍵)   → cond:[井戸に磁石使用済み] → result:[キーホルダーの鍵所持]
EV_014(扉2)               → cond:[キーホルダーの鍵所持] → result:[扉２開放済み]
EV_015(扉3)               → cond:[キーホルダーの鍵所持] → result:[扉３開放済み]
EV_016(タコ糸入手)        → result:[タコ糸所持]
EV_017(同僚のメモ)        → result:[同僚のメモ所持]
EV_018(ロッカー調査)      → result:[ロッカー確認済み]
EV_019(ひまわり男消失)    → cond:[磁石所持, タコ糸所持] → result:[ひまわり男消失済み]
EV_020(北の小屋鍵開け)    → result:[北の小屋開放済み]
EV_021(アルミボール入手)  → result:[アルミボール所持]
EV_022(広場鍵開け)        → result:[広場開放済み]
EV_023(コーラ缶入手)      → result:[コーラ缶所持]

[パート3: ラスト]
EV_024(合成・コーラ)       → cond:[アルミボール所持] → result:[化学反応完了]
EV_025(合成・アルミ)       → cond:[コーラ缶所持] → result:[化学反応完了]
EV_026(ロッカー番号入手)  → cond:[化学反応完了] → result:[ロッカー番号判明]
EV_027(南の小屋ロッカー)  → cond:[ロッカー番号判明] → result:[ロッカー開放済み]
EV_028(折り紙入手)        → cond:[ロッカー開放済み] → result:[折り紙所持]
EV_029(エンディング)      → cond:[折り紙所持] → result:[ゲームクリア]
```

> **注意**: 日本語タグ名が完全一致しているか（全角半角、スペース等）を厳密にチェックする必要がある。

### Step 4: Receptor↔Trigger位置の整合性

ReceptorとTriggerが同じイベントオブジェクト配下にあるか、または適切な位置に配置されているかを確認。

- [ ] InspectReceptorの位置がプレイヤーの到達可能な場所にあるか
- [ ] ContactReceptorのCollider/位置がプレイヤーの動線上にあるか
- [ ] Triggerの位置がゲームオブジェクト（扉、井戸等）と一致するか

### Step 5: Timeline/ダイアログの確認

- [ ] 各イベントのTimelineアセットが存在するか
- [ ] TimelineにSignalTrack/Emitterが設定されているか（イベント終了シグナル）
- [ ] ダイアログデータのConversationIDが正しいか

---

## 具体的な確認コマンド

### 全EventDataのconditionTags/resultTagsを一覧出力するエディタスクリプト
```csharp
// メニュー: AnoGame/Tools/全EventDataのタグを一覧出力
[MenuItem("AnoGame/Tools/全EventDataのタグを一覧出力")]
public static void ListAllEventTags()
{
    var guids = AssetDatabase.FindAssets("t:EventData");
    foreach (var guid in guids)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var data = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (data != null)
        {
            var conditions = data.ConditionTags != null ? string.Join(", ", data.ConditionTags) : "none";
            var results = data.ResultTags != null ? string.Join(", ", data.ResultTags) : "none";
            Debug.Log($"{data.EventId} | cond=[{conditions}] | result=[{results}]");
        }
    }
}
```

### Receptor/Triggerの一覧出力
```csharp
// メニュー: AnoGame/Tools/全Receptor&Triggerを一覧出力
[MenuItem("AnoGame/Tools/全Receptor&Triggerを一覧出力")]
public static void ListAllReceptorsAndTriggers()
{
    var receptors = FindObjectsOfType<InspectReceptor>(true);
    var contactReceptors = FindObjectsOfType<ContactReceptor>(true);
    var triggers = FindObjectsOfType<EventTriggerBase>(true);
    
    Debug.Log($"=== InspectReceptor: {receptors.Length}件 ===");
    foreach (var r in receptors)
    {
        var so = new SerializedObject(r);
        var id = so.FindProperty("targetEventId")?.stringValue ?? "null";
        Debug.Log($"  {r.gameObject.name} | active={r.gameObject.activeSelf} | targetEventId={id} | pos={r.transform.position}");
    }
    
    Debug.Log($"=== ContactReceptor: {contactReceptors.Length}件 ===");
    // 同様に出力...
    
    Debug.Log($"=== EventTriggerBase: {triggers.Length}件 ===");
    foreach (var t in triggers)
    {
        var eventData = t.EventData;
        Debug.Log($"  {t.gameObject.name} | active={t.gameObject.activeSelf} | eventId={eventData?.EventId ?? "null"} | pos={t.transform.position}");
    }
}
```

---

## 次のアクション

1. **Step 1 を実行**: 全EventDataアセットのタグ一覧を出力し、設計表との対応を確認
2. **Step 2 を実行**: シーン上のReceptor/Triggerを一覧出力し、抜け漏れを確認  
3. **不整合が見つかった場合**: BatchEventCreatorまたは手動で修正
4. **Step 3〜5を実行**: 連鎖チェック、位置の整合性、Timeline確認

---

## 参照ファイル

| ファイル | パス |
|---|---|
| EventDataアセット | `Assets/AnoGame/Data/Events/Story2/EV_*.asset` |
| events_story2.json | `Assets/AnoGame/Data/ItemsResources/events_story2.json` |
| InspectReceptor.cs | `Assets/AnoGame/Scripts/Application/Event/InspectReceptor.cs` |
| ContactReceptor.cs | `Assets/AnoGame/Scripts/Application/Event/ContactReceptor.cs` |
| EventTriggerBase.cs | `Assets/AnoGame/Scripts/Application/Event/EventTriggerBase.cs` |
| EventService.cs | `Assets/AnoGame/Scripts/Infrastructure/EventService.cs` |
| EventSelectorDrawer.cs | `Assets/AnoGame/Scripts/Editor/PropertyDrawers/EventSelectorDrawer.cs` |
| FixInvalidReceptors.cs | `Assets/AnoGame/Scripts/Editor/Tools/FixInvalidReceptors.cs` |
| ストーリー概要 | `Assets/AnoGame/Documents/Story/00_StoryOverview.md` |
| Chapter2-0 シナリオ | `Assets/AnoGame/Documents/Story/01_Chapter2-0_Scenario.md` |

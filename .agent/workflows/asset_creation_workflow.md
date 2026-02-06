---
description: How to batch create Items and Event objects from spreadsheet data
---

This workflow describes how to import Item and Event data into the project using batch generation tools.

# 1. Prepare Data
Ensure you have the data source (e.g. Spreadsheet).
Create/Update the JSON files in `Assets/AnoGame/Data/ItemsResources/`.

- **Items**: `items_batch.json`
- **Events**: `events_batch.json`

# 2. Generate Items
1.  Verify `items_batch.json` contains the correct list of items.
2.  Run the batch creator tool in Unity:
    **Tools > Run Batch Item Creation**
3.  This will:
    -   Create `ItemData` assets in `Assets/AnoGame/Data/ItemsResources/`.
    -   Register them to the `ItemDatabase` at `Assets/AnoGame/Data/ItemDatabase.asset`.

# 3. Generate Events
1.  **Template Check**: Ensure the following prefabs exist in `Assets/AnoGame/Prefabs/EventZone/`:
    -   `Tpl_ContactZone.prefab` (Receptor: Triggered by contact)
    -   `Tpl_InspectZone.prefab` (Receptor: Triggered by inspect/interact)
    -   `Tpl_ItemZone.prefab` (Receptor: Triggered by item use)
    -   `Tpl_Trigger.prefab` (Logic: InstantEventTrigger + Timeline)
2.  Verify `events_batch.json` contains the correct list of events.
3.  Run the batch creator tool in Unity:
    **Tools > Run Batch Event Creation**
4.  **Generation Logic**:
    -   For each event defined in JSON, the tool creates a **Pair** of objects in the scene:
        1.  **Receptor**: The Zone object (based on `prefabType` from JSON) that detects player interaction.
        2.  **Trigger**: The `Tpl_Trigger` object that handles the actual event logic and Timeline.
    -   This ensures a 1-to-1 relationship between the Entry Point (Receptor) and Execution Logic (Trigger).

# Tools Locations
-   **Item Batch Script**: `Assets/AnoGame/Scripts/Application/Data/Editor/BatchItemCreator.cs`
-   **Event Batch Script**: `Assets/AnoGame/Scripts/Application/Event/Editor/BatchEventCreator.cs`

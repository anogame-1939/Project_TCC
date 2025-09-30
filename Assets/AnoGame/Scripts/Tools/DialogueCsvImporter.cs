#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

public static class DialogueCsvImporter
{
    [MenuItem("Tools/Dialogue System/CSVから会話一括追加")]
    private static void ImportFromCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel("Import Dialogue CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(csvPath)) return;

        DialogueDatabase db = Selection.activeObject as DialogueDatabase;
        if (db == null)
        {
            string targetFolder = "Assets/Dialogue";
            if (!AssetDatabase.IsValidFolder(targetFolder)) AssetDatabase.CreateFolder("Assets", "Dialogue");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/Generated.asset");

            db = ScriptableObject.CreateInstance<DialogueDatabase>();
            db.name = Path.GetFileNameWithoutExtension(assetPath);

            // ★ 各リストは必ずnew
            db.actors        = new List<Actor>();
            db.conversations = new List<Conversation>();
            db.items         = new List<Item>();
            db.locations     = new List<Location>();
            db.variables     = new List<Variable>();

            AssetDatabase.CreateAsset(db, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueCSV] 新規DBを作成: {assetPath}");
        }

        // 念のため null ガード
        db.actors        = db.actors        ?? new List<Actor>();
        db.conversations = db.conversations ?? new List<Conversation>();
        db.items         = db.items         ?? new List<Item>();
        db.locations     = db.locations     ?? new List<Location>();
        db.variables     = db.variables     ?? new List<Variable>();

        // 既定アクター
        var aMisaki = GetOrCreateActor(db, "美咲",   true);
        var aGrand  = GetOrCreateActor(db, "祖母",   false);
        var aAunt   = GetOrCreateActor(db, "おばさん", false);
        var aNone   = GetOrCreateActor(db, "None",   false);

        // CSV読込
        var lines = File.ReadAllLines(csvPath, new UTF8Encoding(true));
        if (lines.Length <= 1)
        {
            EditorUtility.DisplayDialog("CSVエラー", "ヘッダー行を含むCSVを指定してください。", "OK");
            return;
        }

        var header = SplitCsvLine(lines[0]);
        int idxCode  = Array.IndexOf(header, "Code");
        int idxTitle = Array.IndexOf(header, "Title");
        int idxLines = Array.IndexOf(header, "Lines");
        if (idxCode < 0 || idxTitle < 0 || idxLines < 0)
        {
            EditorUtility.DisplayDialog("CSVエラー", "ヘッダーに Code, Title, Lines が必要です。", "OK");
            return;
        }

        // ★ 変更追跡（必須）
        Undo.RecordObject(db, "Import Dialogue CSV");

        int added = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = SplitCsvLine(lines[i]);

            string code  = SafeGet(cols, idxCode);
            string title = SafeGet(cols, idxTitle);
            string fullTitle = string.IsNullOrEmpty(code) ? title : $"{code}_{title}";

            var parsed = ParseLines(SafeGet(cols, idxLines));
            AddConversation(db, fullTitle, parsed, aMisaki.id, aGrand.id, aAunt.id, aNone.id);
            added++;
        }

        // ★ 保存＆再読込
        EditorUtility.SetDirty(db);
        string dbPath = AssetDatabase.GetAssetPath(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(dbPath);  // 明示的に再インポート
        AssetDatabase.Refresh();

        Debug.Log($"[DialogueCSV] 追加完了: {added} 件 -> DB: {db.name} (path: {dbPath})");
        Debug.Log($"[DialogueCSV] 追加後件数: actors={db.actors?.Count ?? -1}, conversations={db.conversations?.Count ?? -1}");
        EditorUtility.DisplayDialog("インポート完了", $"会話 {added} 件を追加しました。\nDB: {db.name}", "OK");
    }

    [MenuItem("Tools/Dialogue System/選択DBの件数を表示")]
    private static void ShowCounts()
    {
        var db = Selection.activeObject as DialogueDatabase;
        if (db == null)
        {
            EditorUtility.DisplayDialog("Info", "DialogueDatabase アセットを選択してください。", "OK");
            return;
        }
        string path = AssetDatabase.GetAssetPath(db);
        Debug.Log($"[DialogueCSV] DB '{db.name}' at {path}: actors={db.actors?.Count ?? -1}, conversations={db.conversations?.Count ?? -1}");
        EditorUtility.DisplayDialog("件数", $"actors={db.actors?.Count ?? -1}\nconversations={db.conversations?.Count ?? -1}", "OK");
    }

    // ====== ここから下はユーティリティ ======

    private class LineDef { public string speaker; public string text; }

    private static List<LineDef> ParseLines(string cell)
    {
        var result = new List<LineDef>();
        if (string.IsNullOrWhiteSpace(cell)) return result;

        var chunks = cell.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in chunks)
        {
            var part = raw.Trim();
            if (string.IsNullOrEmpty(part)) continue;

            int idx = part.IndexOf('：');
            if (idx < 0) idx = part.IndexOf(':');

            string spk = "美咲";
            string txt = part;
            if (idx >= 0)
            {
                spk = part.Substring(0, idx).Trim();
                txt = part.Substring(idx + 1).Trim();
            }
            if (txt.StartsWith("「") && txt.EndsWith("」") && txt.Length >= 2)
                txt = txt.Substring(1, txt.Length - 2);

            result.Add(new LineDef { speaker = spk, text = txt });
        }
        return result;
    }

    private static void AddConversation(
        DialogueDatabase db,
        string title,
        List<LineDef> entries,
        int misakiId, int grandId, int auntId, int noneId)
    {
        if (db.conversations == null) db.conversations = new List<Conversation>();

        int convId = NextConversationId(db);

        var conv = new Conversation
        {
            id = convId,
            dialogueEntries = new List<DialogueEntry>(),
            fields = new List<Field>() // ★ 先に fields
        };
        conv.Title = title;           // ★ fields 後

        // ルート
        var root = new DialogueEntry
        {
            id = 0,
            conversationID = convId,
            isRoot = true,
            outgoingLinks = new List<Link>(),
            fields = new List<Field>() // ★ 先に fields
        };
        root.Title = "START";         // ★ fields 後
        root.DialogueText = string.Empty;
        conv.dialogueEntries.Add(root);

        var speakers = new HashSet<string>(entries.Select(e => e.speaker));
        bool isMonologue = (speakers.Count <= 1 && entries.Count > 0);

        int entryId = 1;
        DialogueEntry prev = root;

        foreach (var e in entries)
        {
            int actorId = SpeakerToActorId(e.speaker, misakiId, grandId, auntId, noneId);
            int conversantId = isMonologue
                ? noneId
                : ((prev != null && prev.ActorID != 0 && prev.ActorID != actorId) ? prev.ActorID : noneId);

            var entry = new DialogueEntry
            {
                id = entryId,
                conversationID = convId,
                outgoingLinks = new List<Link>(),
                fields = new List<Field>() // ★ 先に fields
            };

            // ★ setterはfields初期化後
            entry.ActorID = actorId;
            entry.ConversantID = conversantId;
            entry.Title = $"Line {entryId}";
            entry.DialogueText = e.text;

            conv.dialogueEntries.Add(entry);

            // ★ Linkは4引数（同一会話内）
            prev.outgoingLinks.Add(new Link(convId, prev.id, convId, entry.id));
            prev = entry;
            entryId++;
        }

        db.conversations.Add(conv);
    }

    private static int SpeakerToActorId(string speaker, int misakiId, int grandId, int auntId, int noneId)
    {
        switch (speaker)
        {
            case "美咲": return misakiId;
            case "祖母": return grandId;
            case "おばさん": return auntId;
            case "None":
            case "なし":
            case "ナレーション": return noneId;
            default: return misakiId; // 安全側
        }
    }

    private static Actor GetOrCreateActor(DialogueDatabase db, string name, bool isPlayer)
    {
        db.actors = db.actors ?? new List<Actor>();
        var actor = db.actors.FirstOrDefault(a => a != null && a.Name == name);
        if (actor != null) return actor;

        int nextId = 1;
        foreach (var a in db.actors) if (a != null && a.id >= nextId) nextId = a.id + 1;

        actor = new Actor
        {
            id = nextId,
            fields = new List<Field>() // ★ 先に fields
        };
        actor.Name = name;            // ★ fields 後
        Field.SetValue(actor.fields, "Is Player", isPlayer ? "True" : "False", FieldType.Boolean);

        db.actors.Add(actor);
        return actor;
    }

    private static int NextConversationId(DialogueDatabase db)
    {
        int max = 0;
        foreach (var c in db.conversations) if (c != null && c.id > max) max = c.id;
        return max + 1;
    }

    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString()); sb.Length = 0;
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    private static string SafeGet(string[] arr, int index)
        => (index >= 0 && index < arr.Length) ? arr[index] : string.Empty;
}
#endif

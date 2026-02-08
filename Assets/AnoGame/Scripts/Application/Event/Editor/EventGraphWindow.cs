#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnoGame.Application.Event.Editor
{
    public class EventGraphWindow : EditorWindow
    {
        private const string JSON_PATH = "Assets/AnoGame/Data/ItemsResources/events_batch.json";
        
        [MenuItem("Window/AnoGame/Event Graph")]
        public static void Open()
        {
            GetWindow<EventGraphWindow>("Event Graph");
        }

        private List<Node> nodes = new List<Node>();
        private Vector2 scrollPos;
        private Vector2 panOffset = Vector2.zero;
        private float zoom = 1.0f;

        private void OnEnable()
        {
            LoadData();
        }

        private void OnGUI()
        {
            DrawToolbar();

            // Background
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);

            // Zoom/Pan
            UnityEngine.Event e = UnityEngine.Event.current;
            if (e.type == EventType.MouseDrag && e.button == 2) // Middle click pan
            {
                panOffset += e.delta;
                Repaint();
            }
            
            // Draw Connections (Behind windows)
            DrawConnections();

            // Draw Nodes
            BeginWindows();
            for (int i = 0; i < nodes.Count; i++)
            {
                // Convert World to Screen
                Rect screenRect = new Rect(nodes[i].rect.x + panOffset.x, nodes[i].rect.y + panOffset.y, nodes[i].rect.width, nodes[i].rect.height);
                
                // Draw Window
                Rect newScreenRect = GUI.Window(i, screenRect, DrawNodeWindow, nodes[i].title);
                
                // Convert Screen back to World
                nodes[i].rect = new Rect(newScreenRect.x - panOffset.x, newScreenRect.y - panOffset.y, newScreenRect.width, newScreenRect.height);
            }
            EndWindows();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
            {
                LoadData();
            }
            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton))
            {
                DoAutoLayout();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            Vector2 offset = new Vector2(panOffset.x % gridSpacing, panOffset.y % gridSpacing);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + (Vector3)offset, new Vector3(gridSpacing * i, position.height, 0f) + (Vector3)offset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + (Vector3)offset, new Vector3(position.width, gridSpacing * j, 0f) + (Vector3)offset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawConnections()
        {
            // Map Results to Nodes
            Dictionary<string, Node> resultToNodeMap = new Dictionary<string, Node>();
            foreach (var node in nodes)
            {
                if (node.results != null)
                {
                    foreach (var res in node.results)
                    {
                        if (!resultToNodeMap.ContainsKey(res))
                        {
                            resultToNodeMap[res] = node;
                        }
                    }
                }
            }

            foreach (var node in nodes)
            {
                // Draw lines from Condition Nodes TO this Node
                if (node.conditions != null)
                {
                    foreach (var condId in node.conditions)
                    {
                        // 1. Check direct Event ID match
                        var parentNode = nodes.FirstOrDefault(n => n.id == condId);
                        
                        // 2. Check Result match (Indirect dependency via Item/State)
                        if (parentNode == null && resultToNodeMap.ContainsKey(condId))
                        {
                            parentNode = resultToNodeMap[condId];
                        }

                        if (parentNode != null)
                        {
                            DrawBezierConnection(parentNode.rect, node.rect, Color.white, 2f);
                        }
                    }
                }
            }
        }

        private void DrawBezierConnection(Rect start, Rect end, Color color, float width)
        {
            Vector3 startPos = new Vector3(start.x + start.width + panOffset.x, start.y + start.height / 2 + panOffset.y, 0);
            Vector3 endPos = new Vector3(end.x + panOffset.x, end.y + end.height / 2 + panOffset.y, 0);
            Vector3 startTan = startPos + Vector3.right * 50;
            Vector3 endTan = endPos + Vector3.left * 50;
            
            Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, width);
        }

        private void DrawNodeWindow(int id)
        {
            var node = nodes[id];
            
            GUILayout.BeginVertical();
            
            // Content
            if (!string.IsNullOrEmpty(node.category))
                GUILayout.Label($"[{node.category}]", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(node.description))
                GUILayout.Label(node.description, EditorStyles.wordWrappedMiniLabel);
            
            GUILayout.Space(5);

            // Display conditions (Items)
            if (node.conditions != null && node.conditions.Count > 0)
            {
                GUILayout.Label("Conditions:", EditorStyles.boldLabel);
                foreach(var c in node.conditions)
                {
                    // Check if it's an event
                    if (nodes.Any(n => n.id == c)) continue; // Already drawn as line
                    GUILayout.Label($"- Item: {c}", EditorStyles.miniLabel);
                }
            }

            // Display Results
            if (node.results != null && node.results.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Results:", EditorStyles.boldLabel);
                foreach(var r in node.results)
                {
                    GUILayout.Label($"- {r}", EditorStyles.miniLabel);
                }
            }
            
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void LoadData()
        {
            nodes.Clear();
            if (!File.Exists(JSON_PATH)) return;

            try
            {
                string jsonContent = File.ReadAllText(JSON_PATH);
                string wrappedJson = "{\"events\":" + jsonContent + "}";
                EventList dataList = JsonUtility.FromJson<EventList>(wrappedJson);

                if (dataList != null && dataList.events != null)
                {
                    foreach (var evt in dataList.events)
                    {
                        Node node = new Node();
                        node.id = evt.eventId;
                        node.title = $"{evt.eventId}\n{evt.name}";
                        node.category = evt.category;
                        node.description = evt.description;
                        node.conditions = evt.conditions ?? new List<string>();
                        node.results = evt.results ?? new List<string>();
                        
                        // Default position
                        node.rect = new Rect(100, 100, 220, 160);
                        nodes.Add(node);
                    }
                }
                DoAutoLayout();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to load Event Graph: " + ex.Message);
            }
        }

        private void DoAutoLayout()
        {
            if (nodes.Count == 0) return;

            // Map Results to Nodes for dependency lookup
            Dictionary<string, Node> resultToNodeMap = new Dictionary<string, Node>();
            foreach (var node in nodes)
            {
                if (node.results != null)
                {
                    foreach (var res in node.results)
                    {
                        if (!resultToNodeMap.ContainsKey(res)) resultToNodeMap[res] = node;
                    }
                }
            }

            // Map: ID -> Node
            Dictionary<string, Node> nodeMap = nodes.ToDictionary(n => n.id);
            Dictionary<string, int> depths = new Dictionary<string, int>();

            // Init depths
            foreach (var n in nodes) depths[n.id] = 0;

            // Relaxation (Calculate max depth based on dependencies)
            for (int i = 0; i < nodes.Count + 2; i++)
            {
                bool changed = false;
                foreach (var node in nodes)
                {
                    int currentMax = -1;
                    if (node.conditions != null)
                    {
                        foreach (var c in node.conditions)
                        {
                            // 1. Direct Event Dependency
                            if (nodeMap.ContainsKey(c))
                            {
                                if (depths[c] > currentMax) currentMax = depths[c];
                            }
                            // 2. Result Dependency
                            else if (resultToNodeMap.ContainsKey(c))
                            {
                                var parentNode = resultToNodeMap[c];
                                if (depths[parentNode.id] > currentMax) currentMax = depths[parentNode.id];
                            }
                        }
                    }
                    
                    int newDepth = currentMax + 1;
                    if (newDepth > depths[node.id])
                    {
                        depths[node.id] = newDepth;
                        changed = true;
                    }
                }
                if (!changed) break;
            }

            // Group by Depth
            var grouped = nodes.GroupBy(n => depths[n.id]).OrderBy(g => g.Key);
            
            float xSpacing = 300f;
            float ySpacing = 200f;
            float startX = 50f;
            float startY = 50f;

            foreach (var group in grouped)
            {
                int depth = group.Key;
                int row = 0;
                foreach (var node in group)
                {
                    node.rect.x = startX + depth * xSpacing;
                    node.rect.y = startY + row * ySpacing;
                    row++;
                }
            }
            Repaint();
        }

        [System.Serializable]
        private class Node
        {
            public string id;
            public string title;
            public string category;
            public string description;
            public List<string> conditions;
            public List<string> results;
            public Rect rect;
        }

        [System.Serializable]
        private class EventList
        {
            public List<EventJsonItem> events;
        }

        [System.Serializable]
        private class EventJsonItem
        {
            public string eventId;
            public string name;
            public string category;
            public string description;
            public List<string> conditions;
            public List<string> results;
        }
    }
}
#endif

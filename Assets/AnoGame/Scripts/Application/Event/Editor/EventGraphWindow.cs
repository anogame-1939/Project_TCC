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
        private Vector2 panOffset = Vector2.zero;
        private float zoom = 1.0f;
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 2.0f;
        
        private Node selectedNode;
        private bool isDraggingNode;

        private GUIStyle nodeStyle;
        private GUIStyle nodeHeaderStyle;

        private void OnEnable()
        {
            LoadData();
        }

        private void SetupStyles()
        {
            if (nodeStyle != null) return;

            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.padding = new RectOffset(10, 10, 10, 10);

            nodeHeaderStyle = new GUIStyle();
            nodeHeaderStyle.alignment = TextAnchor.MiddleCenter;
            nodeHeaderStyle.fontStyle = FontStyle.Bold;
            nodeHeaderStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            SetupStyles();

            DrawToolbar();
            
            // Handle Input (Pan/Zoom/Drag)
            HandleEvents(UnityEngine.Event.current);

            // Draw Background Grid (Screen Space, but moving with Pan/Zoom)
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);

            // Begin Zoom Area
            // We use a matrix to handle zoom and pan for the graph content
            Rect graphRect = new Rect(0, 20, position.width, position.height - 20);
            
            // Define Rects
            float redBorder = 5f;
            float cyanBorder = 2f;
            
            // Window Rect (Full area below toolbar)
            Rect windowRect = new Rect(0, 20, position.width, position.height - 20);
            
            // View Area (Inside Window Red Border)
            Rect viewRect = new Rect(
                windowRect.x + redBorder, 
                windowRect.y + redBorder, 
                windowRect.width - redBorder * 2, 
                windowRect.height - redBorder * 2
            );

            // Clip Area (Inside Cyan Border) - This is where content lives
            Rect clipRect = new Rect(
                viewRect.x + cyanBorder, 
                viewRect.y + cyanBorder, 
                viewRect.width - cyanBorder * 2, 
                viewRect.height - cyanBorder * 2
            );

            // 1. Draw Content (Clipped)
            GUI.BeginGroup(clipRect);
            
            Matrix4x4 prevMatrix = GUI.matrix;
            
            // Matrix construction:
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoom, zoom, 1.0f)) * Matrix4x4.TRS(new Vector3(panOffset.x, panOffset.y, 0), Quaternion.identity, Vector3.one);

            // Calculate Content Bounds (Union of all node rects)
            Rect contentRect = new Rect(0, 0, 0, 0);
            if (nodes.Count > 0)
            {
                contentRect = nodes[0].rect;
                for (int i = 1; i < nodes.Count; i++)
                {
                    contentRect = RectMinMax(contentRect, nodes[i].rect);
                }
                // Add some padding
                contentRect.xMin -= 50;
                contentRect.yMin -= 50;
                contentRect.xMax += 50;
                contentRect.yMax += 50;
            }

            // Draw Yellow Border (2px) around Content Area
            Color yellow = Color.yellow;
            float border = 2f;
            // Top
            GUI.color = yellow;
            GUI.DrawTexture(new Rect(contentRect.x, contentRect.y, contentRect.width, border), Texture2D.whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(contentRect.x, contentRect.yMax - border, contentRect.width, border), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(contentRect.x, contentRect.y, border, contentRect.height), Texture2D.whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(contentRect.xMax - border, contentRect.y, border, contentRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawConnections();
            DrawNodes();

            GUI.matrix = prevMatrix;
            GUI.EndGroup();

            // 2. Draw Borders (On Top)
            
            // Red Border (Window Frame) - Outer
            Color red = Color.red;
            EditorGUI.DrawRect(new Rect(windowRect.x, windowRect.y, windowRect.width, redBorder), red); // Top
            EditorGUI.DrawRect(new Rect(windowRect.x, windowRect.yMax - redBorder, windowRect.width, redBorder), red); // Bottom
            EditorGUI.DrawRect(new Rect(windowRect.x, windowRect.y, redBorder, windowRect.height), red); // Left
            EditorGUI.DrawRect(new Rect(windowRect.xMax - redBorder, windowRect.y, redBorder, windowRect.height), red); // Right

            // Cyan Border (View Frame) - Inner (Inside Red)
            Color cyan = Color.cyan;
            EditorGUI.DrawRect(new Rect(viewRect.x, viewRect.y, viewRect.width, cyanBorder), cyan); // Top
            EditorGUI.DrawRect(new Rect(viewRect.x, viewRect.yMax - cyanBorder, viewRect.width, cyanBorder), cyan); // Bottom
            EditorGUI.DrawRect(new Rect(viewRect.x, viewRect.y, cyanBorder, viewRect.height), cyan); // Left
            EditorGUI.DrawRect(new Rect(viewRect.xMax - cyanBorder, viewRect.y, cyanBorder, viewRect.height), cyan); // Right

            // 3. Draw Debug Info Overlay
            string debugInfo = $"Window (Red): {windowRect.width:F0}x{windowRect.height:F0}\n" +
                               $"View (Cyan): {viewRect.width:F0}x{viewRect.height:F0}\n" +
                               $"Clip Area: {clipRect.width:F0}x{clipRect.height:F0}\n" +
                               $"Zoom: {zoom:F2}\n" +
                               $"Content (Yellow): {contentRect.width:F0}x{contentRect.height:F0}";
            
            GUIStyle debugStyle = new GUIStyle(GUI.skin.box);
            debugStyle.alignment = TextAnchor.UpperLeft;
            debugStyle.fontSize = 11;
            debugStyle.normal.textColor = Color.white;
            
            float overlayWidth = 220f;
            float overlayHeight = 85f;
            Rect overlayRect = new Rect(windowRect.xMax - overlayWidth - 10, windowRect.y + 10, overlayWidth, overlayHeight);
            
            GUI.Box(overlayRect, debugInfo, debugStyle);
            
            Repaint();
        }

        private Rect RectMinMax(Rect r1, Rect r2)
        {
            float xMin = Mathf.Min(r1.xMin, r2.xMin);
            float yMin = Mathf.Min(r1.yMin, r2.yMin);
            float xMax = Mathf.Max(r1.xMax, r2.xMax);
            float yMax = Mathf.Max(r1.yMax, r2.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadData();
            }
            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                DoAutoLayout();
            }
            if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                panOffset = Vector2.zero;
                zoom = 1.0f;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Zoom: {zoom:F2}", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        private void HandleEvents(UnityEngine.Event e)
        {
            // Zoom
            if (e.type == EventType.ScrollWheel)
            {
                Vector2 mousePos = e.mousePosition;
                // Ignore if mouse is in toolbar
                if (mousePos.y > 20) 
                {
                    float zoomDelta = -e.delta.y / 150.0f;
                    float oldZoom = zoom;
                    float newZoom = Mathf.Clamp(zoom + zoomDelta, MIN_ZOOM, MAX_ZOOM);
                    
                    // Zoom towards mouse:
                    // WorldMouse = (ScreenMouse - GroupOffset) / OldZoom - OldPan
                    // NewPan = (ScreenMouse - GroupOffset) / NewZoom - WorldMouse
                    
                    Vector2 outputRectOffset = new Vector2(0, 20); // The graph area offset
                    Vector2 screenMouseInGraph = mousePos - outputRectOffset;
                    
                    Vector2 worldMouse = screenMouseInGraph / oldZoom - panOffset;
                    panOffset = screenMouseInGraph / newZoom - worldMouse;
                    
                    zoom = newZoom;
                    e.Use();
                }
            }

            // Pan
            if (e.type == EventType.MouseDrag && (e.button == 2 || (e.button == 0 && e.alt)))
            {
                panOffset += e.delta / zoom;
                e.Use();
            }
            
            // Allow panning with Left Click on empty space
            if (e.type == EventType.MouseDrag && e.button == 0 && selectedNode == null)
            {
                panOffset += e.delta / zoom;
                e.Use();
            }

            // Node Dragging
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Calculate Mouse World Pos
                Vector2 outputRectOffset = new Vector2(0, 20);
                Vector2 mousePos = e.mousePosition;
                
                if (mousePos.y > 20) // Check inside graph area
                {
                    Vector2 worldMouse = (mousePos - outputRectOffset) / zoom - panOffset;
                    
                    // Select Node (Iterate reverse to pick top one)
                    selectedNode = null;
                    for (int i = nodes.Count - 1; i >= 0; i--)
                    {
                        if (nodes[i].rect.Contains(worldMouse))
                        {
                            selectedNode = nodes[i];
                            isDraggingNode = true;
                            e.Use();
                            break;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingNode && selectedNode != null)
            {
                selectedNode.rect.position += e.delta / zoom;
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                isDraggingNode = false;
            }
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            // Grid logic:
            // We want the grid to move with Pan and scale with Zoom.
            // We can just draw it using Handles in screen space.
            
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing / zoom);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing / zoom);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            // Calculate offset in screen space
            Vector3 offset = new Vector3((panOffset.x * zoom) % (gridSpacing * zoom), (panOffset.y * zoom) % (gridSpacing * zoom), 0);
            
            float scaledSpacing = gridSpacing * zoom;

            for (int i = 0; i <= position.width / scaledSpacing + 1; i++)
            {
                Handles.DrawLine(
                    new Vector3(scaledSpacing * i, 0, 0) + offset + new Vector3(0, 20, 0),
                    new Vector3(scaledSpacing * i, position.height, 0) + offset
                );
            }

            for (int j = 0; j <= position.height / scaledSpacing + 1; j++)
            {
                 Handles.DrawLine(
                    new Vector3(0, scaledSpacing * j, 0) + offset + new Vector3(0, 20, 0),
                    new Vector3(position.width, scaledSpacing * j, 0) + offset
                );
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                Rect nodeRect = node.rect;
                
                // Draw Box
                GUI.Box(nodeRect, "", nodeStyle);
                
                // Header
                Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 25);
                GUI.Label(headerRect, node.title, nodeHeaderStyle);
                
                // Content Area
                GUILayout.BeginArea(new Rect(nodeRect.x + 10, nodeRect.y + 25, nodeRect.width - 20, nodeRect.height - 35));
                
                if (!string.IsNullOrEmpty(node.category))
                    GUILayout.Label($"[{node.category}]", EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(node.description))
                    GUILayout.Label(node.description, EditorStyles.wordWrappedMiniLabel);

                if (node.conditions != null && node.conditions.Count > 0)
                {
                    GUILayout.Space(2);
                    GUILayout.Label("Cond:", EditorStyles.boldLabel);
                    foreach (var c in node.conditions)
                    {
                        if (!nodes.Any(n => n.id == c)) // Only show if not linked
                             GUILayout.Label($"- {c}", EditorStyles.miniLabel);
                    }
                }
                
                if (node.results != null && node.results.Count > 0)
                {
                    GUILayout.Space(2);
                    GUILayout.Label("Res:", EditorStyles.boldLabel);
                    foreach (var r in node.results)
                        GUILayout.Label($"- {r}", EditorStyles.miniLabel);
                }

                GUILayout.EndArea();
            }
        }

        private void DrawConnections()
        {
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

            foreach (var node in nodes)
            {
                if (node.conditions != null)
                {
                    foreach (var condId in node.conditions)
                    {
                        Node parentNode = nodes.FirstOrDefault(n => n.id == condId);
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
            Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
            Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
            Vector3 startTan = startPos + Vector3.right * 50;
            Vector3 endTan = endPos + Vector3.left * 50;
            
            Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, width);
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
                        
                        node.rect = new Rect(100, 100, 200, 150);
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

            Dictionary<string, Node> nodeMap = nodes.ToDictionary(n => n.id);
            Dictionary<string, int> depths = new Dictionary<string, int>();

            foreach (var n in nodes) depths[n.id] = 0;

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
                            if (nodeMap.ContainsKey(c))
                            {
                                if (depths[c] > currentMax) currentMax = depths[c];
                            }
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

            var grouped = nodes.GroupBy(n => depths[n.id]).OrderBy(g => g.Key);
            
            float xSpacing = 250f;
            float ySpacing = 180f;
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

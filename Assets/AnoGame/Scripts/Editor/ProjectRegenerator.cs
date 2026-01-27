using System.IO;
using UnityEditor;
using Unity.CodeEditor;
using UnityEngine;

namespace AnoGame.EditorExtensions
{
    public static class ProjectRegenerator
    {
        [MenuItem("Tools/Force Regenerate Project Files %#r")] // Ctrl+Shift+R
        public static void Regenerate()
        {
            Debug.Log("[ProjectRegenerator] Synced with current editor.");
            string originalEditor = EditorPrefs.GetString("kScriptsDefaultApp");
            // Debug.Log($"[ProjectRegenerator] Current Editor: {originalEditor}");

            // If current editor looks like a valid IDE (VS, Code, Rider), just sync
            if (IsKnownIDE(originalEditor))
            {
                CodeEditor.CurrentEditor.SyncAll();
                Debug.Log("[ProjectRegenerator] Synced with current editor.");
                return;
            }

            // Otherwise, try to find a fallback IDE
            string fallback = FindFallbackIDE();
            if (string.IsNullOrEmpty(fallback))
            {
                Debug.LogError("[ProjectRegenerator] Could not find Visual Studio, VS Code, or Rider to use for regeneration.");
                return;
            }

            // Debug.Log($"[ProjectRegenerator] Temporarily switching to: {fallback}");

            // Set, Sync, Revert
            CodeEditor.SetExternalScriptEditor(fallback);
            try
            {
                CodeEditor.CurrentEditor.SyncAll();
                Debug.Log("[ProjectRegenerator] Project files regenerated successfully (via temporary switch).");
            }
            finally
            {
                CodeEditor.SetExternalScriptEditor(originalEditor);
                // Debug.Log($"[ProjectRegenerator] Reverted editor to: {originalEditor}");
            }
        }

        private static bool IsKnownIDE(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.ToLower();
            return path.Contains("visual studio") || path.Contains("code.exe") || path.Contains("rider");
        }

        private static string FindFallbackIDE()
        {
            // Common Windows Paths
            string[] candidates = {
                // VS Code (User/System)
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code\Code.exe"),
                @"C:\Program Files\Microsoft VS Code\Code.exe",
                // Visual Studio 2022
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe",
                // Visual Studio 2019
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe"
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }
            return "";
        }
    }
}

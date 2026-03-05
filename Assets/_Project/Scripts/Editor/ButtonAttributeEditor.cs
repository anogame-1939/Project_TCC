// Assets/Editor/ButtonAttribute.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using editor = UnityEditor.Editor;
    
namespace AnoGame.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ButtonAttributeEditor : editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var mono = target as MonoBehaviour;
            var methods = mono.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic);

            foreach (var m in methods)
            {
                if (Attribute.IsDefined(m, typeof(ButtonAttribute)))
                {
                    if (GUILayout.Button(m.Name))
                    {
                        m.Invoke(mono, null);
                    }
                }
            }
        }
    }
}
#endif
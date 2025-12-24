//
#if UNITY_EDITOR
// KinoGlitch - Video glitch effect
//
// Copyright (C) 2015 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using UnityEngine;
using UnityEditor;

namespace Kino
{
    [CustomEditor(typeof(Kino.DigitalGlitch))]
    sealed class DigitalGlitchEditor : Editor
    {
        SerializedProperty _intensity;
        SerializedProperty _targetMaterial; // ★ 追加

        void OnEnable()
        {
            _intensity = serializedObject.FindProperty("_intensity");
            _targetMaterial = serializedObject.FindProperty("targetMaterial"); // ★ 追加
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_intensity);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_targetMaterial, new GUIContent("Target Material (URP)")); // ★ 追加
            EditorGUILayout.HelpBox("Renderer Feature の Digital Material と同じものを割り当ててください。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

using System;
using UnityEditor.Rendering;
using UnityEditor.Rendering.FernRenderPipeline;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.FernRenderPipeline;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.FernRenderPipeline
{
    [CustomEditor(typeof(AmbientProbeUpdateVolume))]
    public class AmbientProbeUpdateEditor : VolumeComponentEditor
    {
        private FernAdditionalReflectionProbe _mAdditionalReflectionProbe;
        private AmbientProbeUpdateVolume m_target;

        public override void OnEnable()
        {
            base.OnEnable();
            m_target = (AmbientProbeUpdateVolume)target;
        }

        public override void OnInspectorGUI()
        {
            _mAdditionalReflectionProbe = GameObject.FindObjectOfType<FernAdditionalReflectionProbe>();
            if (_mAdditionalReflectionProbe == null)
            {
                EditorGUILayout.Space();
                DrawFixMeBox(new GUIContent("Fern Reflection Probe not found, cannot be turned on"), "Fix", () =>
                {
                    FernRPMenu.CreateFernReflectionProbe();
                });
                m_target.isEnable.overrideState = false;
            }
            else
            {
                base.OnInspectorGUI();
            }
        }
        
        /// <summary>Draw a help box with the Fix button.</summary>
        /// <param name="message">The message with icon if needed.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        public static void DrawFixMeBox(GUIContent message, string buttonLabel, Action action)
        {
            EditorGUILayout.BeginHorizontal();

            float indent = EditorGUI.indentLevel * 15 - EditorStyles.helpBox.margin.left;
            GUILayoutUtility.GetRect(indent, EditorGUIUtility.singleLineHeight, EditorStyles.helpBox, GUILayout.ExpandWidth(false));

            Rect leftRect = GUILayoutUtility.GetRect(new GUIContent(buttonLabel), EditorStyles.miniButton, GUILayout.MinWidth(60));
            Rect rect = GUILayoutUtility.GetRect(message, EditorStyles.helpBox);
            Rect boxRect = new Rect(leftRect.x, rect.y, rect.xMax - leftRect.xMin, rect.height);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (Event.current.type == EventType.Repaint)
                EditorStyles.helpBox.Draw(boxRect, false, false, false, false);

            Rect labelRect = new Rect(boxRect.x + 4, boxRect.y + 3, rect.width - 8, rect.height);
            EditorGUI.LabelField(labelRect, message); // TODO: Error Type

            var buttonRect = leftRect;
            buttonRect.x += rect.width - 2;
            buttonRect.y = rect.yMin + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            bool clicked = GUI.Button(buttonRect, buttonLabel);

            EditorGUI.indentLevel = oldIndent;
            EditorGUILayout.EndHorizontal();

            if (clicked)
                action();
        }
    }
    
  
}
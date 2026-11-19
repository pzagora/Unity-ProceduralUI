using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal sealed class ShadowInspector
    {
        private readonly UnityEditor.Editor _editor;

        private const string TITLE = "Shadow";
        private const string STATE = "ProceduralUI.Inspector.Shadow";
        private static readonly GUIContent ENABLED = new("Enabled");
        private static readonly GUIContent COLOR = new("Color", "Solid shadow color.");
        private static readonly GUIContent STRENGTH = new("Strength", "Shadow opacity while sampling the content color.");
        private static readonly GUIContent SAMPLE = new("Sample Content Color", "Tint the shadow with the content's own colors instead of a solid color.");
        private static readonly GUIContent OFFSET = new("Offset", "Shadow position relative to the content, in local UI units.");
        private static readonly GUIContent SIZE = new("Size", "Blur radius in local UI units.");
        private static readonly GUIContent SPREAD = new("Spread", "Extra padding grown around the shape before blurring, in local UI units.");

        internal ShadowInspector(UnityEditor.Editor editor) => _editor = editor;

        internal void Draw()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!InspectorSection.DrawHeader(TITLE, STATE, BuildBadges()))
                    return;

                using (new EditorGUI.IndentLevelScope())
                    DrawBody();
            }
        }

        private void DrawBody()
        {
            var targets = _editor.targets;
            var shadows = new List<UIShadow>();
            foreach (var item in targets)
                if (item != null && ((ProceduralImage)item).TryGetComponent<UIShadow>(out var shadow) && shadow != null)
                    shadows.Add(shadow);

            EditorGUI.showMixedValue = shadows.Count != 0 && shadows.Count != targets.Length;
            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUILayout.Toggle(ENABLED, shadows.Count == targets.Length);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var item in targets)
                    ManagedShadow.SetEnabled(((ProceduralImage)item).gameObject, enabled);
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            if (shadows.Count == 0)
                return;

            var serialized = new SerializedObject(shadows.ToArray());
            serialized.Update();

            EditorGUI.BeginChangeCheck();
            var sample = serialized.FindProperty("sampleContentColor");
            var colorProperty = serialized.FindProperty("color");
            EditorGUILayout.PropertyField(sample, SAMPLE);
            if (!sample.hasMultipleDifferentValues && sample.boolValue)
                DrawStrength(colorProperty);
            else
                EditorGUILayout.PropertyField(colorProperty, COLOR);

            EditorGUILayout.PropertyField(serialized.FindProperty("offset"), OFFSET);
            EditorGUILayout.PropertyField(serialized.FindProperty("size"), SIZE);
            EditorGUILayout.PropertyField(serialized.FindProperty("spread"), SPREAD);

            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedProperties();
                foreach (var shadow in shadows)
                    shadow.Refresh();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private static void DrawStrength(SerializedProperty colorProperty)
        {
            EditorGUI.showMixedValue = colorProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var strength = EditorGUILayout.Slider(STRENGTH, colorProperty.colorValue.a, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                var value = colorProperty.colorValue;
                value.a = strength;
                colorProperty.colorValue = value;
            }

            EditorGUI.showMixedValue = false;
        }

        private IReadOnlyList<SectionBadge> BuildBadges()
        {
            var badges = new List<SectionBadge>();
            if (_editor.target is ProceduralImage image && image.TryGetComponent<UIShadow>(out var shadow))
            {
                var color = shadow.Color;
                badges.Add(SectionBadge.Create(
                    $"Shadow: size {shadow.Size:0.##}, spread {shadow.Spread:0.##}",
                    rect => BadgeGlyphs.Swatch(rect, color, color)));
            }

            return badges;
        }
    }

    internal static class ManagedShadow
    {
        internal static void SetEnabled(GameObject gameObject, bool enabled)
        {
            var has = gameObject.TryGetComponent<UIShadow>(out var shadow);
            if (enabled && !has)
            {
                shadow = Undo.AddComponent<UIShadow>(gameObject);
                Hide(shadow);
            }
            else if (!enabled && has)
            {
                Undo.DestroyObjectImmediate(shadow);
            }
        }

        internal static void HideManagedShadow(GameObject gameObject)
        {
            if (gameObject != null && gameObject.TryGetComponent<UIShadow>(out var shadow))
                Hide(shadow);
        }

        private static void Hide(UIShadow shadow)
        {
            if ((shadow.hideFlags & HideFlags.HideInInspector) != 0)
                return;

            shadow.hideFlags |= HideFlags.HideInInspector;
        }
    }
}

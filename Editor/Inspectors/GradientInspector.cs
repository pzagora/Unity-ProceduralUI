using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal sealed class GradientInspector
    {
        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _type;
        private readonly SerializedProperty _blendMode;
        private readonly SerializedProperty _intensity;
        private readonly SerializedProperty _angle;
        private readonly SerializedProperty _startColor;
        private readonly SerializedProperty _endColor;
        private readonly SerializedProperty _topLeft;
        private readonly SerializedProperty _topRight;
        private readonly SerializedProperty _bottomLeft;
        private readonly SerializedProperty _bottomRight;
        private readonly SerializedProperty _ramp;

        private const string TITLE = "Gradient";
        private const string STATE = "ProceduralUI.Inspector.Gradient";
        private const string ENABLED_PROPERTY = "enabled";
        private const string TYPE_PROPERTY = "type";
        private const string BLEND_MODE_PROPERTY = "blendMode";
        private const string INTENSITY_PROPERTY = "intensity";
        private const string ANGLE_PROPERTY = "angle";
        private const string START_COLOR_PROPERTY = "startColor";
        private const string END_COLOR_PROPERTY = "endColor";
        private const string TOP_LEFT_PROPERTY = "topLeft";
        private const string TOP_RIGHT_PROPERTY = "topRight";
        private const string BOTTOM_LEFT_PROPERTY = "bottomLeft";
        private const string BOTTOM_RIGHT_PROPERTY = "bottomRight";
        private const string RAMP_PROPERTY = "ramp";
        private static readonly GUIContent ANGLE = new("Angle", "Zero runs from bottom to top. Positive angles rotate the gradient coordinates counterclockwise.");
        private static readonly GUIContent START = new("Start Color", "Color at the bottom when the angle is zero.");
        private static readonly GUIContent END = new("End Color", "Color at the top when the angle is zero.");

        internal GradientInspector(SerializedProperty gradient)
        {
            _enabled = gradient.FindPropertyRelative(ENABLED_PROPERTY);
            _type = gradient.FindPropertyRelative(TYPE_PROPERTY);
            _blendMode = gradient.FindPropertyRelative(BLEND_MODE_PROPERTY);
            _intensity = gradient.FindPropertyRelative(INTENSITY_PROPERTY);
            _angle = gradient.FindPropertyRelative(ANGLE_PROPERTY);
            _startColor = gradient.FindPropertyRelative(START_COLOR_PROPERTY);
            _endColor = gradient.FindPropertyRelative(END_COLOR_PROPERTY);
            _topLeft = gradient.FindPropertyRelative(TOP_LEFT_PROPERTY);
            _topRight = gradient.FindPropertyRelative(TOP_RIGHT_PROPERTY);
            _bottomLeft = gradient.FindPropertyRelative(BOTTOM_LEFT_PROPERTY);
            _bottomRight = gradient.FindPropertyRelative(BOTTOM_RIGHT_PROPERTY);
            _ramp = gradient.FindPropertyRelative(RAMP_PROPERTY);
        }

        internal void Draw()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!InspectorSection.DrawHeader(TITLE, STATE, BuildBadges()))
                    return;
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_enabled);
                    if (_enabled.hasMultipleDifferentValues || !_enabled.boolValue)
                        return;

                    EditorGUILayout.PropertyField(_type);
                    if (!_type.hasMultipleDifferentValues)
                        DrawColors((GradientType)_type.intValue);
                    EditorGUILayout.PropertyField(_angle, ANGLE);
                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(_blendMode);
                    EditorGUILayout.PropertyField(_intensity);
                }
            }
        }

        private IReadOnlyList<SectionBadge> BuildBadges()
        {
            var badges = new List<SectionBadge>();
            if (_enabled.hasMultipleDifferentValues || !_enabled.boolValue || _type.hasMultipleDifferentValues)
                return badges;

            var type = (GradientType)_type.intValue;
            var (start, end) = SwatchColors(type);
            badges.Add(SectionBadge.Create(BadgeTooltip(type), rect => BadgeGlyphs.Swatch(rect, start, end)));
            return badges;
        }

        private (Color start, Color end) SwatchColors(GradientType type) => type switch
        {
            GradientType.Corners => (_topLeft.colorValue, _bottomRight.colorValue),
            GradientType.Ramp => (Color.black, Color.white),
            _ => (_startColor.colorValue, _endColor.colorValue)
        };

        private string BadgeTooltip(GradientType type)
        {
            var blend = _blendMode.hasMultipleDifferentValues
                ? "—"
                : _blendMode.enumDisplayNames[_blendMode.enumValueIndex];
            var intensity = _intensity.hasMultipleDifferentValues ? "—" : _intensity.floatValue.ToString("0.##");
            return $"Gradient: {type} · {blend} · Intensity {intensity}";
        }

        private void DrawColors(GradientType type)
        {
            switch (type)
            {
                case GradientType.Linear:
                    EditorGUILayout.PropertyField(_startColor, START);
                    EditorGUILayout.PropertyField(_endColor, END);
                    break;
                case GradientType.Corners:
                    EditorGUILayout.PropertyField(_topLeft);
                    EditorGUILayout.PropertyField(_topRight);
                    EditorGUILayout.PropertyField(_bottomLeft);
                    EditorGUILayout.PropertyField(_bottomRight);
                    break;
                case GradientType.Ramp:
                    EditorGUILayout.PropertyField(_ramp);
                    break;
            }
        }
    }
}

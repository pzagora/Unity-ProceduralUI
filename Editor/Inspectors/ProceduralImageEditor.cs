using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI.Editor
{
    [CustomEditor(typeof(ProceduralImage), true)]
    [CanEditMultipleObjects]
    internal sealed class ProceduralImageEditor : ImageEditor
    {
        private SerializedProperty _cornerMode;
        private SerializedProperty _uniformRadius;
        private SerializedProperty _cornerRadii;
        private SerializedProperty _roundedEdge;
        private SerializedProperty _borderWidth;
        private SerializedProperty _falloffDistance;
        private SerializedProperty _sprite;
        private SerializedProperty _type;
        private SerializedProperty _preserveAspect;
        private SerializedProperty _useSpriteMesh;
        private GradientInspector _gradient;

        private static readonly GUIContent SOURCE_SPRITE = new("Source Sprite");
        private static readonly GUIContent RADIUS = new("Radius", "Corner radius in local UI units.");
        private static readonly GUIContent TOP_LEFT = new("Top Left");
        private static readonly GUIContent TOP_RIGHT = new("Top Right");
        private static readonly GUIContent BOTTOM_LEFT = new("Bottom Left");
        private static readonly GUIContent BOTTOM_RIGHT = new("Bottom Right");
        private static readonly GUIContent BORDER = new("Border Width", "Zero fills the shape. Positive values draw an outline.");
        private static readonly GUIContent FALLOFF = new("Edge Softness", "Anti-aliasing distance in local UI units. Zero produces a hard edge.");
        private const string APPEARANCE = "Appearance";
        private const string SHAPE = "Shape";
        private const string INTERACTION = "Interaction & Masking";
        private const string NATIVE_SIZE = "Set Native Size";
        private const string MATERIAL_WARNING = "A custom material must support the Procedural Image vertex data to render corners and outlines.";
        private const string CORNER_MODE_PROPERTY = "cornerMode";
        private const string UNIFORM_RADIUS_PROPERTY = "uniformRadius";
        private const string CORNER_RADII_PROPERTY = "cornerRadii";
        private const string ROUNDED_EDGE_PROPERTY = "roundedEdge";
        private const string BORDER_WIDTH_PROPERTY = "borderWidth";
        private const string FALLOFF_PROPERTY = "falloffDistance";
        private const string SPRITE_PROPERTY = "m_Sprite";
        private const string TYPE_PROPERTY = "m_Type";
        private const string PRESERVE_ASPECT_PROPERTY = "m_PreserveAspect";
        private const string USE_SPRITE_MESH_PROPERTY = "m_UseSpriteMesh";
        private const string TOP_LEFT_PROPERTY = "x";
        private const string TOP_RIGHT_PROPERTY = "y";
        private const string BOTTOM_RIGHT_PROPERTY = "z";
        private const string BOTTOM_LEFT_PROPERTY = "w";
        private const string GRADIENT_PROPERTY = "gradient";
        private const string SHAPE_STATE = "ProceduralUI.Inspector.Shape";
        private const string INTERACTION_STATE = "ProceduralUI.Inspector.Interaction";

        protected override void OnEnable()
        {
            base.OnEnable();
            _cornerMode = serializedObject.FindProperty(CORNER_MODE_PROPERTY);
            _uniformRadius = serializedObject.FindProperty(UNIFORM_RADIUS_PROPERTY);
            _cornerRadii = serializedObject.FindProperty(CORNER_RADII_PROPERTY);
            _roundedEdge = serializedObject.FindProperty(ROUNDED_EDGE_PROPERTY);
            _borderWidth = serializedObject.FindProperty(BORDER_WIDTH_PROPERTY);
            _falloffDistance = serializedObject.FindProperty(FALLOFF_PROPERTY);
            _sprite = serializedObject.FindProperty(SPRITE_PROPERTY);
            _type = serializedObject.FindProperty(TYPE_PROPERTY);
            _preserveAspect = serializedObject.FindProperty(PRESERVE_ASPECT_PROPERTY);
            _useSpriteMesh = serializedObject.FindProperty(USE_SPRITE_MESH_PROPERTY);
            _gradient = new GradientInspector(serializedObject.FindProperty(GRADIENT_PROPERTY));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawAppearance();
            EditorGUILayout.Space(6);
            DrawShape();
            EditorGUILayout.Space(6);
            _gradient.Draw();
            EditorGUILayout.Space(6);
            DrawInteraction();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAppearance()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(APPEARANCE, EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawSprite();
                    AppearanceControlsGUI();
                    if (m_Material.objectReferenceValue != null || m_Material.hasMultipleDifferentValues)
                        EditorGUILayout.HelpBox(MATERIAL_WARNING, MessageType.Info);

                    TypeGUI();
                    if (!_type.hasMultipleDifferentValues && (Image.Type)_type.intValue is Image.Type.Simple or Image.Type.Filled)
                    {
                        EditorGUILayout.PropertyField(_preserveAspect);
                        if ((Image.Type)_type.intValue == Image.Type.Simple)
                            EditorGUILayout.PropertyField(_useSpriteMesh);

                        DrawNativeSize();
                    }
                }
            }
        }

        private void DrawSprite()
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, SOURCE_SPRITE, _sprite);
            EditorGUI.showMixedValue = _sprite.hasMultipleDifferentValues;
            var sprite = _sprite.objectReferenceValue as Sprite;
            if (sprite == ProceduralImageAssets.WhiteSprite)
                sprite = null;

            EditorGUI.BeginChangeCheck();
            var selected = EditorGUI.ObjectField(rect, SOURCE_SPRITE, sprite, typeof(Sprite), false) as Sprite;
            if (EditorGUI.EndChangeCheck())
            {
                _sprite.objectReferenceValue = selected != null ? selected : ProceduralImageAssets.WhiteSprite;
                foreach (var item in targets)
                    ((ProceduralImage)item).DisableSpriteOptimizations();
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        private void DrawNativeSize()
        {
            using (new EditorGUI.DisabledScope(_sprite.hasMultipleDifferentValues ||
                       _sprite.objectReferenceValue == null || _sprite.objectReferenceValue == ProceduralImageAssets.WhiteSprite))
            {
                var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                if (!GUI.Button(rect, NATIVE_SIZE))
                    return;

                serializedObject.ApplyModifiedProperties();
                foreach (var item in targets)
                {
                    var image = (ProceduralImage)item;
                    Undo.RecordObject(image.rectTransform, NATIVE_SIZE);
                    image.SetNativeSize();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(image.rectTransform);
                }
            }
        }

        private void DrawShape()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!InspectorSection.DrawHeader(SHAPE, SHAPE_STATE))
                    return;
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_cornerMode);
                    if (!_cornerMode.hasMultipleDifferentValues)
                        DrawCornerSettings((CornerMode)_cornerMode.intValue);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(_borderWidth, BORDER);
                    EditorGUILayout.PropertyField(_falloffDistance, FALLOFF);
                }
            }
        }

        private void DrawCornerSettings(CornerMode mode)
        {
            switch (mode)
            {
                case CornerMode.Uniform:
                    EditorGUILayout.PropertyField(_uniformRadius, RADIUS);
                    break;
                case CornerMode.Individual:
                    EditorGUILayout.PropertyField(_cornerRadii.FindPropertyRelative(TOP_LEFT_PROPERTY), TOP_LEFT);
                    EditorGUILayout.PropertyField(_cornerRadii.FindPropertyRelative(TOP_RIGHT_PROPERTY), TOP_RIGHT);
                    EditorGUILayout.PropertyField(_cornerRadii.FindPropertyRelative(BOTTOM_LEFT_PROPERTY), BOTTOM_LEFT);
                    EditorGUILayout.PropertyField(_cornerRadii.FindPropertyRelative(BOTTOM_RIGHT_PROPERTY), BOTTOM_RIGHT);
                    break;
                case CornerMode.Edge:
                    EditorGUILayout.PropertyField(_roundedEdge);
                    EditorGUILayout.PropertyField(_uniformRadius, RADIUS);
                    break;
            }
        }

        private void DrawInteraction()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!InspectorSection.DrawHeader(INTERACTION, INTERACTION_STATE))
                    return;
                using (new EditorGUI.IndentLevelScope())
                {
                    RaycastControlsGUI();
                    MaskableControlsGUI();
                }
            }
        }
    }
}

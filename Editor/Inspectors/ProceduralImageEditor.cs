using System.Collections.Generic;
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
        private SerializedProperty _raycastTarget;
        private SerializedProperty _maskable;
        private GradientInspector _gradient;
        private ShadowInspector _shadow;

        private static readonly GUIContent SOURCE_SPRITE = new("Source Sprite");
        private static readonly GUIContent RADIUS = new("Radius", "Corner radius in local UI units.");
        private static readonly GUIContent TOP_LEFT = new("Top Left");
        private static readonly GUIContent TOP_RIGHT = new("Top Right");
        private static readonly GUIContent BOTTOM_LEFT = new("Bottom Left");
        private static readonly GUIContent BOTTOM_RIGHT = new("Bottom Right");
        private static readonly GUIContent BORDER = new("Border Width", "Zero fills the shape. Positive values draw an outline.");
        private static readonly GUIContent FALLOFF = new("Edge Softness", "Anti-aliasing distance in local UI units. Zero produces a hard edge.");
        private static readonly GUIContent MASK_CHILDREN = new("Mask Children",
            "Clip child graphics to this image's rounded shape using a uGUI Mask (added and removed automatically).");
        private static readonly GUIContent SHOW_MASK_GRAPHIC = new("Show Mask Graphic",
            "Draw this image as well as using it to mask its children.");
        private const string APPEARANCE = "Appearance";
        private const string SHAPE = "Shape";
        private const string INTERACTION = "Interaction";
        private const string MASKING = "Masking";
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
        private const string RAYCAST_TARGET_PROPERTY = "m_RaycastTarget";
        private const string MASKABLE_PROPERTY = "m_Maskable";
        private const string SHAPE_STATE = "ProceduralUI.Inspector.Shape";
        private const string INTERACTION_STATE = "ProceduralUI.Inspector.Interaction";
        private const string MASKING_STATE = "ProceduralUI.Inspector.Masking";

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
            _raycastTarget = serializedObject.FindProperty(RAYCAST_TARGET_PROPERTY);
            _maskable = serializedObject.FindProperty(MASKABLE_PROPERTY);
            _gradient = new GradientInspector(serializedObject.FindProperty(GRADIENT_PROPERTY));
            _shadow = new ShadowInspector(this);

            foreach (var item in targets)
            {
                var gameObject = ((ProceduralImage)item).gameObject;
                ManagedMask.HideManagedMask(gameObject);
                ManagedShadow.HideManagedShadow(gameObject);
            }
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
            _shadow.Draw();
            EditorGUILayout.Space(6);
            DrawInteraction();
            EditorGUILayout.Space(6);
            DrawMasking();
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
                if (!InspectorSection.DrawHeader(SHAPE, SHAPE_STATE, BuildShapeBadges()))
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
                if (!InspectorSection.DrawHeader(INTERACTION, INTERACTION_STATE, BuildInteractionBadges()))
                    return;
                using (new EditorGUI.IndentLevelScope())
                    RaycastControlsGUI();
            }
        }

        private void DrawMasking()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!InspectorSection.DrawHeader(MASKING, MASKING_STATE, BuildMaskingBadges()))
                    return;
                using (new EditorGUI.IndentLevelScope())
                {
                    MaskableControlsGUI();
                    DrawMaskControls();
                }
            }
        }

        private void DrawMaskControls()
        {
            var masks = new List<Mask>();
            var withMask = 0;
            foreach (var item in targets)
            {
                var gameObject = ((ProceduralImage)item).gameObject;
                if (gameObject.TryGetComponent<Mask>(out var mask))
                {
                    masks.Add(mask);
                    withMask++;
                }
            }

            EditorGUI.showMixedValue = withMask != 0 && withMask != targets.Length;
            EditorGUI.BeginChangeCheck();
            var enabled = EditorGUILayout.Toggle(MASK_CHILDREN, withMask == targets.Length);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var item in targets)
                    ManagedMask.SetMaskChildren(((ProceduralImage)item).gameObject, enabled);
            }
            EditorGUI.showMixedValue = false;

            if (masks.Count == 0)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var serialized = new SerializedObject(masks.ToArray());
                EditorGUILayout.PropertyField(serialized.FindProperty(ManagedMask.ShowMaskGraphicProperty), SHOW_MASK_GRAPHIC);
                serialized.ApplyModifiedProperties();
            }
        }

        private IReadOnlyList<SectionBadge> BuildShapeBadges()
        {
            var badges = new List<SectionBadge>();

            if (!_cornerMode.hasMultipleDifferentValues && TryGetMaxRadius(out var maxRadius) && maxRadius > 0f)
                badges.Add(SectionBadge.Create(CornerTooltip((CornerMode)_cornerMode.intValue), BadgeGlyphs.RoundedRect));

            if (!_borderWidth.hasMultipleDifferentValues && _borderWidth.floatValue > 0f)
                badges.Add(SectionBadge.Create($"Outline width {_borderWidth.floatValue:0.##}", BadgeGlyphs.Ring));

            if (!_falloffDistance.hasMultipleDifferentValues && _falloffDistance.floatValue > 0f)
                badges.Add(SectionBadge.Create($"Edge softness {_falloffDistance.floatValue:0.##}", BadgeGlyphs.Softness));

            return badges;
        }

        private IReadOnlyList<SectionBadge> BuildInteractionBadges()
        {
            var badges = new List<SectionBadge>();

            if (!_raycastTarget.hasMultipleDifferentValues && _raycastTarget.boolValue)
                badges.Add(SectionBadge.Create("Raycast Target: on", BadgeGlyphs.Crosshair));

            return badges;
        }

        private IReadOnlyList<SectionBadge> BuildMaskingBadges()
        {
            var badges = new List<SectionBadge>();

            if (!_maskable.hasMultipleDifferentValues && _maskable.boolValue)
                badges.Add(SectionBadge.Create("Maskable: on", BadgeGlyphs.FilledSquare));

            if (target is ProceduralImage image && image.TryGetComponent<Mask>(out var mask) && mask.enabled)
                badges.Add(SectionBadge.Create("Masks children", BadgeGlyphs.Mask));

            return badges;
        }

        private bool TryGetMaxRadius(out float maxRadius)
        {
            maxRadius = 0f;
            switch ((CornerMode)_cornerMode.intValue)
            {
                case CornerMode.Uniform:
                case CornerMode.Edge:
                    if (_uniformRadius.hasMultipleDifferentValues)
                        return false;
                    maxRadius = _uniformRadius.floatValue;
                    return true;
                case CornerMode.Individual:
                    if (_cornerRadii.hasMultipleDifferentValues)
                        return false;
                    var radii = _cornerRadii.vector4Value;
                    maxRadius = Mathf.Max(Mathf.Max(radii.x, radii.y), Mathf.Max(radii.z, radii.w));
                    return true;
                case CornerMode.Round:
                    maxRadius = 1f;
                    return true;
                default:
                    return false;
            }
        }

        private string CornerTooltip(CornerMode mode)
        {
            switch (mode)
            {
                case CornerMode.Uniform:
                    return $"Corners: Uniform, radius {_uniformRadius.floatValue:0.##}";
                case CornerMode.Individual:
                    var r = _cornerRadii.vector4Value;
                    return $"Corners: Individual (TL {r.x:0.##}, TR {r.y:0.##}, BR {r.z:0.##}, BL {r.w:0.##})";
                case CornerMode.Edge:
                    var rounded = _roundedEdge.hasMultipleDifferentValues
                        ? string.Empty
                        : _roundedEdge.boolValue ? ", rounded edge" : ", straight edge";
                    return $"Corners: Edge, radius {_uniformRadius.floatValue:0.##}{rounded}";
                case CornerMode.Round:
                    return "Corners: Round (pill)";
                default:
                    return "Corners";
            }
        }
    }
}

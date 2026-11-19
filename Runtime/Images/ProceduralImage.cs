using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace ProceduralUI
{
    [ExecuteAlways]
    [AddComponentMenu(COMPONENT_MENU)]
    [MovedFrom("UnityEngine.UI.ProceduralImage")]
    public class ProceduralImage : Image
    {
        [SerializeField] private CornerMode cornerMode;
        [SerializeField, Min(0)] private float uniformRadius;
        [SerializeField] private Vector4 cornerRadii;
        [SerializeField] private RoundedEdge roundedEdge;
        [SerializeField, Min(0)] private float borderWidth;
        [SerializeField, Min(0)] private float falloffDistance = 1f;
        [SerializeField] private GradientSettings gradient = GradientSettings.Create();

        private GradientMeshRenderer _gradientRenderer;

        private ImageGradient _gradient;

        public ImageGradient Gradient => _gradient ??= new ImageGradient(GetGradientSettings, SetVerticesDirty);

        public CornerMode CornerMode
        {
            get => cornerMode;
            set => SetValue(ref cornerMode, value);
        }

        public float UniformRadius
        {
            get => uniformRadius;
            set => SetValue(ref uniformRadius, Mathf.Max(0, value));
        }

        public Vector4 CornerRadii
        {
            get => cornerRadii;
            set => SetValue(ref cornerRadii, Vector4.Max(Vector4.zero, value));
        }

        public RoundedEdge RoundedEdge
        {
            get => roundedEdge;
            set => SetValue(ref roundedEdge, value);
        }

        public float BorderWidth
        {
            get => borderWidth;
            set => SetValue(ref borderWidth, Mathf.Max(0, value));
        }

        public float FalloffDistance
        {
            get => falloffDistance;
            set => SetValue(ref falloffDistance, Mathf.Max(0, value));
        }

        public override Material defaultMaterial => ProceduralImageAssets.Material;

        public override Material material
        {
            get => m_Material != null ? m_Material : defaultMaterial;
            set => base.material = value;
        }

        private const string COMPONENT_MENU = "UI/Procedural Image";
        private const AdditionalCanvasShaderChannels SHADER_CHANNELS =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 |
            AdditionalCanvasShaderChannels.TexCoord3;

        protected override void OnEnable()
        {
            EnsureSprite();
            base.OnEnable();
            RegisterDirtyVerticesCallback(EnsureSprite);
            EnsureShaderChannels();
        }

        protected override void OnDisable()
        {
            UnregisterDirtyVerticesCallback(EnsureSprite);
            base.OnDisable();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            EnsureShaderChannels();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            EnsureShaderChannels();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            base.OnPopulateMesh(vertices);
            var rect = ProceduralImageGeometry.GetDrawingRect(this);
            if (gradient.IsActive)
            {
                _gradientRenderer ??= new GradientMeshRenderer();
                _gradientRenderer.Apply(vertices, rect, gradient);
            }
            var radii = ProceduralImageGeometry.CalculateRadii(rect, cornerMode, uniformRadius, cornerRadii, roundedEdge);
            var activeSprite = overrideSprite;
            var splitAlpha = activeSprite != null && activeSprite.associatedAlphaSplitTexture != null;
            ProceduralImageGeometry.Encode(vertices, rect, radii, borderWidth, falloffDistance, splitAlpha);
        }

        private void EnsureSprite()
        {
            if (sprite == null)
                sprite = ProceduralImageAssets.WhiteSprite;
        }

        private void EnsureShaderChannels()
        {
            if (canvas != null)
                canvas.additionalShaderChannels |= SHADER_CHANNELS;
        }

        private void SetValue<T>(ref T field, T value)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            SetVerticesDirty();
        }

        private ref GradientSettings GetGradientSettings()
            => ref gradient;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            uniformRadius = Mathf.Max(0, uniformRadius);
            cornerRadii = Vector4.Max(Vector4.zero, cornerRadii);
            borderWidth = Mathf.Max(0, borderWidth);
            falloffDistance = Mathf.Max(0, falloffDistance);
            gradient.InvalidateRamp();
            base.OnValidate();
        }
#endif
    }
}

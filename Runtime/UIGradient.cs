using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    [AddComponentMenu(COMPONENT_MENU)]
    [DisallowMultipleComponent]
    public sealed class UIGradient : BaseMeshEffect
    {
        [SerializeField] private GradientSettings gradient = GradientSettings.Create(true);
        private GradientMeshRenderer _renderer;
        private ImageGradient _gradient;

        public ImageGradient Gradient => _gradient ??= new ImageGradient(GetGradientSettings, SetVerticesDirty);

        private const string COMPONENT_MENU = "UI/Effects/UI Gradient";

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || !gradient.IsActive)
                return;

            _renderer ??= new GradientMeshRenderer();
            var rect = graphic is Image image ? ProceduralImageGeometry.GetDrawingRect(image) : graphic.rectTransform.rect;
            _renderer.Apply(vertices, rect, gradient);
        }

        private void SetVerticesDirty()
        {
            if (IsActive() && graphic != null)
                graphic.SetVerticesDirty();
        }

        private ref GradientSettings GetGradientSettings()
            => ref gradient;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            gradient.InvalidateRamp();
            base.OnValidate();
        }
#endif
    }
}

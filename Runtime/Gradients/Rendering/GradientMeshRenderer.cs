using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    internal sealed class GradientMeshRenderer
    {
        private GradientMeshSlicer _slicer;

        private const float MIN_SIZE = 0.0001f;

        internal void Apply(VertexHelper vertices, Rect rect, in GradientSettings gradient)
        {
            if (!gradient.IsActive || vertices.currentVertCount == 0 || rect.width <= MIN_SIZE || rect.height <= MIN_SIZE)
                return;

            var space = new GradientSpace(rect, gradient.angle);
            if (gradient.type == GradientType.Ramp)
            {
                _slicer ??= new GradientMeshSlicer();
                if (_slicer.TryApply(vertices, gradient, space))
                    return;
            }

            var vertex = default(UIVertex);
            for (var index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                vertex.color = gradient.Blend(vertex.color, gradient.Evaluate(space.Project(vertex.position)));
                vertices.SetUIVertex(vertex, index);
            }
        }
    }
}

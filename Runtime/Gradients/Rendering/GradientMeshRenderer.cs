using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    internal sealed class GradientMeshRenderer
    {
        private GradientMeshSlicer _slicer;
        private List<UIVertex> _bufferA = new();
        private List<UIVertex> _bufferB = new();

        private const float MIN_SIZE = 0.0001f;
        private const int SUBDIVISION_LEVEL = 3;
        private const int MAX_OUTPUT_VERTICES = 48000;

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

            _bufferA.Clear();
            vertices.GetUIVertexStream(_bufferA);
            if (_bufferA.Count == 0)
                return;

            var level = SubdivisionLevel(_bufferA.Count);
            for (var pass = 0; pass < level; pass++)
            {
                if (!Subdivide(_bufferA, _bufferB))
                    break;
                (_bufferA, _bufferB) = (_bufferB, _bufferA);
            }

            for (var index = 0; index < _bufferA.Count; index++)
            {
                var vertex = _bufferA[index];
                vertex.color = gradient.Blend(vertex.color, gradient.Evaluate(space.Project(vertex.position)));
                _bufferA[index] = vertex;
            }

            vertices.Clear();
            vertices.AddUIVertexTriangleStream(_bufferA);
            _bufferA.Clear();
            _bufferB.Clear();
        }

        private static int SubdivisionLevel(int vertexCount)
        {
            var triangles = Mathf.Max(1, vertexCount / 3);
            var level = SUBDIVISION_LEVEL;
            while (level > 0 && triangles * Mathf.Pow(4, level) * 3f > MAX_OUTPUT_VERTICES)
                level--;
            return level;
        }

        private static bool Subdivide(List<UIVertex> source, List<UIVertex> destination)
        {
            destination.Clear();
            if (source.Count * 4 > MAX_OUTPUT_VERTICES)
            {
                destination.AddRange(source);
                return false;
            }

            for (var index = 0; index + 2 < source.Count; index += 3)
            {
                var a = source[index];
                var b = source[index + 1];
                var c = source[index + 2];
                var ab = Midpoint(a, b);
                var bc = Midpoint(b, c);
                var ca = Midpoint(c, a);

                AddTriangle(destination, a, ab, ca);
                AddTriangle(destination, ab, b, bc);
                AddTriangle(destination, ca, bc, c);
                AddTriangle(destination, ab, bc, ca);
            }

            return true;
        }

        private static void AddTriangle(List<UIVertex> list, in UIVertex a, in UIVertex b, in UIVertex c)
        {
            list.Add(a);
            list.Add(b);
            list.Add(c);
        }

        private static UIVertex Midpoint(in UIVertex a, in UIVertex b) => new()
        {
            position = Vector3.LerpUnclamped(a.position, b.position, 0.5f),
            normal = Vector3.LerpUnclamped(a.normal, b.normal, 0.5f),
            tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, 0.5f),
            color = Color.LerpUnclamped(a.color, b.color, 0.5f),
            uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, 0.5f),
            uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, 0.5f),
            uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, 0.5f),
            uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, 0.5f)
        };
    }
}

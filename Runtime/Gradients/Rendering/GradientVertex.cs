using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    internal readonly struct GradientVertex
    {
        internal readonly UIVertex Vertex;
        internal readonly float Position;

        internal GradientVertex(UIVertex vertex, float position)
        {
            Vertex = vertex;
            Position = position;
        }

        internal static GradientVertex Intersect(GradientVertex first, GradientVertex second, float position)
        {
            var fraction = Mathf.Clamp01((position - first.Position) / (second.Position - first.Position));
            var a = first.Vertex;
            var b = second.Vertex;
            var vertex = new UIVertex
            {
                position = Vector3.LerpUnclamped(a.position, b.position, fraction),
                color = Color.LerpUnclamped(a.color, b.color, fraction),
                normal = Vector3.LerpUnclamped(a.normal, b.normal, fraction),
                tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, fraction),
                uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, fraction),
                uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, fraction),
                uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, fraction),
                uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, fraction)
            };
            return new GradientVertex(vertex, position);
        }
    }
}

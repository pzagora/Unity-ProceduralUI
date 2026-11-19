using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace ProceduralUI
{
    internal static class ProceduralImageGeometry
    {
        private const float MIN_SIZE = 0.0001f;
        private const float MAX_PIXEL_SCALE = 2048f;

        internal static Rect GetDrawingRect(Image image)
        {
            var rect = image.GetPixelAdjustedRect();
            var sprite = image.overrideSprite;
            if (sprite == null || image.type is Image.Type.Sliced or Image.Type.Tiled)
                return rect;

            var size = sprite.rect.size;
            if (size.x <= MIN_SIZE || size.y <= MIN_SIZE)
                return rect;

            if (image.preserveAspect && rect.width > MIN_SIZE && rect.height > MIN_SIZE)
            {
                var scale = Mathf.Min(rect.width / size.x, rect.height / size.y);
                var fitted = size * scale;
                rect.position += Vector2.Scale(rect.size - fitted, image.rectTransform.pivot);
                rect.size = fitted;
            }

            if (image.type == Image.Type.Simple && image.useSpriteMesh)
                return rect;

            var padding = DataUtility.GetPadding(sprite);
            var width = Mathf.Max(1, Mathf.RoundToInt(size.x));
            var height = Mathf.Max(1, Mathf.RoundToInt(size.y));
            return Rect.MinMaxRect(
                rect.x + rect.width * padding.x / width,
                rect.y + rect.height * padding.y / height,
                rect.xMax - rect.width * padding.z / width,
                rect.yMax - rect.height * padding.w / height);
        }

        internal static Vector4 CalculateRadii(Rect rect, CornerMode mode, float radius, Vector4 corners, RoundedEdge edge)
        {
            var radii = mode switch
            {
                CornerMode.Uniform => Vector4.one * radius,
                CornerMode.Individual => corners,
                CornerMode.Round => Vector4.one * (Mathf.Min(rect.width, rect.height) * 0.5f),
                CornerMode.Edge => EdgeRadii(radius, edge),
                _ => Vector4.zero
            };

            radii = Vector4.Max(Vector4.zero, radii);
            var scale = Mathf.Min(1f, Fit(rect.width, radii.x + radii.y));
            scale = Mathf.Min(scale, Fit(rect.width, radii.z + radii.w));
            scale = Mathf.Min(scale, Fit(rect.height, radii.x + radii.w));
            scale = Mathf.Min(scale, Fit(rect.height, radii.y + radii.z));
            return radii * scale;
        }

        internal static void Encode(VertexHelper vertices, Rect rect, Vector4 radii, float border, float falloff, bool splitAlpha)
        {
            var minimumSide = Mathf.Min(rect.width, rect.height);
            if (minimumSide <= MIN_SIZE)
            {
                vertices.Clear();
                return;
            }

            radii /= minimumSide;
            falloff = Mathf.Max(0, falloff);
            var packedRadii = new Vector4(Pack(radii.x, radii.y), Pack(radii.z, radii.w), splitAlpha ? 1 : 0, 0);
            var borderData = new Vector4(border <= 0 ? 1 : Mathf.Clamp01(border / minimumSide * 2),
                falloff > 0 ? Mathf.Min(MAX_PIXEL_SCALE, 1f / falloff) : MAX_PIXEL_SCALE, 0, 0);
            var vertex = default(UIVertex);

            for (var index = 0; index < vertices.currentVertCount; index++)
            {
                vertices.PopulateUIVertex(ref vertex, index);
                var x = (vertex.position.x - rect.x) / rect.width;
                var y = (vertex.position.y - rect.y) / rect.height;
                vertex.position += new Vector3(x - 0.5f, y - 0.5f) * falloff;
                vertex.uv1 = new Vector4(rect.width + falloff, rect.height + falloff, x, y);
                vertex.uv2 = packedRadii;
                vertex.uv3 = borderData;
                vertices.SetUIVertex(vertex, index);
            }
        }

        private static float Fit(float size, float sum)
            => sum > MIN_SIZE ? Mathf.Max(0, size) / sum : 1f;

        private static Vector4 EdgeRadii(float radius, RoundedEdge edge) => edge switch
        {
            RoundedEdge.Top => new Vector4(radius, radius, 0, 0),
            RoundedEdge.Bottom => new Vector4(0, 0, radius, radius),
            RoundedEdge.Left => new Vector4(radius, 0, 0, radius),
            RoundedEdge.Right => new Vector4(0, radius, radius, 0),
            _ => Vector4.zero
        };

        private static float Pack(float first, float second)
            => Mathf.Floor(Mathf.Clamp01(first) * 65534f) / 65535f
                + Mathf.Floor(Mathf.Clamp01(second) * 65534f) / (65535f * 65535f);
    }
}

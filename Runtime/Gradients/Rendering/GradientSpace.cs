using UnityEngine;

namespace ProceduralUI
{
    internal readonly struct GradientSpace
    {
        private readonly Vector2 _minimum;
        private readonly Vector2 _inverseSize;
        private readonly float _sine;
        private readonly float _cosine;

        internal GradientSpace(Rect rect, float angle)
        {
            _minimum = rect.min;
            _inverseSize = new Vector2(1f / rect.width, 1f / rect.height);
            var radians = Mathf.Repeat(angle, 360f) * Mathf.Deg2Rad;
            _sine = Mathf.Sin(radians);
            _cosine = Mathf.Cos(radians);
        }

        internal Vector2 Project(Vector3 position)
        {
            var normalized = Vector2.Scale((Vector2)position - _minimum, _inverseSize) - Vector2.one * 0.5f;
            return new Vector2(_cosine * normalized.x - _sine * normalized.y + 0.5f,
                _sine * normalized.x + _cosine * normalized.y + 0.5f);
        }
    }
}

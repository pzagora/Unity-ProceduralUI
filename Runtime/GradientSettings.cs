using System;
using UnityEngine;

namespace ProceduralUI
{
    [Serializable]
    internal struct GradientSettings
    {
        [SerializeField] internal bool enabled;
        [SerializeField] internal GradientType type;
        [SerializeField] internal GradientBlendMode blendMode;
        [SerializeField, Range(0, 1)] internal float intensity;
        [SerializeField, Range(0, 360)] internal float angle;
        [SerializeField] internal Color startColor;
        [SerializeField] internal Color endColor;
        [SerializeField] internal Color topLeft;
        [SerializeField] internal Color topRight;
        [SerializeField] internal Color bottomLeft;
        [SerializeField] internal Color bottomRight;
        [SerializeField] internal Gradient ramp;
        [NonSerialized] internal int rampRevision;

        internal readonly bool IsActive => enabled && intensity > 0;

        internal static GradientSettings Create(bool enabled = false)
            => new()
            {
                enabled = enabled,
                blendMode = GradientBlendMode.Multiply,
                intensity = 1,
                startColor = Color.white,
                endColor = Color.white,
                topLeft = Color.white,
                topRight = Color.white,
                bottomLeft = Color.white,
                bottomRight = Color.white,
                ramp = new Gradient()
            };

        internal void InvalidateRamp()
        {
            ramp ??= new Gradient();
            rampRevision = unchecked(rampRevision + 1);
        }

        internal readonly Color Evaluate(Vector2 position)
            => type switch
            {
                GradientType.Corners => Color.Lerp(Color.Lerp(bottomLeft, bottomRight, position.x),
                    Color.Lerp(topLeft, topRight, position.x), position.y),
                GradientType.Ramp => ramp.Evaluate(position.y),
                _ => Color.Lerp(startColor, endColor, position.y)
            };

        internal readonly Color Blend(Color original, Color gradientColor)
            => Color.Lerp(original, blendMode == GradientBlendMode.Multiply ? original * gradientColor : gradientColor, intensity);
    }
}

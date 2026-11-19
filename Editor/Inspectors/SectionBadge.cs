using System;
using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal sealed class SectionBadge
    {
        private readonly Action<Rect> _draw;

        private SectionBadge(string tooltip, Action<Rect> draw)
        {
            Tooltip = tooltip;
            _draw = draw;
        }

        public string Tooltip { get; }

        public void Draw(Rect rect) => _draw(rect);

        public static SectionBadge Create(string tooltip, Action<Rect> draw) => new(tooltip, draw);
    }

    internal static class BadgeGlyphs
    {
        private const float Stroke = 1.5f;
        private const float CornerGap = 3f;

        private static Color Foreground => EditorStyles.label.normal.textColor;

        public static void RoundedRect(Rect r)
        {
            var color = Foreground;
            EditorGUI.DrawRect(new Rect(r.x + CornerGap, r.y, r.width - 2f * CornerGap, Stroke), color);
            EditorGUI.DrawRect(new Rect(r.x + CornerGap, r.yMax - Stroke, r.width - 2f * CornerGap, Stroke), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y + CornerGap, Stroke, r.height - 2f * CornerGap), color);
            EditorGUI.DrawRect(new Rect(r.xMax - Stroke, r.y + CornerGap, Stroke, r.height - 2f * CornerGap), color);
        }

        public static void Ring(Rect r)
        {
            var color = Foreground;
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, Stroke), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - Stroke, r.width, Stroke), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, Stroke, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - Stroke, r.y, Stroke, r.height), color);
        }

        public static void Softness(Rect r)
        {
            var color = Foreground;
            EditorGUI.DrawRect(r, new Color(color.r, color.g, color.b, 0.22f));
            var mid = new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f);
            EditorGUI.DrawRect(mid, new Color(color.r, color.g, color.b, 0.45f));
            var inner = new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f);
            EditorGUI.DrawRect(inner, new Color(color.r, color.g, color.b, 0.9f));
        }

        public static void Crosshair(Rect r)
        {
            var color = Foreground;
            EditorGUI.DrawRect(new Rect(r.x, r.center.y - Stroke * 0.5f, r.width, Stroke), color);
            EditorGUI.DrawRect(new Rect(r.center.x - Stroke * 0.5f, r.y, Stroke, r.height), color);
            EditorGUI.DrawRect(new Rect(r.center.x - 2f, r.center.y - 2f, 4f, 4f), color);
        }

        public static void FilledSquare(Rect r)
        {
            var color = Foreground;
            EditorGUI.DrawRect(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f),
                new Color(color.r, color.g, color.b, 0.85f));
        }

        public static void Mask(Rect r)
        {
            var color = Foreground;
            RoundedRect(r);
            EditorGUI.DrawRect(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f),
                new Color(color.r, color.g, color.b, 0.55f));
        }

        public static void Swatch(Rect r, Color start, Color end)
        {
            const int bands = 6;
            var bandWidth = r.width / bands;
            for (var i = 0; i < bands; i++)
            {
                var t = bands == 1 ? 0f : i / (float)(bands - 1);
                EditorGUI.DrawRect(new Rect(r.x + i * bandWidth, r.y, bandWidth + 0.5f, r.height), Color.Lerp(start, end, t));
            }

            var outline = new Color(0f, 0f, 0f, 0.35f);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), outline);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), outline);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), outline);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), outline);
        }
    }
}

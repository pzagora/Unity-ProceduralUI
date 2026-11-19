using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralUI.Editor
{
    internal static class InspectorSection
    {
        private const float BadgeSize = 14f;
        private const float BadgeSpacing = 3f;
        private const float BadgeRightPadding = 2f;
        private const float TitleGap = 12f;

        private static GUIStyle _headerStyle;

        internal static bool DrawHeader(string title, string stateKey, IReadOnlyList<SectionBadge> badges = null)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var expanded = SessionState.GetBool(stateKey, true);
                _headerStyle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
                var rect = EditorGUILayout.GetControlRect();
                var selected = EditorGUI.Foldout(rect, expanded, title, true, _headerStyle);
                if (selected != expanded)
                    SessionState.SetBool(stateKey, selected);

                if (!selected && badges is { Count: > 0 })
                    DrawBadges(rect, title, badges);

                return selected;
            }
        }

        private static void DrawBadges(Rect rect, string title, IReadOnlyList<SectionBadge> badges)
        {
            var titleRight = EditorGUI.IndentedRect(rect).x + _headerStyle.CalcSize(new GUIContent(title)).x;
            var totalWidth = badges.Count * BadgeSize + (badges.Count - 1) * BadgeSpacing;
            var left = rect.xMax - BadgeRightPadding - totalWidth;

            if (left < titleRight + TitleGap)
                return;

            var y = rect.y + (rect.height - BadgeSize) * 0.5f;
            for (var i = 0; i < badges.Count; i++)
            {
                var badgeRect = new Rect(left + i * (BadgeSize + BadgeSpacing), y, BadgeSize, BadgeSize);
                badges[i].Draw(badgeRect);
                GUI.Label(badgeRect, new GUIContent(string.Empty, badges[i].Tooltip));
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Chris.Gameplay.Editor
{
    public static class GameplayEditorGUILayout
    {
        private static readonly GUIStyle FoldoutStyle = new("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.label).font,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f)
        };

        public static bool Foldout(bool foldout, string content)
        {
            var color = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            
            var rect = GUILayoutUtility.GetRect(new GUIContent(content), FoldoutStyle);
            GUI.Box(rect, content, FoldoutStyle);
            GUI.backgroundColor = color;

            var e = Event.current;
            if (e.type == EventType.Repaint)
            {
                var arrowRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                EditorStyles.foldout.Draw(arrowRect, false, false, foldout, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                foldout = !foldout;
                e.Use();
            }

            return foldout;
        }
    }
}
using System;
using Ceres.Graph.Flow;
using UnityEditor;
using UnityEngine;

namespace Chris.Gameplay.Editor
{
    [CustomPropertyDrawer(typeof(Actor.FlowGraphSettings))]
    public class ActorFlowGraphSettingsDrawer : PropertyDrawer
    {
        private const string FoldoutKey = "Actor_Flow_Advanced_Settings";

        private static class Styles
        {
            public static readonly GUIContent ExportRemoteAssetLabel;

            static Styles()
            {
                var image = EditorGUIUtility.IconContent("Download-Available@2x").image;
                ExportRemoteAssetLabel = new GUIContent(image, 
                    "Export actor flow graph data to a remote asset for hot update");
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect foldoutRect = position;
            foldoutRect.y += 2;
            foldoutRect.height = EditorGUIUtility.singleLineHeight;
            Foldout(foldoutRect, FoldoutKey, "Advanced Settings", () =>
            {
                position.height = EditorGUIUtility.singleLineHeight;
                position.y += position.height + 10;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(Actor.FlowGraphSettings.graphAsset)));
                position.y += position.height + 5;
                float width = position.width;
                position.width = width - 35;
                var addressProp = property.FindPropertyRelative(nameof(Actor.FlowGraphSettings.actorAddress));
                EditorGUI.PropertyField(position, addressProp);
                position.x += position.width + 5;
                position.width = 30;
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(addressProp.stringValue)))
                {
                    if (GUI.Button(position, Styles.ExportRemoteAssetLabel))
                    {
                        var actor = (IFlowGraphContainer)property.serializedObject.targetObject;
                        EditorApplication.delayCall += () => ActorFlowGraphEditorUtils.ExportRemoteAsset(actor, addressProp.stringValue);
                    }
                }
            }, !Application.isPlaying);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool foldOut = EditorPrefs.GetBool(FoldoutKey);
            if (foldOut)
            {
                return EditorGUIUtility.singleLineHeight * 3 + 5 * 2 + 2;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        private static void Foldout(
            Rect rect,
            string foldKey,
            string title = null,
            Action drawAct = null,
            bool enable = true
        )
        {
            var style = new GUIStyle("ShurikenModuleTitle")
            {
                font = new GUIStyle(EditorStyles.label).font,
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = 22,
                contentOffset = new Vector2(20f, -2f)
            };

            var color = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            GUI.Box(rect, title ?? foldKey, style);
            GUI.backgroundColor = color;

            var e = Event.current;
            bool foldOut = EditorPrefs.GetBool(foldKey);

            if (e.type == EventType.Repaint)
            {
                var arrowRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                EditorStyles.foldout.Draw(arrowRect, false, false, foldOut, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                foldOut = !foldOut;
                EditorPrefs.SetBool(foldKey, foldOut);
                e.Use();
            }

            if (foldOut && drawAct != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(!enable))
                    {
                        drawAct();
                    }
                }
            }
        }
    }
}

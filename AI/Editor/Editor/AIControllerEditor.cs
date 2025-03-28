using System.Linq;
using UnityEditor;
using UnityEngine;
using UEditor = UnityEditor.Editor;

namespace Chris.AI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AIController), true)]
    public class AIControllerEditor : UEditor
    {
        private AIController Controller => (AIController)target;
        
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("AI Status       " + (Controller.enabled ?
             (Controller.IsAIEnabled ? "<color=#92F2FF>Running</color>" : "<color=#FFF892>Pending</color>")
             : "<color=#FF787E>Disabled</color>"), new GUIStyle(GUI.skin.label) { richText = true });
            if (Application.isPlaying)
            {
                var tasks = Controller.GetAllTasks();
                if (tasks.Any())
                {
                    GUILayout.Label($"Tasks:", new GUIStyle(GUI.skin.label) { fontSize = 15 });
                }
                foreach (var task in tasks)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{task.GetTaskID()}");
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.x += 200;
                    GUI.Label(rect, $"Status    {task.GetStatus().ToString()}", new GUIStyle(GUI.skin.label) { richText = true });
                    GUILayout.EndHorizontal();
                }
            }
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
}

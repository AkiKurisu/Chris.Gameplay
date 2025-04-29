using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chris.Gameplay.Editor
{
    public enum RemoteUpdateSerializeMode
    {
        AssetBundle,
        PreferText,
        ForceText
    }
    
    [FilePath("ProjectSettings/ChrisGameplaySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ChrisGameplaySettings : ScriptableSingleton<ChrisGameplaySettings>
    {
        public RemoteUpdateSerializeMode remoteUpdateSerializeMode = RemoteUpdateSerializeMode.AssetBundle;
        
        public static void SaveSettings()
        {
            instance.Save(true);
        }
    }

    internal class ChrisGameplaySettingsProvider : SettingsProvider
    {
        private SerializedObject _settingsObject;
        private class Styles
        {
            public static readonly GUIContent RemoteUpdateSerializeModeLabel = new("Serialize Mode", 
                "Asset Bundle: Always export a single AssetBundle. " +
                "Prefer Text: Export plain text if has no dependencies. " +
                "Force Text: Always export plain text which may loose assets dependencies.");
        }
        
        public ChrisGameplaySettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settingsObject = new SerializedObject(ChrisGameplaySettings.instance);
        }
        
        public override void OnGUI(string searchContext)
        {
            DrawRemoteUpdateSettings();
        }
        
        private void DrawRemoteUpdateSettings()
        {
            GUILayout.BeginVertical("Remote Update", GUI.skin.box);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(ChrisGameplaySettings.remoteUpdateSerializeMode)), Styles.RemoteUpdateSerializeModeLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                ChrisGameplaySettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new ChrisGameplaySettingsProvider("Project/Chris/Gameplay Settings", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
}

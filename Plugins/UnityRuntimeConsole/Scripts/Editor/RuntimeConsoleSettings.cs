﻿using Chris.Configs.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityRuntimeConsole.Editor
{
    [FilePath("ProjectSettings/RuntimeConsoleSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class RuntimeConsoleSettings : ScriptableSingleton<RuntimeConsoleSettings>
    {
        public bool enableConsoleInReleaseBuild;

        internal static void SaveSettings()
        {
            instance.Save(true);
            var serializer = ConfigsEditorUtils.GetConfigSerializer();
            var config = RuntimeConsoleConfig.Get();
            config.enableConsoleInReleaseBuild = instance.enableConsoleInReleaseBuild;
            config.Save(serializer);
        }
    }

    internal class RuntimeConsoleSettingsProvider : SettingsProvider
    {
        private SerializedObject _settingsObject;
        
        private class Styles
        {
            public static readonly GUIContent EnableConsoleInReleaseBuildLabel = new("Enable Console In Release Build", "Allow runtime console using in release build.");
        }

        private RuntimeConsoleSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settingsObject = new SerializedObject(RuntimeConsoleSettings.instance);
        }
        
        public override void OnGUI(string searchContext)
        {
            GUILayout.BeginVertical("Runtime Settings", GUI.skin.box);
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.PropertyField(_settingsObject.FindProperty(nameof(RuntimeConsoleSettings.enableConsoleInReleaseBuild)), Styles.EnableConsoleInReleaseBuildLabel);
            if (_settingsObject.ApplyModifiedPropertiesWithoutUndo())
            {
                RuntimeConsoleSettings.SaveSettings();
            }
            GUILayout.EndVertical();
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new RuntimeConsoleSettingsProvider("Project/Chris/Runtime Console Settings", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
}
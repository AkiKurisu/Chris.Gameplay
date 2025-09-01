using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Chris.Gameplay.Editor;
using Chris.Serialization;
using R3;
using UEditor = UnityEditor.Editor;

namespace Chris.Graphics.Editor
{
    [CustomEditor(typeof(GraphicsController))]
    public class GraphicsControllerEditor : UEditor
    {
        private GraphicsController _target;

        private SerializedProperty _graphicsConfigProperty;

        private bool _showVolumeDebug = true;

        private bool _showGraphicsSettings = true;

        private bool _showModuleDebug = true;

        private bool _lookDevMode;

        private static Type[] _cachedModuleTypes;

        private void OnEnable()
        {
            _target = (GraphicsController)target;
            _target.InitializeIfNeed();
            _graphicsConfigProperty = serializedObject.FindProperty("graphicsConfig");

            // Reload LookDev mode
            _lookDevMode = EditorPrefs.GetBool(GraphicsController.LookDevModeKey, false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBasicConfig();

            EditorGUILayout.Space();

            DrawVolumeDebugPanel();

            EditorGUILayout.Space();

            DrawGraphicsSettingsDebugPanel();

            EditorGUILayout.Space();

            DrawModuleDebugPanel();

            EditorGUILayout.Space();

            DrawQuickActionButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBasicConfig()
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_graphicsConfigProperty);
        }

        private void DrawVolumeDebugPanel()
        {
            _showVolumeDebug = GameplayEditorGUILayout.Foldout(_showVolumeDebug, "Volume Debug");
            if (!_showVolumeDebug) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Dynamic Volume Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Dynamic Profiles", GUILayout.Height(25)))
            {
                _target.ApplyDynamicVolumeProfiles();
            }
            if (GUILayout.Button("Switch Windows", GUILayout.Height(25)))
            {
                _target.ApplyVolumeProfiles(DynamicVolumePlatform.Windows);
            }
            if (GUILayout.Button("Switch Mobile", GUILayout.Height(25)))
            {
                _target.ApplyVolumeProfiles(DynamicVolumePlatform.Mobile);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // LookDev Mode Toggle
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // Create styled button with color change when active
            var originalBackgroundColor = GUI.backgroundColor;
            var originalContentColor = GUI.contentColor;

            if (_lookDevMode)
            {
                GUI.backgroundColor = new Color(0.2f, 0.5f, 1f, 1f); // Blue background when active
                GUI.contentColor = Color.white; // White text/icon when active
            }

            var buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 25,
                fontStyle = _lookDevMode ? FontStyle.Bold : FontStyle.Normal
            };

            if (GUILayout.Button(Styles.LookDevContent, buttonStyle, GUILayout.Width(140)))
            {
                _lookDevMode = !_lookDevMode;
                
                EditorPrefs.SetBool(GraphicsController.LookDevModeKey, _lookDevMode);

                ApplyLookDevSettings(_lookDevMode);
            }

            // Restore original colors
            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            EditorGUILayout.EndHorizontal();

            if (_lookDevMode)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("LookDev Mode Active: Post process effects are disabled for material preview.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Volume Weights", EditorStyles.boldLabel);

            var volumeTypes = Enum.GetValues(typeof(DynamicVolumeType));
            foreach (DynamicVolumeType volumeType in volumeTypes)
            {
                var volume = _target.GetVolume(volumeType);
                if (volume != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{volumeType}", GUILayout.Width(180));

                    // In LookDev mode, disable sliders and show current values
                    if (_lookDevMode)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.Slider(volume.weight, 0f, 1f, GUILayout.Width(180));
                        EditorGUI.EndDisabledGroup();

                        // Show LookDev status for each volume
                        var isEnabled = volume.enabled;
                        var statusText = isEnabled ? "ON" : "OFF";
                        var statusColor = isEnabled ? Color.green : Color.red;

                        var originalColor = GUI.color;
                        GUI.color = statusColor;
                        EditorGUILayout.LabelField(statusText, GUILayout.Width(40));
                        GUI.color = originalColor;
                    }
                    else
                    {
                        var newWeight = EditorGUILayout.Slider(volume.weight, 0f, 1f, GUILayout.Width(150));
                        if (Mathf.Abs(newWeight - volume.weight) > 0.001f)
                        {
                            volume.weight = newWeight;
                        }

                        if (GUILayout.Button("0", GUILayout.Width(25)))
                        {
                            volume.weight = 0f;
                        }
                        if (GUILayout.Button("1", GUILayout.Width(25)))
                        {
                            volume.weight = 1f;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGraphicsSettingsDebugPanel()
        {
            _showGraphicsSettings = GameplayEditorGUILayout.Foldout(_showGraphicsSettings, "Graphics Settings Debug");
            if (!_showGraphicsSettings) return;

            EditorGUI.indentLevel++;

            if (Application.isPlaying)
            {
                var settings = GraphicsSettings.Get();

                EditorGUILayout.LabelField("Real-time Settings Control", EditorStyles.boldLabel);

                // Render Scale
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Render Scale", GUILayout.Width(100));
                var newRenderScale = EditorGUILayout.IntSlider(settings.RenderScale.Value, 1, GraphicsSettings.RenderScalePresets.Length);
                if (newRenderScale != settings.RenderScale.Value)
                {
                    settings.RenderScale.Value = newRenderScale;
                }
                EditorGUILayout.LabelField($"{GraphicsSettings.RenderScalePresets[newRenderScale - 1]:F2}", GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Effect Toggles", EditorStyles.boldLabel);

                DrawToggleSetting("Ambient Occlusion", settings.AmbientOcclusion);
                DrawToggleSetting("Bloom", settings.Bloom);
                DrawToggleSetting("Depth of Field", settings.DepthOfField);
                DrawToggleSetting("Motion Blur", settings.MotionBlur);
                DrawToggleSetting("Tonemapping", settings.Tonemapping);
                DrawToggleSetting("Vignette", settings.Vignette);
#if ILLUSION_RP_INSTALL
                DrawToggleSetting("Contact Shadow", settings.ContactShadows);
                DrawToggleSetting("Screen Space Reflection", settings.ScreenSpaceReflection);
                DrawToggleSetting("Volumetric Light", settings.VolumetricFog);
#endif

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable All"))
                {
                    SetAllEffects(true);
                }
                if (GUILayout.Button("Disable All"))
                {
                    SetAllEffects(false);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Graphics settings controls are only available in Play mode", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawModuleDebugPanel()
        {
            _showModuleDebug = GameplayEditorGUILayout.Foldout(_showModuleDebug, "Graphics Modules");
            if (!_showModuleDebug) return;

            EditorGUI.indentLevel++;

            if (_target.graphicsConfig != null && _target.graphicsConfig.graphicsModules != null)
            {
                EditorGUILayout.LabelField($"Loaded Modules ({_target.graphicsConfig.graphicsModules.Length})", EditorStyles.boldLabel);

                for (int i = 0; i < _target.graphicsConfig.graphicsModules.Length; i++)
                {
                    var moduleType = _target.graphicsConfig.graphicsModules[i];
                    if (moduleType != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{i + 1}. {moduleType.GetObjectType()?.Name ?? "Unknown"}", GUILayout.Width(200));

                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            RemoveModuleAt(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Add New Module", EditorStyles.boldLabel);
                var allModuleTypes = GetGraphicsModuleTypes();
                var selectedModuleIndex = EditorGUILayout.Popup("Module Type", -1, allModuleTypes.Select(type => type.Name).ToArray());
                if (selectedModuleIndex >= 0)
                {
                    AddModule(allModuleTypes[selectedModuleIndex]);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No graphics config assigned or no modules configured", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawQuickActionButtons()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All Volumes", GUILayout.Height(30)))
            {
                ResetAllVolumes();
            }
            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                SaveGraphicsSettings();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToggleSetting(string label, ReactiveProperty<bool> setting)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(180));
            var newValue = EditorGUILayout.Toggle(setting.Value, GUILayout.Width(20));
            if (newValue != setting.Value)
            {
                setting.Value = newValue;
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetAllEffects(bool enabled)
        {
            if (!Application.isPlaying) return;

            var settings = GraphicsSettings.Get();
            settings.AmbientOcclusion.Value = enabled;
            settings.Bloom.Value = enabled;
            settings.DepthOfField.Value = enabled;
            settings.MotionBlur.Value = enabled;
            settings.Tonemapping.Value = enabled;
            settings.Vignette.Value = enabled;
#if ILLUSION_RP_INSTALL
            settings.ContactShadows.Value = enabled;
            settings.ScreenSpaceReflection.Value = enabled;
            settings.VolumetricFog.Value = enabled;
#endif
        }

        private void ResetAllVolumes()
        {
            if (!Application.isPlaying) return;

            var volumeTypes = Enum.GetValues(typeof(DynamicVolumeType));
            foreach (DynamicVolumeType volumeType in volumeTypes)
            {
                var volume = _target.GetVolume(volumeType);
                if (volume != null)
                {
                    volume.weight = 1f;
                }
            }
        }

        private static void SaveGraphicsSettings()
        {
            if (!Application.isPlaying) return;

            var settings = GraphicsSettings.Get();
            settings.Save();
            Debug.Log("[Chris] Graphics settings saved successfully!");
        }

        private static Type[] GetGraphicsModuleTypes()
        {
            _cachedModuleTypes ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(GraphicsModule)) && !type.IsAbstract)
                .ToArray();

            return _cachedModuleTypes;
        }

        private void AddModule(Type moduleType)
        {
            if (_target.graphicsConfig == null) return;

            if (moduleType != null)
            {
                var serializedType = SerializedType<GraphicsModule>.FromType(moduleType);

                Undo.RecordObject(_target.graphicsConfig, "Add Graphics Module");

                var newModules = new SerializedType<GraphicsModule>[_target.graphicsConfig.graphicsModules.Length + 1];
                _target.graphicsConfig.graphicsModules.CopyTo(newModules, 0);
                newModules[^1] = serializedType;
                _target.graphicsConfig.graphicsModules = newModules;

                EditorUtility.SetDirty(_target.graphicsConfig);
            }
        }

        private void RemoveModuleAt(int index)
        {
            if (_target.graphicsConfig == null || index < 0 || index >= _target.graphicsConfig.graphicsModules.Length) return;

            Undo.RecordObject(_target.graphicsConfig, "Remove Graphics Module");

            var newModules = new SerializedType<GraphicsModule>[_target.graphicsConfig.graphicsModules.Length - 1];
            for (int i = 0, j = 0; i < _target.graphicsConfig.graphicsModules.Length; i++)
            {
                if (i != index)
                {
                    newModules[j] = _target.graphicsConfig.graphicsModules[i];
                    j++;
                }
            }
            _target.graphicsConfig.graphicsModules = newModules;

            EditorUtility.SetDirty(_target.graphicsConfig);
        }

        private void ApplyLookDevSettings(bool isLookDevModeOn)
        {
            var volumeTypes = Enum.GetValues(typeof(DynamicVolumeType));
            foreach (DynamicVolumeType volumeType in volumeTypes)
            {
                var volume = _target.GetVolume(volumeType);
                if (volume != null)
                {
                    var shouldEnableInLookDev = GraphicsController.IsLookDevVolumeType(volumeType);
                    volume.enabled = shouldEnableInLookDev || !isLookDevModeOn;
                }
            }
        }

        private static class Styles
        {
            public static readonly GUIContent LookDevContent = new(" Look Dev Mode", EditorGUIUtility.IconContent("d_SceneViewFX@2x").image);
        }
    }
}
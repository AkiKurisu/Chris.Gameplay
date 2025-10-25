using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UEditor = UnityEditor.Editor;
using R3;

namespace Chris.Graphics.Editor
{
    [CustomEditor(typeof(GraphicsController))]
    public class GraphicsControllerEditor : UEditor
    {
        private GraphicsController _target;

        private SerializedProperty _graphicsConfigProperty;

        private bool _lookDevMode;

        private static Type[] _cachedModuleTypes;

        private readonly Dictionary<string, bool> _status = new();
        
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

            if (_target.graphicsConfig)
            {
                EditorGUILayout.Space();

                DrawVolumeSettingsPanel();

                EditorGUILayout.Space();

                DrawGraphicsSettingsPanel();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBasicConfig()
        {
            // LookDev Mode Toggle
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
            
            EditorGUILayout.PropertyField(_graphicsConfigProperty);
            
            using (new EditorGUI.DisabledScope(!_target.graphicsConfig))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reload", GUILayout.Height(20)))
                {
                    _target.ApplyDynamicVolumeProfiles();
                }

                if (GUILayout.Button("Switch Windows", GUILayout.Height(20)))
                {
                    _target.ApplyVolumeProfiles(DynamicVolumePlatform.Windows);
                }

                if (GUILayout.Button("Switch Mobile", GUILayout.Height(20)))
                {
                    _target.ApplyVolumeProfiles(DynamicVolumePlatform.Mobile);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawVolumeSettingsPanel()
        {
            if (!Foldout("Volume Settings")) return;

            EditorGUI.indentLevel++;

            if (_lookDevMode)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("LookDev Mode Active: Post process effects are disabled for material preview.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            var volumeTypes = Enum.GetValues(typeof(DynamicVolumeType));
            foreach (DynamicVolumeType volumeType in volumeTypes)
            {
                if (volumeType == DynamicVolumeType.DepthOfField && !_target.graphicsConfig.enableDepthOfField) continue;
                
                var volume = _target.GetVolume(volumeType);
                if (volume)
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

        private void DrawGraphicsSettingsPanel()
        {
            if (!Foldout("Graphics Settings")) return;

            EditorGUI.indentLevel++;

            if (Application.isPlaying)
            {
                var settings = GraphicsSettings.Get();

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

                // Effects
                DrawToggleSetting("Ambient Occlusion", settings.AmbientOcclusion);
                DrawToggleSetting("Bloom", settings.Bloom);
                DrawToggleSetting("Depth of Field", settings.DepthOfField);
                DrawToggleSetting("Motion Blur", settings.MotionBlur);
                DrawToggleSetting("Vignette", settings.Vignette);
#if ILLUSION_RP_INSTALL
                DrawToggleSetting("Contact Shadow", settings.ContactShadows);
                DrawToggleSetting("Screen Space Reflection", settings.ScreenSpaceReflection);
                DrawToggleSetting("Screen Space Global Illumination", settings.ScreenSpaceGlobalIllumination);
                DrawToggleSetting("Volumetric Light", settings.VolumetricFog);
#endif
                
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
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings"))
                {
                    SaveGraphicsSettings();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Graphics settings controls are only available in Play mode", MessageType.Info);
            }

            EditorGUI.indentLevel--;
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
            settings.Vignette.Value = enabled;
#if ILLUSION_RP_INSTALL
            settings.ContactShadows.Value = enabled;
            settings.ScreenSpaceReflection.Value = enabled;
            settings.ScreenSpaceGlobalIllumination.Value = enabled;
            settings.VolumetricFog.Value = enabled;
#endif
        }

        private static void SaveGraphicsSettings()
        {
            if (!Application.isPlaying) return;

            var settings = GraphicsSettings.Get();
            settings.Save();
            Debug.Log("[Chris] Graphics settings saved successfully!");
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
        
        private bool Foldout(string content)
        {
            bool status;
            if (!_status.ContainsKey(content))
            {
                status = EditorPrefs.GetBool($"GraphicsController_{content}", false);
                _status[content] = status;
            }
            else
            {
                status = _status.GetValueOrDefault(content);
            }
            var color = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            
            var rect = GUILayoutUtility.GetRect(new GUIContent(content), Styles.FoldoutStyle);
            GUI.Box(rect, content, Styles.FoldoutStyle);
            GUI.backgroundColor = color;

            var e = Event.current;
            if (e.type == EventType.Repaint)
            {
                var arrowRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
                EditorStyles.foldout.Draw(arrowRect, false, false, status, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                status = !status;
                _status[content] = status;
                EditorPrefs.SetBool($"GraphicsController_{content}", status);
                e.Use();
            }

            return status;
        }
        
        private static class Styles
        {
            public static readonly GUIStyle FoldoutStyle = new("ShurikenModuleTitle")
            {
                font = new GUIStyle(EditorStyles.label).font,
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = 22,
                contentOffset = new Vector2(20f, -2f)
            };
            
            public static readonly GUIContent LookDevContent = new(" Look Dev Mode", EditorGUIUtility.IconContent("d_SceneViewFX@2x").image);
        }
    }
}
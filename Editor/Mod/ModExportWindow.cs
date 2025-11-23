using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Debug = UnityEngine.Debug;

namespace Chris.Gameplay.Mod.Editor
{
    public class ModExportWindow : EditorWindow
    {
        private delegate Vector2 BeginVerticalScrollViewFunc(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options);
        
        private static BeginVerticalScrollViewFunc _beginVerticalScrollViewFunc;
        
        private Vector2 _scrollPosition;
        
        private ModExportConfig _exportConfig;
        
        private SerializedObject _exportConfigObject;
        
        private static BeginVerticalScrollViewFunc BeginVerticalScrollView
        {
            get
            {
                if (_beginVerticalScrollViewFunc == null)
                {
                    var methods = typeof(EditorGUILayout).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.Name == "BeginVerticalScrollView").ToArray();
                    var method = methods.First(x => x.GetParameters()[1].ParameterType == typeof(bool));
                    _beginVerticalScrollViewFunc = (BeginVerticalScrollViewFunc)method.CreateDelegate(typeof(BeginVerticalScrollViewFunc));
                }
                return _beginVerticalScrollViewFunc;
            }
        }
        
        private static readonly FieldInfo MUseCustomPaths = typeof(BundledAssetGroupSchema).GetField("m_UseCustomPaths", BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static string ConfigGuidKey => Application.productName + "_ModConfigGUID";
        
        [MenuItem("Tools/Chris/Mod Exporter")]
        public static void OpenEditor()
        {
            var window = GetWindow<ModExportWindow>("Mod Exporter");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnGUI()
        {
            _scrollPosition = BeginVerticalScrollView(_scrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            ShowExportEditor();
            EditorGUILayout.EndScrollView();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var orgColor = GUI.backgroundColor;
            if (GUILayout.Button(new GUIContent("Select Export Config", "Select Export Config")))
            {
                string path = EditorUtility.OpenFilePanel("Select export config", Application.dataPath, "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace(Application.dataPath, string.Empty);
                    var config = AssetDatabase.LoadAssetAtPath($"Assets/{path}", typeof(ModExportConfig)) as ModExportConfig;
                    if (config != null)
                    {
                        _exportConfig = config;
                        _exportConfigObject = new SerializedObject(_exportConfig);
                    }
                    else
                    {
                        ShowNotification(new GUIContent($"Invalid path: Assets/{path}, please pick export config"));
                    }
                }
            }
            GUI.enabled = _exportConfig.Validate();
            if (GUILayout.Button("Create Group", GUILayout.MinWidth(100)))
            {
                var group = _exportConfig.Group;
                //Set not include in packed build
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.IncludeInBuild = false;
                schema.BuildPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteLoadPath);
#pragma warning disable UNT0018 // System.Reflection features in performance critical messages
                MUseCustomPaths.SetValue(schema, false);
#pragma warning restore UNT0018 // System.Reflection features in performance critical messages
            }
            GUI.backgroundColor = new Color(253 / 255f, 163 / 255f, 255 / 255f);
            if (GUILayout.Button("Export", GUILayout.MinWidth(100)))
            {
                new ModExporter(_exportConfig).Export();
                Process.Start(ModExporter.ExportPath);
                EditorUtility.SetDirty(_exportConfig);
                AssetDatabase.SaveAssets();
            }
            GUI.enabled = true;
            GUI.backgroundColor = orgColor;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        
        private void DrawExportConfig()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_exportConfigObject.FindProperty("authorName"), true);
            EditorGUILayout.PropertyField(_exportConfigObject.FindProperty("modName"), true);
            EditorGUILayout.PropertyField(_exportConfigObject.FindProperty("version"), true);
            GUILayout.EndVertical();
            _exportConfig.modIcon = (Texture2D)EditorGUILayout.ObjectField("Icon", _exportConfig.modIcon, typeof(Texture2D), false);
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_exportConfigObject.FindProperty("description"), true);
            EditorGUILayout.PropertyField(_exportConfigObject.FindProperty("customBuilders"), true);
            if (EditorGUI.EndChangeCheck())
            {
                _exportConfigObject.ApplyModifiedProperties();
            }
        }
        
        private void ShowExportEditor()
        {
            EditorGUILayout.HelpBox($"Export your Mod.\nCurrent Platform: {EditorUserBuildSettings.activeBuildTarget}", MessageType.Info);
            if (_exportConfig == null)
            {
                _exportConfig = LoadExportConfig();
                _exportConfigObject = new SerializedObject(_exportConfig);
            }
            _exportConfigObject ??= new SerializedObject(_exportConfig);
            var newConfig = EditorGUILayout.ObjectField("Export Config", _exportConfig, typeof(ModExportConfig), false) as ModExportConfig;
            if (newConfig != _exportConfig && newConfig != null)
            {
                _exportConfig = newConfig;
                _exportConfigObject = new SerializedObject(_exportConfig);
                EditorPrefs.SetString(ConfigGuidKey, GetGuid(_exportConfig));
            }
            DrawExportConfig();
            if (_exportConfig.customBuilders != null)
            {
                foreach (var customBuilder in _exportConfig.customBuilders)
                {
                    var builder = customBuilder.GetObject();
                    if (string.IsNullOrEmpty(builder?.Description)) continue;
                    GUILayout.Label($"Custom builder {builder.GetType().Name} in use");
                    GUILayout.Label(builder.Description, new GUIStyle(GUI.skin.label) { wordWrap = true });
                }
            }
        }
        
        private static ModExportConfig LoadExportConfig()
        {
            var configs = AssetDatabase.FindAssets($"t:{typeof(ModExportConfig)}").Select(x => AssetDatabase.LoadAssetAtPath<ModExportConfig>(AssetDatabase.GUIDToAssetPath(x))).ToArray();
            ModExportConfig config = null;
            if (configs.Length != 0)
            {
                string configGuid = EditorPrefs.GetString(ConfigGuidKey, null);
                config = configs.FirstOrDefault(x => GetGuid(x) == configGuid);
                if (config == null)
                {
                    config = configs[0];
                    EditorPrefs.SetString(ConfigGuidKey, GetGuid(config));
                }
            }
            if (config == null)
            {
                config = CreateInstance<ModExportConfig>();
                string settingsPath = "Assets/ModExportConfig.asset";
                Debug.Log($"<color=#3aff48>Exporter</color>: Mod export config saved to {settingsPath}");
                AssetDatabase.CreateAsset(config, settingsPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
        
        private static string GetGuid(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}

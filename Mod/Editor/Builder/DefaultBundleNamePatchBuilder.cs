using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;

namespace Chris.Mod.Editor
{
    public class DefaultBundleNamePatchBuilder : CustomBuilder
    {
        public override string Description => "Use mod name for naming shader bundle and monoScript bundle." +
                " If you build mod in source project and use project hash as name, use this builder for preventing bundle conflict.";
        
        public override void Build(ModExportConfig exportConfig, string buildPath)
        {
#if UNITY_6000_0_OR_NEWER
            AddressableAssetSettingsDefaultObject.Settings.BuiltInBundleNaming = BuiltInBundleNaming.Custom;
            AddressableAssetSettingsDefaultObject.Settings.BuiltInBundleCustomNaming = $"Mod_{exportConfig.modName}_BuiltIn";
#else
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleNaming = ShaderBundleNaming.Custom;
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleCustomNaming = $"Mod_{exportConfig.modName}_Shader";
#endif
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleNaming = MonoScriptBundleNaming.Custom;
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleCustomNaming = $"Mod_{exportConfig.modName}_MonoScript";
        }

        public override void Cleanup(ModExportConfig exportConfig)
        {
#if UNITY_6000_0_OR_NEWER
            AddressableAssetSettingsDefaultObject.Settings.BuiltInBundleNaming = BuiltInBundleNaming.ProjectName;
#else
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleNaming = ShaderBundleNaming.ProjectName;
#endif
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleNaming = MonoScriptBundleNaming.ProjectName;
        }
    }
}
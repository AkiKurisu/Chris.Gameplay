using Ceres.Graph.Flow;
using Ceres.Graph.Flow.Annotations;
using Chris.Resource.Editor;
using Chris.Serialization;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Chris.Gameplay.Mod.Editor
{
    /// <summary>
    /// Scriptable mod export configuration
    /// </summary>
    [CreateAssetMenu(fileName = "ModExportConfig", menuName = "Chris/Mod/Export Config")]
    public class ModExportConfig : FlowGraphScriptableObject
    {
        public string authorName = "Default";

        public string modName = "Mod";

        public string version = "1.0";

        [Multiline]
        public string description;

        [HideInInspector]
        public byte[] iconData;

        public SerializedType<CustomBuilder>[] customBuilders;

        [HideInInspector]
        public string lastExportPath;

        internal bool Validate()
        {
            if (string.IsNullOrEmpty(authorName)) return false;
            if (string.IsNullOrEmpty(modName)) return false;
            if (string.IsNullOrEmpty(version)) return false;
            return true;
        }

        public AddressableAssetGroup GetOrCreateAssetGroup()
        {
            return ResourceEditorUtils.GetOrCreateAssetGroup($"Mod_{modName}");
        }

        [ImplementableEvent]
        public void Flow_OnBuild(string buildPath)
        {

        }

        [ImplementableEvent]
        public void Flow_OnCleanup()
        {

        }
    }
}

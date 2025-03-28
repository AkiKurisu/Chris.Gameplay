using System;
using Chris.DataDriven;
using Chris.Resource;
using UnityEngine;

namespace Chris.Gameplay.Level
{
    public enum LoadLevelMode
    {
        Single,
        Additive,
        Dynamic
    }
    
    [Flags]
    public enum LoadLevelPolicy
    {
        Never = 0,
        PC = 2,
        Mobile = 4,
        Console = 8
    }
    
    [Serializable, AddressableDataTable(address:LevelSceneDataTableManager.TableKey)]
    public class LevelSceneRow : IDataTableRow
    {
        public string levelName;
        
        /// <summary>
        /// Soft reference (address) to the real scene asset
        /// </summary>
#if UNITY_EDITOR
        [AssetReferenceConstraint(typeof(UnityEditor.SceneAsset))]
#endif
        public SoftAssetReference reference;
        
        public LoadLevelMode loadMode;
        
        public LoadLevelPolicy loadPolicy = LoadLevelPolicy.PC | LoadLevelPolicy.Mobile | LoadLevelPolicy.Console;

        public string[] tags;
        
        public bool ValidateLoadPolicy()
        {
            if (loadPolicy == LoadLevelPolicy.Never) return false;
            if (Application.isMobilePlatform) return loadPolicy.HasFlag(LoadLevelPolicy.Mobile);
            if (Application.isConsolePlatform) return loadPolicy.HasFlag(LoadLevelPolicy.Console);
            return loadPolicy.HasFlag(LoadLevelPolicy.PC);
        }
    }
}

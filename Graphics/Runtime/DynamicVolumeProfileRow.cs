using System;
using System.Collections.Generic;
using Chris.DataDriven;
using Chris.Pool;
using Chris.Resource;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chris.Graphics
{
    /// <summary>
    /// Define volume type that can can be altered dynamically
    /// </summary>
    public enum DynamicVolumeType
    {
        Bloom,
        DepthOfField,
        MotionBlur,
        Tonemapping,
        Vignette,
        ScreenSpaceAmbientOcclusion,
        ScreenSpaceReflection,
        ScreenSpaceGlobalIllumination,
        SubsurfaceScattering,
        PercentageCloserSoftShadows,
        ContactShadows,
        VolumetricFog
    }

    /// <summary>
    /// Define platform options for Dynamic Volume
    /// </summary>
    public enum DynamicVolumePlatform
    {
        Windows,
        Mobile
    }
    
    [Serializable, AddressableDataTable(address: DynamicVolumeProfileTableManager.TableKey)]
    public class DynamicVolumeProfileRow: IDataTableRow, IValidateRow
    {
        public DynamicVolumeType type;
        
        [AssetReferenceConstraint(group: "Volumes")]
        public SoftAssetReference<VolumeProfile> windowsProfile;
        
        [AssetReferenceConstraint(group: "Volumes")]
        public SoftAssetReference<VolumeProfile> mobileProfile;

        [Tooltip("A value which determines which Volume is being used when Volumes have an equal amount of influence on the Scene. " +
                 "Volumes with a higher priority will override lower ones.")]
        public int priority;

        public SoftAssetReference<VolumeProfile> GetVolumeProfile(DynamicVolumePlatform? overridePlatform = null)
        {
#if UNITY_STANDALONE_WIN
            bool useMobileProfile = false;
#else
            bool useMobileProfile = true;
#endif
            if (overridePlatform.HasValue)
            {
                useMobileProfile = overridePlatform.Value == DynamicVolumePlatform.Mobile;
            }
            
            if (useMobileProfile)
            {
                return mobileProfile;
            }

            return windowsProfile;
        }

        public bool ValidateRow(string rowId, out string reason)
        {
            if (type.ToString() == rowId)
            {
                reason = null;
                return true;
            }

            reason = $"row {rowId} should match with DynamicVolumeType {type}";
            return false;
        }
    }
    
    public class DynamicVolumeProfileTableManager : DataTableManager<DynamicVolumeProfileTableManager>
    {
        public DynamicVolumeProfileTableManager(object _) : base(_)
        {
        }

        public const string TableKey = "DynamicVolumeProfileTable";

        private readonly Dictionary<DynamicVolumeType, DynamicVolumeProfileRow> _profileRows = new();
        
        protected sealed override async UniTask Initialize(bool sync)
        {
            await InitializeSingleTable(TableKey, sync);
            var dt = GetDataTable(TableKey);
            var rows = dt.GetAllRows<DynamicVolumeProfileRow>();
            if (sync)
            {
                foreach (var row in rows)
                {
                    _profileRows[row.type] = row;
#if UNITY_STANDALONE_WIN
                    row.windowsProfile.LoadAsync().WaitForCompletion();
#else
                    row.mobileProfile.LoadAsync().WaitForCompletion();
#endif
                }
                return;
            }
            
            using var parallel = UniParallel.Get();
            foreach (var row in rows)
            {
                _profileRows[row.type] = row;
#if UNITY_STANDALONE_WIN
                parallel.Add(row.windowsProfile.LoadAsync().ToUniTask());
#else
                parallel.Add(row.mobileProfile.LoadAsync().ToUniTask());
#endif
            }
            await parallel;
        }

        public VolumeProfile GetProfile(DynamicVolumeType volumeType, DynamicVolumePlatform? overridePlatform = null)
        {
            if (_profileRows.TryGetValue(volumeType, out var row))
            {
                return row.GetVolumeProfile(overridePlatform).LoadAsync().Result;
            }

            return null;
        }
        
        public float GetPriority(DynamicVolumeType volumeType)
        {
            if (_profileRows.TryGetValue(volumeType, out var row))
            {
                return row.priority;
            }

            return 0;
        }
    }
}